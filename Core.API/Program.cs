using Core.API.Security;
using Core.EF.Infrastructure.Database;
using Core.EF.Infrastructure.Services;
using Core.EF.WebApi;
using Core.EF.WebApi.Helper;
using Core.Entity.Model.Systems;
using Core.Helper.IOC;
using Core.Helper.NetCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Diagnostics;
using System.Net;


var builder = WebApplication.CreateBuilder(args);

try
{
    ThreadPool.GetMaxThreads(out var workerThreads, out var completionPortThreads);
    Console.WriteLine($"Get workerThreads={workerThreads} - completionPortThreads={completionPortThreads}");
    workerThreads /= 2;
    completionPortThreads /= 2;
    ThreadPool.SetMinThreads(workerThreads, completionPortThreads);
}
catch (Exception exception)
{
    Console.WriteLine(exception);

}



// Add services to the container
builder.Services.AddControllers(o =>
{
    if (o.Conventions.All(t => t.GetType() != typeof(GenericControllerRouteConvention)))
    {
       // o.Conventions.Add(new GenericControllerRouteConvention());
    }
    o.ModelMetadataDetailsProviders.Add(new ValidationMetadataProvider());

}).AddJsonOptions(options =>
{
     options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
}).ConfigureApplicationPartManager(m =>
{
    if (m.FeatureProviders.All(t => t.GetType() != typeof(GenericTypeControllerFeatureProvider)))
    {
        //m.FeatureProviders.Add(new GenericTypeControllerFeatureProvider());
    }
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104857600; // 100 MB
});

// Đăng ký HttpContext Accessor 
builder.Services.AddHttpContextAccessor();

// Đăng ký Infrastructure:DbContext, Identity, JWT
builder.Services.AddCoreAuthentication(builder.Configuration);

#region Serilog configuration
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
);
#endregion Serilog configuration

builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, Core.EF.WebApi.Helper.ServiceBasedControllerActivator>());
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
/*
builder.Services.RemoveAll<IApiDescriptionProvider>();
builder.Services.TryAddEnumerable(
    ServiceDescriptor.Transient<IApiDescriptionProvider, EndpointMetadataApiDescriptionProvider>());
*/
//builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(options => {

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Bearer Authentication with JWT Token",
        Type = SecuritySchemeType.Http
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List < string > ()
        }
    });
});

builder.Services.AddScoped<JwtService>();



builder.Services.AddDbContextPool<AppDbContext>((serviceProvider, options) =>
{
    var configDb=  serviceProvider.GetRequiredService<IConfiguration>().GetSection("DbProvider:ConnectionString").Value;
    options.UseLazyLoadingProxies();
    options.EnableDetailedErrors(true);
    options.EnableSensitiveDataLogging(true);
    //options.UseLazyLoadingProxies(true);
    //options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(CoreEventId.DetachedLazyLoadingWarning));
  
 
  
    options.LogTo(message => Debug.WriteLine(message));   
    options.UseLazyLoadingProxies();
    options.ConfigureWarnings(warnings => warnings.Ignore(SqlServerEventId.DecimalTypeDefaultWarning));
    options.UseSqlServer(configDb, opt => {
        opt.MigrationsHistoryTable(
         "__EFMigrationsHistory");
        opt.EnableRetryOnFailure(2);
        opt.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
    }).EnableThreadSafetyChecks();//.AddInterceptors(serviceProvider.GetRequiredService<SecondLevelCacheInterceptor>());
}, poolSize: 256);


builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "OpenCors",
                      policy =>
                      {
                          policy.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin();
                      });
});
builder.Services.AddScoped<IApplicationContext, ApplicationContext>();
//EF Core Config Service
builder.Services.AddApiServiceCore(typeof(IServiceBase<>), typeof(ServiceBase<>));


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
    // Kiểm tra nếu Admin chưa tồn tại thì thêm vào
    if (!dbContext.Set<SysUser>().Any(u => u.UserName == "admin@cube.com"))
    {
        var adminUser = new SysUser
        {
            ID = Guid.NewGuid().ToString(),
            FirstName = "Supper",
            LastName = "Admin",
            Email = "admin@cube.com",

            UserName= "admin@cube.com",
            PassWord = BCrypt.Net.BCrypt.HashPassword("cube@123"), // Mã hóa mật khẩu
            ResetPasswordToken=Guid.NewGuid().ToString(),
            Status = 1,
            CreateBy = "admin@cube.com",
            CreatebyName = "Supper Admin",
            CreateDate = DateTime.UtcNow,
            LastUpdateBy = "admin@cube.com",
            LastUpdateByName = "Supper Admin",
            LastUpdateDate = DateTime.UtcNow,
            JwtRefreshToken = string.Empty,


        };


        await dbContext.Set<SysUser>().AddAsync(adminUser);
        await dbContext.SaveChangesAsync();
    }
}
var provider = builder.Services.BuildServiceProvider();
var iHttpContextAccessor = provider.GetService<IHttpContextAccessor>();
var iLoggerFactory = provider.GetService<ILoggerFactory>();
ServiceLocator.Setup(iHttpContextAccessor!, iLoggerFactory!);

// Configure the HTTP request pipeline.
//if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.InjectStylesheet("/swagger/ui/custom.css");
        options.DefaultModelsExpandDepth(-1);
    });
}
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {

        var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (contextFeature != null)
        {
            context.RequestServices.GetService<ILogger<Program>>()?.LogError(contextFeature.Error, contextFeature.Path);
            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    StatusCode = 500,
                    Message = contextFeature.Error.Message,
                    StackTrace = contextFeature.Error.StackTrace
                });
            }
            catch (Exception exception)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    StatusCode = 500,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace
                });
            }
            

        }

    });
});

app.UseStaticFiles();
app.UseHttpsRedirection();
//Yêu cầu xác thực và ủy quyền JWT: Lưu ý add Authentication trước Author

app.UseSerilogRequestLogging();


app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
/*
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});*/

app.MapControllers(); // ✅ Replaces UseEndpoints

app.Run();
