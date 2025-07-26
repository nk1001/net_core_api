using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using Core.Helper.Attribute;

namespace Core.EF.WebApi.Helper
{
    public class EndpointMetadataApiDescriptionProvider : IApiDescriptionProvider
    {
        private readonly EndpointDataSource _endpointDataSource;
        private readonly IHostEnvironment _environment;
        private readonly IServiceProviderIsService? _serviceProviderIsService;
        private readonly ParameterBindingMethodCache ParameterBindingMethodCache = new();

        // Executes before MVC's DefaultApiDescriptionProvider and GrpcHttpApiDescriptionProvider for no particular reason.
        public int Order => -1100;

        public EndpointMetadataApiDescriptionProvider(EndpointDataSource endpointDataSource, IHostEnvironment environment)
            : this(endpointDataSource, environment, null)
        {
        }

        public EndpointMetadataApiDescriptionProvider(
            EndpointDataSource endpointDataSource,
            IHostEnvironment environment,
            IServiceProviderIsService? serviceProviderIsService)
        {
            _endpointDataSource = endpointDataSource;
            _environment = environment;
            _serviceProviderIsService = serviceProviderIsService;
        }

        public void OnProvidersExecuting(ApiDescriptionProviderContext context)
        {
            foreach (var endpoint in _endpointDataSource.Endpoints)
            {
                if (endpoint is RouteEndpoint)
                {
                    
                    var routeEndpoint = (endpoint as RouteEndpoint);
                    var httpMethodMetadata = routeEndpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
                    var _target = routeEndpoint.RequestDelegate.Target;

                    var _actionDescriptor = routeEndpoint.RequestDelegate.Target.GetType().GetField("actionDescriptor");
                    if (_actionDescriptor != null) 
                    {
                        var methodInfo = ((ControllerActionDescriptor)_actionDescriptor.GetValue(_target))?.MethodInfo;
                        foreach (var httpMethod in httpMethodMetadata.HttpMethods)
                        {
                            var action = CreateApiDescription((endpoint as RouteEndpoint), httpMethod, methodInfo);
                            if (!context.Results.Any(t => t.RelativePath == action.RelativePath && t.HttpMethod==action.HttpMethod))
                            {
                                context.Results.Add(CreateApiDescription((endpoint as RouteEndpoint), httpMethod, methodInfo));
                            }
                        }
                    }
                    else
                    {
                        var methodInfo = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?.MethodInfo;
                        foreach (var httpMethod in httpMethodMetadata.HttpMethods)
                        {
                            var action = CreateApiDescription((endpoint as RouteEndpoint), httpMethod, methodInfo);
                            if (!context.Results.Any(t => t.RelativePath == action.RelativePath && t.HttpMethod == action.HttpMethod))
                            {
                                context.Results.Add(CreateApiDescription((endpoint as RouteEndpoint), httpMethod, methodInfo));
                            }
                        }
                    }
                  
                   
                }
            }

        }

        public void OnProvidersExecuted(ApiDescriptionProviderContext context)
        {
        }

        private ApiDescription CreateApiDescription(RouteEndpoint routeEndpoint, string httpMethod, MethodInfo methodInfo)
        {
            // Swashbuckle uses the "controller" name to group endpoints together.
            // For now, put all methods defined the same declaring type together.
            string controllerName;
            string? routerText = null;
            if (methodInfo.DeclaringType is not null && !TypeHelper.IsCompilerGeneratedType(methodInfo.DeclaringType))
            {
                controllerName = methodInfo.DeclaringType.Name;
                if ((methodInfo).DeclaringType.GenericTypeArguments.Length>0)
                {
                    var apiControllerAttribute = (methodInfo).DeclaringType.GenericTypeArguments[0]
                        .GetCustomAttribute<GeneratedApiControllerAttribute>();
                    var isMethod = httpMethod.ToLower() == routeEndpoint.RoutePattern.Defaults["action"]?.ToString()?.ToLower();
                    routerText = apiControllerAttribute == null ? routeEndpoint.RoutePattern.RawText : apiControllerAttribute.Route + (isMethod?"": "/" + routeEndpoint.RoutePattern.Defaults["action"]);
                    var disAttr = ((methodInfo).DeclaringType.GenericTypeArguments[0])
                        .GetCustomAttribute<DisplayNameAttribute>();
                    if (disAttr!=null)
                    {
                        controllerName = disAttr.DisplayName;
                    }
                    else
                    {
                        controllerName = (methodInfo).DeclaringType.GenericTypeArguments[0].Name;
                    }
                }
            }
            else
            {
                // If the declaring type is null or compiler-generated (e.g. lambdas),
                // group the methods under the application name.
                controllerName = _environment.ApplicationName;
            }

           
            var apiDescription = new ApiDescription
            {
                HttpMethod = httpMethod,
                GroupName = routeEndpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName,
                RelativePath = routerText?? routeEndpoint.RoutePattern.RawText?.TrimStart('/'),
                ActionDescriptor = new ActionDescriptor
                {
                    DisplayName = routeEndpoint.DisplayName,
                    RouteValues =
                    {
                        ["controller"] = controllerName,
                    },
                },
            };

            foreach (var parameter in methodInfo.GetParameters())
            {
                var parameterDescription = CreateApiParameterDescription(parameter, routeEndpoint.RoutePattern);

                if (parameterDescription is null)
                {
                    continue;
                }

                apiDescription.ParameterDescriptions.Add(parameterDescription);
            }

            // Get IAcceptsMetadata.
            var acceptsMetadata = routeEndpoint.Metadata.GetMetadata<IAcceptsMetadata>();
            if (acceptsMetadata is not null)
            {
                var acceptsRequestType = acceptsMetadata.RequestType;
                var isOptional = acceptsMetadata.IsOptional;
                var parameterDescription = new ApiParameterDescription
                {
                    Name = acceptsRequestType is not null ? acceptsRequestType.Name : typeof(void).Name,
                    ModelMetadata = CreateModelMetadata(acceptsRequestType ?? typeof(void)),
                    Source = BindingSource.Body,
                    Type = acceptsRequestType ?? typeof(void),
                    IsRequired = !isOptional,
                };
                apiDescription.ParameterDescriptions.Add(parameterDescription);

                var supportedRequestFormats = apiDescription.SupportedRequestFormats;

                foreach (var contentType in acceptsMetadata.ContentTypes)
                {
                    supportedRequestFormats.Add(new ApiRequestFormat
                    {
                        MediaType = contentType
                    });
                }
            }

            AddSupportedResponseTypes(apiDescription.SupportedResponseTypes, methodInfo.ReturnType, routeEndpoint.Metadata);
            AddActionDescriptorEndpointMetadata(apiDescription.ActionDescriptor, routeEndpoint.Metadata);

            return apiDescription;
        }

        private ApiParameterDescription? CreateApiParameterDescription(ParameterInfo parameter, RoutePattern pattern)
        {
            var (source, name, allowEmpty, paramType) = GetBindingSourceAndName(parameter, pattern);

            // Services are ignored because they are not request parameters.
            // We ignore/skip body parameter because the value will be retrieved from the IAcceptsMetadata.
          /*  if (source == BindingSource.Services)
            {
                return null;
            }*/

            // Determine the "requiredness" based on nullability, default value or if allowEmpty is set
            var nullabilityContext = new NullabilityInfoContext();
            var nullability = nullabilityContext.Create(parameter);
            var isOptional = parameter.HasDefaultValue || nullability.ReadState != NullabilityState.NotNull || allowEmpty;
            var parameterDescriptor = CreateParameterDescriptor(parameter);

            return new ApiParameterDescription
            {
                Name = name,
                ModelMetadata = CreateModelMetadata(paramType),
                Source = source,
                DefaultValue = parameter.DefaultValue,
                Type = parameter.ParameterType,
                IsRequired = !isOptional,
                ParameterDescriptor = parameterDescriptor
            };
        }

        private static ParameterDescriptor CreateParameterDescriptor(ParameterInfo parameter)
            => new EndpointParameterDescriptor
            {
                Name = parameter.Name ?? string.Empty,
                ParameterInfo = parameter,
                ParameterType = parameter.ParameterType,
            };

        // TODO: Share more of this logic with RequestDelegateFactory.CreateArgument(...) using RequestDelegateFactoryUtilities
        // which is shared source.
        private (BindingSource, string, bool, Type) GetBindingSourceAndName(ParameterInfo parameter, RoutePattern pattern)
        {
            var attributes = parameter.GetCustomAttributes();

            if (attributes.OfType<IFromRouteMetadata>().FirstOrDefault() is { } routeAttribute)
            {
                return (BindingSource.Path, routeAttribute.Name ?? parameter.Name ?? string.Empty, false, parameter.ParameterType);
            }
            else if (attributes.OfType<IFromQueryMetadata>().FirstOrDefault() is { } queryAttribute)
            {
                return (BindingSource.Query, queryAttribute.Name ?? parameter.Name ?? string.Empty, false, parameter.ParameterType);
            }
            else if (attributes.OfType<IFromHeaderMetadata>().FirstOrDefault() is { } headerAttribute)
            {
                return (BindingSource.Header, headerAttribute.Name ?? parameter.Name ?? string.Empty, false, parameter.ParameterType);
            }
            else if (attributes.OfType<IFromBodyMetadata>().FirstOrDefault() is { } fromBodyAttribute)
            {
                return (BindingSource.Body, parameter.Name ?? string.Empty, fromBodyAttribute.AllowEmpty, parameter.ParameterType);
            }
            else if (parameter.CustomAttributes.Any(a => typeof(IFromServiceMetadata).IsAssignableFrom(a.AttributeType)) ||
                     parameter.ParameterType == typeof(HttpContext) ||
                     parameter.ParameterType == typeof(HttpRequest) ||
                     parameter.ParameterType == typeof(HttpResponse) ||
                     parameter.ParameterType == typeof(ClaimsPrincipal) ||
                     parameter.ParameterType == typeof(CancellationToken) ||
                     ParameterBindingMethodCache.HasBindAsyncMethod(parameter) ||
                     _serviceProviderIsService?.IsService(parameter.ParameterType) == true)
            {
                return (BindingSource.Services, parameter.Name ?? string.Empty, false, parameter.ParameterType);
            }
            else if (parameter.ParameterType == typeof(string) || ParameterBindingMethodCache.HasTryParseMethod(parameter))
            {
                // complex types will display as strings since they use custom parsing via TryParse on a string
                var displayType = !parameter.ParameterType.IsPrimitive && Nullable.GetUnderlyingType(parameter.ParameterType)?.IsPrimitive != true
                    ? typeof(string) : parameter.ParameterType;
                // Path vs query cannot be determined by RequestDelegateFactory at startup currently because of the layering, but can be done here.
                if (parameter.Name is { } name && pattern.GetParameter(name) is not null)
                {
                    return (BindingSource.Path, name, false, displayType);
                }
                else
                {
                    return (BindingSource.Query, parameter.Name ?? string.Empty, false, displayType);
                }
            }
            else
            {
                return (BindingSource.Body, parameter.Name ?? string.Empty, false, parameter.ParameterType);
            }
        }

        private static void AddSupportedResponseTypes(
            IList<ApiResponseType> supportedResponseTypes,
            Type returnType,
            EndpointMetadataCollection endpointMetadata)
        {
            var responseType = returnType;

            if (AwaitableInfo.IsTypeAwaitable(responseType, out var awaitableInfo))
            {
                responseType = awaitableInfo.ResultType;
            }

            // Can't determine anything about IResults yet that's not from extra metadata. IResult<T> could help here.
            if (typeof(IResult).IsAssignableFrom(responseType))
            {
                responseType = typeof(void);
            }

            // We support attributes (which implement the IApiResponseMetadataProvider) interface
            // and types added via the extension methods (which implement IProducesResponseTypeMetadata).
            var responseProviderMetadata = endpointMetadata.GetOrderedMetadata<IApiResponseMetadataProvider>();
            var producesResponseMetadata = endpointMetadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
            var errorMetadata = endpointMetadata.GetMetadata<ProducesErrorResponseTypeAttribute>();
            var defaultErrorType = errorMetadata?.Type ?? typeof(void);
            var contentTypes = new MediaTypeCollection();

            var responseProviderMetadataTypes = ApiResponseTypeProvider.ReadResponseMetadata(
                responseProviderMetadata, responseType, defaultErrorType, contentTypes);
            var producesResponseMetadataTypes = ReadResponseMetadata(producesResponseMetadata, responseType);

            // We favor types added via the extension methods (which implements IProducesResponseTypeMetadata)
            // over those that are added via attributes.
            var responseMetadataTypes = producesResponseMetadataTypes.Values.Concat(responseProviderMetadataTypes);

            if (responseMetadataTypes.Any())
            {
                foreach (var apiResponseType in responseMetadataTypes)
                {
                    // void means no response type was specified by the metadata, so use whatever we inferred.
                    // ApiResponseTypeProvider should never return ApiResponseTypes with null Type, but it doesn't hurt to check.
                    if (apiResponseType.Type is null || apiResponseType.Type == typeof(void))
                    {
                        apiResponseType.Type = responseType;
                    }

                    apiResponseType.ModelMetadata = CreateModelMetadata(apiResponseType.Type);

                    if (contentTypes.Count > 0)
                    {
                        AddResponseContentTypes(apiResponseType.ApiResponseFormats, contentTypes);
                    }
                    // Only set the default response type if it hasn't already been set via a
                    // ProducesResponseTypeAttribute.
                    else if (apiResponseType.ApiResponseFormats.Count == 0 && CreateDefaultApiResponseFormat(apiResponseType.Type) is { } defaultResponseFormat)
                    {
                        apiResponseType.ApiResponseFormats.Add(defaultResponseFormat);
                    }

                    if (!supportedResponseTypes.Any(existingResponseType => existingResponseType.StatusCode == apiResponseType.StatusCode))
                    {
                        supportedResponseTypes.Add(apiResponseType);
                    }

                }
            }
            else
            {
                // Set the default response type only when none has already been set explicitly with metadata.
                var defaultApiResponseType = CreateDefaultApiResponseType(responseType);

                if (contentTypes.Count > 0)
                {
                    // If metadata provided us with response formats, use that instead of the default.
                    defaultApiResponseType.ApiResponseFormats.Clear();
                    AddResponseContentTypes(defaultApiResponseType.ApiResponseFormats, contentTypes);
                }

                supportedResponseTypes.Add(defaultApiResponseType);
            }
        }

        private static Dictionary<int, ApiResponseType> ReadResponseMetadata(
            IReadOnlyList<IProducesResponseTypeMetadata> responseMetadata,
            Type? type)
        {
            var results = new Dictionary<int, ApiResponseType>();

            foreach (var metadata in responseMetadata)
            {
                var statusCode = metadata.StatusCode;

                var apiResponseType = new ApiResponseType
                {
                    Type = metadata.Type,
                    StatusCode = statusCode,
                };

                if (apiResponseType.Type == typeof(void))
                {
                    if (type != null && (statusCode == StatusCodes.Status200OK || statusCode == StatusCodes.Status201Created))
                    {
                        // Allow setting the response type from the return type of the method if it has
                        // not been set explicitly by the method.
                        apiResponseType.Type = type;
                    }
                }

                var attributeContentTypes = new MediaTypeCollection();
                if (metadata.ContentTypes != null)
                {
                    foreach (var contentType in metadata.ContentTypes)
                    {
                        attributeContentTypes.Add(contentType);
                    }
                }
                ApiResponseTypeProvider.CalculateResponseFormatForType(apiResponseType, attributeContentTypes, responseTypeMetadataProviders: null, modelMetadataProvider: null);

                if (apiResponseType.Type != null)
                {
                    results[apiResponseType.StatusCode] = apiResponseType;
                }
            }

            return results;
        }

        private static ApiResponseType CreateDefaultApiResponseType(Type responseType)
        {
            var apiResponseType = new ApiResponseType
            {
                ModelMetadata = CreateModelMetadata(responseType),
                StatusCode = 200,
                Type = responseType,
            };

            if (CreateDefaultApiResponseFormat(responseType) is { } responseFormat)
            {
                apiResponseType.ApiResponseFormats.Add(responseFormat);
            }

            return apiResponseType;
        }

        private static ApiResponseFormat? CreateDefaultApiResponseFormat(Type responseType)
        {
            if (responseType == typeof(void))
            {
                return null;
            }
            else if (responseType == typeof(string))
            {
                // This uses HttpResponse.WriteAsync(string) method which doesn't set a content type. It could be anything,
                // but I think "text/plain" is a reasonable assumption if nothing else is specified with metadata.
                return new ApiResponseFormat { MediaType = "text/plain" };
            }
            else
            {
                // Everything else is written using HttpResponse.WriteAsJsonAsync<TValue>(T).
                return new ApiResponseFormat { MediaType = "application/json" };
            }
        }

        private static EndpointModelMetadata CreateModelMetadata(Type type) =>
            new(ModelMetadataIdentity.ForType(type));

        private static void AddResponseContentTypes(IList<ApiResponseFormat> apiResponseFormats, IReadOnlyList<string> contentTypes)
        {
            foreach (var contentType in contentTypes)
            {
                apiResponseFormats.Add(new ApiResponseFormat
                {
                    MediaType = contentType,
                });
            }
        }

        private static void AddActionDescriptorEndpointMetadata(
            ActionDescriptor actionDescriptor,
            EndpointMetadataCollection endpointMetadata)
        {
            if (endpointMetadata.Count > 0)
            {
                // ActionDescriptor.EndpointMetadata is an empty array by
                // default so need to add the metadata into a new list.
                actionDescriptor.EndpointMetadata = new List<object>(endpointMetadata);
            }
        }
    }
    internal class EndpointModelMetadata : ModelMetadata
    {
        public EndpointModelMetadata(ModelMetadataIdentity identity) : base(identity)
        {
            IsBindingAllowed = true;
        }

        public override IReadOnlyDictionary<object, object> AdditionalValues { get; } = ImmutableDictionary<object, object>.Empty;
        public override string? BinderModelName { get; }
        public override Type? BinderType { get; }
        public override BindingSource? BindingSource { get; }
        public override bool ConvertEmptyStringToNull { get; }
        public override string? DataTypeName { get; }
        public override string? Description { get; }
        public override string? DisplayFormatString { get; }
        public override string? DisplayName { get; }
        public override string? EditFormatString { get; }
        public override ModelMetadata? ElementMetadata { get; }
        public override IEnumerable<KeyValuePair<EnumGroupAndName, string>>? EnumGroupedDisplayNamesAndValues { get; }
        public override IReadOnlyDictionary<string, string>? EnumNamesAndValues { get; }
        public override bool HasNonDefaultEditFormat { get; }
        public override bool HideSurroundingHtml { get; }
        public override bool HtmlEncode { get; }
        public override bool IsBindingAllowed { get; }
        public override bool IsBindingRequired { get; }
        public override bool IsEnum { get; }
        public override bool IsFlagsEnum { get; }
        public override bool IsReadOnly { get; }
        public override bool IsRequired { get; }
        public override ModelBindingMessageProvider ModelBindingMessageProvider { get; } = new DefaultModelBindingMessageProvider();
        public override string? NullDisplayText { get; }
        public override int Order { get; }
        public override string? Placeholder { get; }
        public override ModelPropertyCollection Properties { get; } = new(Enumerable.Empty<ModelMetadata>());
        public override IPropertyFilterProvider? PropertyFilterProvider { get; }
        public override Func<object, object>? PropertyGetter { get; }
        public override Action<object, object?>? PropertySetter { get; }
        public override bool ShowForDisplay { get; }
        public override bool ShowForEdit { get; }
        public override string? SimpleDisplayProperty { get; }
        public override string? TemplateHint { get; }
        public override bool ValidateChildren { get; }
        public override IReadOnlyList<object> ValidatorMetadata { get; } = Array.Empty<object>();
    }
    internal static class TypeHelper
    {
        /// <summary>
        /// Checks to see if a given type is compiler generated.
        /// <remarks>
        /// The compiler will annotate either the target type or the declaring type
        /// with the CompilerGenerated attribute. We walk up the declaring types until
        /// we find a CompilerGenerated attribute or declare the type as not compiler
        /// generated otherwise.
        /// </remarks>
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns><see langword="true" /> if <paramref name="type"/> is compiler generated.</returns>
        internal static bool IsCompilerGeneratedType(Type? type = null)
        {
            if (type is not null)
            {
                return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)) || IsCompilerGeneratedType(type.DeclaringType);
            }
            return false;
        }

        /// <summary>
        /// Checks to see if a given method is compiler generated.
        /// </summary>
        /// <param name="method">The method to evaluate.</param>
        /// <returns><see langword="true" /> if <paramref name="method"/> is compiler generated.</returns>
        internal static bool IsCompilerGeneratedMethod(MethodInfo method)
        {
            return Attribute.IsDefined(method, typeof(CompilerGeneratedAttribute)) || IsCompilerGeneratedType(method.DeclaringType);
        }
    }
    internal sealed class ParameterBindingMethodCache
    {
        private static readonly MethodInfo ConvertValueTaskMethod = typeof(ParameterBindingMethodCache).GetMethod(nameof(ConvertValueTask), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ConvertValueTaskOfNullableResultMethod = typeof(ParameterBindingMethodCache).GetMethod(nameof(ConvertValueTaskOfNullableResult), BindingFlags.NonPublic | BindingFlags.Static)!;

        internal static readonly ParameterExpression TempSourceStringExpr = Expression.Variable(typeof(string), "tempSourceString");
        internal static readonly ParameterExpression HttpContextExpr = Expression.Parameter(typeof(HttpContext), "httpContext");

        private readonly MethodInfo _enumTryParseMethod;

        // Since this is shared source, the cache won't be shared between RequestDelegateFactory and the ApiDescriptionProvider sadly :(
        private readonly ConcurrentDictionary<Type, Func<ParameterExpression, Expression>?> _stringMethodCallCache = new();
        private readonly ConcurrentDictionary<Type, (Func<ParameterInfo, Expression>?, int)> _bindAsyncMethodCallCache = new();

        // If IsDynamicCodeSupported is false, we can't use the static Enum.TryParse<T> since there's no easy way for
        // this code to generate the specific instantiation for any enums used
        public ParameterBindingMethodCache() : this(preferNonGenericEnumParseOverload: !RuntimeFeature.IsDynamicCodeSupported)
        {
        }

        // This is for testing
        public ParameterBindingMethodCache(bool preferNonGenericEnumParseOverload)
        {
            _enumTryParseMethod = GetEnumTryParseMethod(preferNonGenericEnumParseOverload);
        }

        public bool HasTryParseMethod(ParameterInfo parameter)
        {
            var nonNullableParameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            return FindTryParseMethod(nonNullableParameterType) is not null;
        }

        public bool HasBindAsyncMethod(ParameterInfo parameter) =>
            FindBindAsyncMethod(parameter).Expression is not null;

        public Func<ParameterExpression, Expression>? FindTryParseMethod(Type type)
        {
            Func<ParameterExpression, Expression>? Finder(Type type)
            {
                MethodInfo? methodInfo;

                if (type.IsEnum)
                {
                    if (_enumTryParseMethod.IsGenericMethod)
                    {
                        methodInfo = _enumTryParseMethod.MakeGenericMethod(type);

                        return (expression) => Expression.Call(methodInfo!, TempSourceStringExpr, expression);
                    }

                    return (expression) =>
                    {
                        var enumAsObject = Expression.Variable(typeof(object), "enumAsObject");
                        var success = Expression.Variable(typeof(bool), "success");

                        // object enumAsObject;
                        // bool success;
                        // success = Enum.TryParse(type, tempSourceString, out enumAsObject);
                        // parsedValue = success ? (Type)enumAsObject : default;
                        // return success;

                        return Expression.Block(new[] { success, enumAsObject },
                            Expression.Assign(success, Expression.Call(_enumTryParseMethod, Expression.Constant(type), TempSourceStringExpr, enumAsObject)),
                            Expression.Assign(expression,
                                Expression.Condition(success, Expression.Convert(enumAsObject, type), Expression.Default(type))),
                            success);
                    };

                }

                if (TryGetDateTimeTryParseMethod(type, out methodInfo))
                {
                    // We generate `DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces ` to
                    // support parsing types into the UTC timezone for DateTime. We don't assume the timezone
                    // on the original value which will cause the parser to set the `Kind` property on the
                    // `DateTime` as `Unspecified` indicating that it was parsed from an ambiguous timezone.
                    //
                    // `DateTimeOffset`s are always in UTC and don't allow specifying an `Unspecific` kind.
                    // For this, we always assume that the original value is already in UTC to avoid resolving
                    // the offset incorrectly depending on the timezone of the machine. We don't bother mapping
                    // it to UTC in this case. In the event that the original timestamp is not in UTC, it's offset
                    // value will be maintained.
                    //
                    // DateOnly and TimeOnly types do not support conversion to Utc so we
                    // default to `DateTimeStyles.AllowWhiteSpaces`.
                    var dateTimeStyles = type switch
                    {
                        Type t when t == typeof(DateTime) => DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces,
                        Type t when t == typeof(DateTimeOffset) => DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces,
                        _ => DateTimeStyles.AllowWhiteSpaces
                    };

                    return (expression) => Expression.Call(
                        methodInfo!,
                        TempSourceStringExpr,
                        Expression.Constant(CultureInfo.InvariantCulture),
                        Expression.Constant(dateTimeStyles),
                        expression);
                }

                if (TryGetNumberStylesTryGetMethod(type, out methodInfo, out var numberStyle))
                {
                    return (expression) => Expression.Call(
                        methodInfo!,
                        TempSourceStringExpr,
                        Expression.Constant(numberStyle),
                        Expression.Constant(CultureInfo.InvariantCulture),
                        expression);
                }

                methodInfo = GetStaticMethodFromHierarchy(type, "TryParse", new[] { typeof(string), typeof(IFormatProvider), type.MakeByRefType() }, ValidateReturnType);

                if (methodInfo is not null)
                {
                    return (expression) => Expression.Call(
                        methodInfo,
                        TempSourceStringExpr,
                        Expression.Constant(CultureInfo.InvariantCulture),
                        expression);
                }

                methodInfo = GetStaticMethodFromHierarchy(type, "TryParse", new[] { typeof(string), type.MakeByRefType() }, ValidateReturnType);

                if (methodInfo is not null)
                {
                    return (expression) => Expression.Call(methodInfo, TempSourceStringExpr, expression);
                }

                if (GetAnyMethodFromHierarchy(type, "TryParse") is MethodInfo invalidMethod)
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"TryParse method found on {TypeNameHelper.GetTypeDisplayName(type, fullName: false)} with incorrect format. Must be a static method with format");
                    stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"bool TryParse(string, IFormatProvider, out {TypeNameHelper.GetTypeDisplayName(type, fullName: false)})");
                    stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"bool TryParse(string, out {TypeNameHelper.GetTypeDisplayName(type, fullName: false)})");
                    stringBuilder.AppendLine("but found");
                    stringBuilder.Append(invalidMethod.IsStatic ? "static " : "not-static ");
                    stringBuilder.Append(invalidMethod.ToString());

                    throw new InvalidOperationException(stringBuilder.ToString());
                }

                return null;

                static bool ValidateReturnType(MethodInfo methodInfo)
                {
                    return methodInfo.ReturnType.Equals(typeof(bool));
                }
            }

            return _stringMethodCallCache.GetOrAdd(type, Finder);
        }

        public (Expression? Expression, int ParamCount) FindBindAsyncMethod(ParameterInfo parameter)
        {
            static (Func<ParameterInfo, Expression>?, int) Finder(Type nonNullableParameterType)
            {
                var hasParameterInfo = true;
                // There should only be one BindAsync method with these parameters since C# does not allow overloading on return type.
                var methodInfo = GetStaticMethodFromHierarchy(nonNullableParameterType, "BindAsync", new[] { typeof(HttpContext), typeof(ParameterInfo) }, ValidateReturnType);
                if (methodInfo is null)
                {
                    hasParameterInfo = false;
                    methodInfo = GetStaticMethodFromHierarchy(nonNullableParameterType, "BindAsync", new[] { typeof(HttpContext) }, ValidateReturnType);
                }

                // We're looking for a method with the following signatures:
                // public static ValueTask<{type}> BindAsync(HttpContext context, ParameterInfo parameter)
                // public static ValueTask<Nullable<{type}>> BindAsync(HttpContext context, ParameterInfo parameter)
                if (methodInfo is not null)
                {
                    var valueTaskResultType = methodInfo.ReturnType.GetGenericArguments()[0];

                    // ValueTask<{type}>?
                    if (valueTaskResultType == nonNullableParameterType)
                    {
                        return ((parameter) =>
                        {
                            MethodCallExpression typedCall;
                            if (hasParameterInfo)
                            {
                                // parameter is being intentionally shadowed. We never want to use the outer ParameterInfo inside
                                // this Func because the ParameterInfo varies after it's been cached for a given parameter type.
                                typedCall = Expression.Call(methodInfo, HttpContextExpr, Expression.Constant(parameter));
                            }
                            else
                            {
                                typedCall = Expression.Call(methodInfo, HttpContextExpr);
                            }
                            return Expression.Call(ConvertValueTaskMethod.MakeGenericMethod(nonNullableParameterType), typedCall);
                        }, hasParameterInfo ? 2 : 1);
                    }
                    // ValueTask<Nullable<{type}>>?
                    else if (valueTaskResultType.IsGenericType &&
                             valueTaskResultType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                             valueTaskResultType.GetGenericArguments()[0] == nonNullableParameterType)
                    {
                        return ((parameter) =>
                        {
                            MethodCallExpression typedCall;
                            if (hasParameterInfo)
                            {
                                // parameter is being intentionally shadowed. We never want to use the outer ParameterInfo inside
                                // this Func because the ParameterInfo varies after it's been cached for a given parameter type.
                                typedCall = Expression.Call(methodInfo, HttpContextExpr, Expression.Constant(parameter));
                            }
                            else
                            {
                                typedCall = Expression.Call(methodInfo, HttpContextExpr);
                            }
                            return Expression.Call(ConvertValueTaskOfNullableResultMethod.MakeGenericMethod(nonNullableParameterType), typedCall);
                        }, hasParameterInfo ? 2 : 1);
                    }
                }

                if (GetAnyMethodFromHierarchy(nonNullableParameterType, "BindAsync") is MethodInfo invalidBindMethod)
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"BindAsync method found on {TypeNameHelper.GetTypeDisplayName(nonNullableParameterType, fullName: false)} with incorrect format. Must be a static method with format");
                    stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"ValueTask<{TypeNameHelper.GetTypeDisplayName(nonNullableParameterType, fullName: false)}> BindAsync(HttpContext context, ParameterInfo parameter)");
                    stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"ValueTask<{TypeNameHelper.GetTypeDisplayName(nonNullableParameterType, fullName: false)}> BindAsync(HttpContext context)");
                    stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"ValueTask<{TypeNameHelper.GetTypeDisplayName(nonNullableParameterType, fullName: false)}?> BindAsync(HttpContext context, ParameterInfo parameter)");
                    stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"ValueTask<{TypeNameHelper.GetTypeDisplayName(nonNullableParameterType, fullName: false)}?> BindAsync(HttpContext context)");
                    stringBuilder.AppendLine("but found");
                    stringBuilder.Append(invalidBindMethod.IsStatic ? "static " : "not-static");
                    stringBuilder.Append(invalidBindMethod.ToString());

                    throw new InvalidOperationException(stringBuilder.ToString());
                }

                return (null, 0);
            }

            var nonNullableParameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            var (method, paramCount) = _bindAsyncMethodCallCache.GetOrAdd(nonNullableParameterType, Finder);
            return (method?.Invoke(parameter), paramCount);

            static bool ValidateReturnType(MethodInfo methodInfo)
            {
                return methodInfo.ReturnType.IsGenericType &&
                    methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>);
            }
        }

        private static MethodInfo? GetStaticMethodFromHierarchy(Type type, string name, Type[] parameterTypes, Func<MethodInfo, bool> validateReturnType)
        {
            bool IsMatch(MethodInfo? method) => method is not null && !method.IsAbstract && validateReturnType(method);

            var methodInfo = type.GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy, parameterTypes);

            if (IsMatch(methodInfo))
            {
                return methodInfo;
            }

            var candidateInterfaceMethodInfo = default(MethodInfo);

            // Check all interfaces for implementations. Fail if there are duplicates.
            foreach (var implementedInterface in type.GetInterfaces())
            {
                var interfaceMethod = implementedInterface.GetMethod(name, BindingFlags.Public | BindingFlags.Static, parameterTypes);

                if (IsMatch(interfaceMethod))
                {
                    if (candidateInterfaceMethodInfo is not null)
                    {
                        throw new InvalidOperationException($"{TypeNameHelper.GetTypeDisplayName(type, fullName: false)} implements multiple interfaces defining a static {interfaceMethod} method causing ambiguity.");
                    }

                    candidateInterfaceMethodInfo = interfaceMethod;
                }
            }

            return candidateInterfaceMethodInfo;
        }

        private static MethodInfo? GetAnyMethodFromHierarchy(Type type, string name)
        {
            // Find first incorrectly formatted method
            var methodInfo = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .FirstOrDefault(methodInfo => methodInfo.Name == name);

            if (methodInfo is not null)
            {
                return methodInfo;
            }

            foreach (var implementedInterface in type.GetInterfaces())
            {
                var interfaceMethod = implementedInterface.GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

                if (interfaceMethod is not null)
                {
                    return interfaceMethod;
                }
            }

            return null;
        }

        private static MethodInfo GetEnumTryParseMethod(bool preferNonGenericEnumParseOverload)
        {
            MethodInfo? methodInfo = null;

            if (preferNonGenericEnumParseOverload)
            {
                methodInfo = typeof(Enum).GetMethod(
                                nameof(Enum.TryParse),
                                BindingFlags.Public | BindingFlags.Static,
                                new[] { typeof(Type), typeof(string), typeof(object).MakeByRefType() });
            }
            else
            {
                methodInfo = typeof(Enum).GetMethod(
                               nameof(Enum.TryParse),
                               genericParameterCount: 1,
                               new[] { typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType() });
            }

            if (methodInfo is null)
            {
                Debug.Fail("No suitable System.Enum.TryParse method found.");
                throw new MissingMethodException("No suitable System.Enum.TryParse method found.");
            }

            return methodInfo!;
        }

        private static bool TryGetDateTimeTryParseMethod(Type type, [NotNullWhen(true)] out MethodInfo? methodInfo)
        {
            methodInfo = null;

            if (type == typeof(DateTime))
            {
                methodInfo = typeof(DateTime).GetMethod(
                     nameof(DateTime.TryParse),
                     BindingFlags.Public | BindingFlags.Static,
                     new[] { typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles), typeof(DateTime).MakeByRefType() });
            }
            else if (type == typeof(DateTimeOffset))
            {
                methodInfo = typeof(DateTimeOffset).GetMethod(
                     nameof(DateTimeOffset.TryParse),
                     BindingFlags.Public | BindingFlags.Static,
                     new[] { typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles), typeof(DateTimeOffset).MakeByRefType() });
            }
            else if (type == typeof(DateOnly))
            {
                methodInfo = typeof(DateOnly).GetMethod(
                     nameof(DateOnly.TryParse),
                     BindingFlags.Public | BindingFlags.Static,
                     new[] { typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles), typeof(DateOnly).MakeByRefType() });
            }
            else if (type == typeof(TimeOnly))
            {
                methodInfo = typeof(TimeOnly).GetMethod(
                     nameof(TimeOnly.TryParse),
                     BindingFlags.Public | BindingFlags.Static,
                     new[] { typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles), typeof(TimeOnly).MakeByRefType() });
            }

            return methodInfo != null;
        }

        private static bool TryGetNumberStylesTryGetMethod(Type type, [NotNullWhen(true)] out MethodInfo? method, [NotNullWhen(true)] out NumberStyles? numberStyles)
        {
            method = null;
            numberStyles = NumberStyles.Integer;

            if (type == typeof(long))
            {
                method = typeof(long).GetMethod(
                          nameof(long.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(long).MakeByRefType() });
            }
            else if (type == typeof(ulong))
            {
                method = typeof(ulong).GetMethod(
                          nameof(ulong.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(ulong).MakeByRefType() });
            }
            else if (type == typeof(int))
            {
                method = typeof(int).GetMethod(
                          nameof(int.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(int).MakeByRefType() });
            }
            else if (type == typeof(uint))
            {
                method = typeof(uint).GetMethod(
                          nameof(uint.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(uint).MakeByRefType() });
            }
            else if (type == typeof(short))
            {
                method = typeof(short).GetMethod(
                          nameof(short.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(short).MakeByRefType() });
            }
            else if (type == typeof(ushort))
            {
                method = typeof(ushort).GetMethod(
                          nameof(ushort.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(ushort).MakeByRefType() });
            }
            else if (type == typeof(byte))
            {
                method = typeof(byte).GetMethod(
                          nameof(byte.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(byte).MakeByRefType() });
            }
            else if (type == typeof(sbyte))
            {
                method = typeof(sbyte).GetMethod(
                          nameof(sbyte.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(sbyte).MakeByRefType() });
            }
            else if (type == typeof(double))
            {
                method = typeof(double).GetMethod(
                          nameof(double.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(double).MakeByRefType() });

                numberStyles = NumberStyles.AllowThousands | NumberStyles.Float;
            }
            else if (type == typeof(float))
            {
                method = typeof(float).GetMethod(
                          nameof(float.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(float).MakeByRefType() });

                numberStyles = NumberStyles.AllowThousands | NumberStyles.Float;
            }
            else if (type == typeof(Half))
            {
                method = typeof(Half).GetMethod(
                          nameof(Half.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(Half).MakeByRefType() });

                numberStyles = NumberStyles.AllowThousands | NumberStyles.Float;
            }
            else if (type == typeof(decimal))
            {
                method = typeof(decimal).GetMethod(
                          nameof(decimal.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(decimal).MakeByRefType() });

                numberStyles = NumberStyles.Number;
            }
            else if (type == typeof(IntPtr))
            {
                method = typeof(IntPtr).GetMethod(
                          nameof(IntPtr.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(IntPtr).MakeByRefType() });
            }
            else if (type == typeof(BigInteger))
            {
                method = typeof(BigInteger).GetMethod(
                          nameof(BigInteger.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(BigInteger).MakeByRefType() });
            }

            return method != null;
        }

        private static ValueTask<object?> ConvertValueTask<T>(ValueTask<T> typedValueTask)
        {
            if (typedValueTask.IsCompletedSuccessfully)
            {
                var result = typedValueTask.GetAwaiter().GetResult();
                return new ValueTask<object?>(result);
            }

            static async ValueTask<object?> ConvertAwaited(ValueTask<T> typedValueTask) => await typedValueTask;
            return ConvertAwaited(typedValueTask);
        }

        private static ValueTask<object?> ConvertValueTaskOfNullableResult<T>(ValueTask<Nullable<T>> typedValueTask) where T : struct
        {
            if (typedValueTask.IsCompletedSuccessfully)
            {
                var result = typedValueTask.GetAwaiter().GetResult();
                return new ValueTask<object?>(result);
            }

            static async ValueTask<object?> ConvertAwaited(ValueTask<Nullable<T>> typedValueTask) => await typedValueTask;
            return ConvertAwaited(typedValueTask);
        }
    }
    internal static class TypeNameHelper
    {
        private const char DefaultNestedTypeDelimiter = '+';

        private static readonly Dictionary<Type, string> _builtInTypeNames = new Dictionary<Type, string>
        {
            { typeof(void), "void" },
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(object), "object" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(string), "string" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(ushort), "ushort" }
        };

        [return: NotNullIfNotNull("item")]
        public static string? GetTypeDisplayName(object? item, bool fullName = true)
        {
            return item == null ? null : GetTypeDisplayName(item.GetType(), fullName);
        }

        /// <summary>
        /// Pretty print a type name.
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <param name="fullName"><c>true</c> to print a fully qualified name.</param>
        /// <param name="includeGenericParameterNames"><c>true</c> to include generic parameter names.</param>
        /// <param name="includeGenericParameters"><c>true</c> to include generic parameters.</param>
        /// <param name="nestedTypeDelimiter">Character to use as a delimiter in nested type names</param>
        /// <returns>The pretty printed type name.</returns>
        public static string GetTypeDisplayName(Type type, bool fullName = true, bool includeGenericParameterNames = false, bool includeGenericParameters = true, char nestedTypeDelimiter = DefaultNestedTypeDelimiter)
        {
            var builder = new StringBuilder();
            ProcessType(builder, type, new DisplayNameOptions(fullName, includeGenericParameterNames, includeGenericParameters, nestedTypeDelimiter));
            return builder.ToString();
        }

        private static void ProcessType(StringBuilder builder, Type type, in DisplayNameOptions options)
        {
            if (type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments();
                ProcessGenericType(builder, type, genericArguments, genericArguments.Length, options);
            }
            else if (type.IsArray)
            {
                ProcessArrayType(builder, type, options);
            }
            else if (_builtInTypeNames.TryGetValue(type, out var builtInName))
            {
                builder.Append(builtInName);
            }
            else if (type.IsGenericParameter)
            {
                if (options.IncludeGenericParameterNames)
                {
                    builder.Append(type.Name);
                }
            }
            else
            {
                var name = options.FullName ? type.FullName! : type.Name;
                builder.Append(name);

                if (options.NestedTypeDelimiter != DefaultNestedTypeDelimiter)
                {
                    builder.Replace(DefaultNestedTypeDelimiter, options.NestedTypeDelimiter, builder.Length - name.Length, name.Length);
                }
            }
        }

        private static void ProcessArrayType(StringBuilder builder, Type type, in DisplayNameOptions options)
        {
            var innerType = type;
            while (innerType.IsArray)
            {
                innerType = innerType.GetElementType()!;
            }

            ProcessType(builder, innerType, options);

            while (type.IsArray)
            {
                builder.Append('[');
                builder.Append(',', type.GetArrayRank() - 1);
                builder.Append(']');
                type = type.GetElementType()!;
            }
        }

        private static void ProcessGenericType(StringBuilder builder, Type type, Type[] genericArguments, int length, in DisplayNameOptions options)
        {
            var offset = 0;
            if (type.IsNested)
            {
                offset = type.DeclaringType!.GetGenericArguments().Length;
            }

            if (options.FullName)
            {
                if (type.IsNested)
                {
                    ProcessGenericType(builder, type.DeclaringType!, genericArguments, offset, options);
                    builder.Append(options.NestedTypeDelimiter);
                }
                else if (!string.IsNullOrEmpty(type.Namespace))
                {
                    builder.Append(type.Namespace);
                    builder.Append('.');
                }
            }

            var genericPartIndex = type.Name.IndexOf('`');
            if (genericPartIndex <= 0)
            {
                builder.Append(type.Name);
                return;
            }

            builder.Append(type.Name, 0, genericPartIndex);

            if (options.IncludeGenericParameters)
            {
                builder.Append('<');
                for (var i = offset; i < length; i++)
                {
                    ProcessType(builder, genericArguments[i], options);
                    if (i + 1 == length)
                    {
                        continue;
                    }

                    builder.Append(',');
                    if (options.IncludeGenericParameterNames || !genericArguments[i + 1].IsGenericParameter)
                    {
                        builder.Append(' ');
                    }
                }
                builder.Append('>');
            }
        }

        private readonly struct DisplayNameOptions
        {
            public DisplayNameOptions(bool fullName, bool includeGenericParameterNames, bool includeGenericParameters, char nestedTypeDelimiter)
            {
                FullName = fullName;
                IncludeGenericParameters = includeGenericParameters;
                IncludeGenericParameterNames = includeGenericParameterNames;
                NestedTypeDelimiter = nestedTypeDelimiter;
            }

            public bool FullName { get; }

            public bool IncludeGenericParameters { get; }

            public bool IncludeGenericParameterNames { get; }

            public char NestedTypeDelimiter { get; }
        }
    }

    internal class ApiResponseTypeProvider
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IActionResultTypeMapper _mapper;
        private readonly MvcOptions _mvcOptions;

        public ApiResponseTypeProvider(
            IModelMetadataProvider modelMetadataProvider,
            IActionResultTypeMapper mapper,
            MvcOptions mvcOptions)
        {
            _modelMetadataProvider = modelMetadataProvider;
            _mapper = mapper;
            _mvcOptions = mvcOptions;
        }

        public ICollection<ApiResponseType> GetApiResponseTypes(ControllerActionDescriptor action)
        {
            // We only provide response info if we can figure out a type that is a user-data type.
            // Void /Task object/IActionResult will result in no data.
            var declaredReturnType = GetDeclaredReturnType(action);

            var runtimeReturnType = GetRuntimeReturnType(declaredReturnType);

            var responseMetadataAttributes = GetResponseMetadataAttributes(action);
            if (!HasSignificantMetadataProvider(responseMetadataAttributes) &&
                action.Properties.TryGetValue(typeof(ApiConventionResult), out var result))
            {
                // Action does not have any conventions. Use conventions on it if present.
                var apiConventionResult = (ApiConventionResult)result!;
                responseMetadataAttributes.AddRange(apiConventionResult.ResponseMetadataProviders);
            }

            var defaultErrorType = typeof(void);
            if (action.Properties.TryGetValue(typeof(ProducesErrorResponseTypeAttribute), out result))
            {
                defaultErrorType = ((ProducesErrorResponseTypeAttribute)result!).Type;
            }

            var apiResponseTypes = GetApiResponseTypes(responseMetadataAttributes, runtimeReturnType, defaultErrorType);
            return apiResponseTypes;
        }

        private static List<IApiResponseMetadataProvider> GetResponseMetadataAttributes(ControllerActionDescriptor action)
        {
            if (action.FilterDescriptors == null)
            {
                return new List<IApiResponseMetadataProvider>();
            }

            // This technique for enumerating filters will intentionally ignore any filter that is an IFilterFactory
            // while searching for a filter that implements IApiResponseMetadataProvider.
            //
            // The workaround for that is to implement the metadata interface on the IFilterFactory.
            return action.FilterDescriptors
                .Select(fd => fd.Filter)
                .OfType<IApiResponseMetadataProvider>()
                .ToList();
        }

        private ICollection<ApiResponseType> GetApiResponseTypes(
           IReadOnlyList<IApiResponseMetadataProvider> responseMetadataAttributes,
           Type? type,
           Type defaultErrorType)
        {
            var contentTypes = new MediaTypeCollection();
            var responseTypeMetadataProviders = _mvcOptions.OutputFormatters.OfType<IApiResponseTypeMetadataProvider>();

            var responseTypes = ReadResponseMetadata(
                responseMetadataAttributes,
                type,
                defaultErrorType,
                contentTypes,
                responseTypeMetadataProviders);

            // Set the default status only when no status has already been set explicitly
            if (responseTypes.Count == 0 && type != null)
            {
                responseTypes.Add(new ApiResponseType
                {
                    StatusCode = StatusCodes.Status200OK,
                    Type = type,
                });
            }

            if (contentTypes.Count == 0)
            {
                // None of the IApiResponseMetadataProvider specified a content type. This is common for actions that
                // specify one or more ProducesResponseType but no ProducesAttribute. In this case, formatters will participate in conneg
                // and respond to the incoming request.
                // Querying IApiResponseTypeMetadataProvider.GetSupportedContentTypes with "null" should retrieve all supported
                // content types that each formatter may respond in.
                contentTypes.Add((string)null!);
            }

            foreach (var apiResponse in responseTypes)
            {
                CalculateResponseFormatForType(apiResponse, contentTypes, responseTypeMetadataProviders, _modelMetadataProvider);
            }

            return responseTypes;
        }

        // Shared with EndpointMetadataApiDescriptionProvider
        internal static List<ApiResponseType> ReadResponseMetadata(
            IReadOnlyList<IApiResponseMetadataProvider> responseMetadataAttributes,
            Type? type,
            Type defaultErrorType,
            MediaTypeCollection contentTypes,
            IEnumerable<IApiResponseTypeMetadataProvider>? responseTypeMetadataProviders = null,
            IModelMetadataProvider? modelMetadataProvider = null)
        {
            var results = new Dictionary<int, ApiResponseType>();

            // Get the content type that the action explicitly set to support.
            // Walk through all 'filter' attributes in order, and allow each one to see or override
            // the results of the previous ones. This is similar to the execution path for content-negotiation.
            if (responseMetadataAttributes != null)
            {
                foreach (var metadataAttribute in responseMetadataAttributes)
                {
                    // All ProducesXAttributes, except for ProducesResponseTypeAttribute do
                    // not allow multiple instances on the same method/class/etc. For those
                    // scenarios, the `SetContentTypes` method on the attribute continuously
                    // clears out more general content types in favor of more specific ones
                    // since we iterate through the attributes in order. For example, if a
                    // Produces exists on both a controller and an action within the controller,
                    // we favor the definition in the action. This is a semantic that does not
                    // apply to ProducesResponseType, which allows multiple instances on an target.
                    if (metadataAttribute is not ProducesResponseTypeAttribute)
                    {
                        metadataAttribute.SetContentTypes(contentTypes);
                    }

                    var statusCode = metadataAttribute.StatusCode;

                    var apiResponseType = new ApiResponseType
                    {
                        Type = metadataAttribute.Type,
                        StatusCode = statusCode,
                        IsDefaultResponse = metadataAttribute is IApiDefaultResponseMetadataProvider,
                    };

                    if (apiResponseType.Type == typeof(void))
                    {
                        if (type != null && (statusCode == StatusCodes.Status200OK || statusCode == StatusCodes.Status201Created))
                        {
                            // ProducesResponseTypeAttribute's constructor defaults to setting "Type" to void when no value is specified.
                            // In this event, use the action's return type for 200 or 201 status codes. This lets you decorate an action with a
                            // [ProducesResponseType(201)] instead of [ProducesResponseType(typeof(Person), 201] when typeof(Person) can be inferred
                            // from the return type.
                            apiResponseType.Type = type;
                        }
                        else if (IsClientError(statusCode))
                        {
                            // Determine whether or not the type was provided by the user. If so, favor it over the default
                            // error type for 4xx client errors if no response type is specified..
                            var setByDefault = metadataAttribute is ProducesResponseTypeAttribute { IsResponseTypeSetByDefault: true };
                            apiResponseType.Type = setByDefault ? defaultErrorType : apiResponseType.Type;
                        }
                        else if (apiResponseType.IsDefaultResponse)
                        {
                            apiResponseType.Type = defaultErrorType;
                        }
                    }

                    // We special case the handling of ProcuesResponseTypeAttributes since
                    // multiple ProducesResponseTypeAttributes are permitted on a single
                    // action/controller/etc. In that scenario, instead of picking the most-specific
                    // set of content types (like we do with the Produces attribute above) we process
                    // the content types for each attribute independently.
                    if (metadataAttribute is ProducesResponseTypeAttribute)
                    {
                        var attributeContentTypes = new MediaTypeCollection();
                        metadataAttribute.SetContentTypes(attributeContentTypes);
                        CalculateResponseFormatForType(apiResponseType, attributeContentTypes, responseTypeMetadataProviders, modelMetadataProvider);
                    }

                    if (apiResponseType.Type != null)
                    {
                        results[apiResponseType.StatusCode] = apiResponseType;
                    }
                }
            }

            return results.Values.ToList();
        }

        // Shared with EndpointMetadataApiDescriptionProvider
        internal static void CalculateResponseFormatForType(ApiResponseType apiResponse, MediaTypeCollection declaredContentTypes, IEnumerable<IApiResponseTypeMetadataProvider>? responseTypeMetadataProviders, IModelMetadataProvider? modelMetadataProvider)
        {
            // If response formats have already been calculate for this type,
            // then exit early. This avoids populating the ApiResponseFormat for
            // types that have already been handled, specifically ProducesResponseTypes.
            if (apiResponse.ApiResponseFormats.Count > 0)
            {
                return;
            }

            // Given the content-types that were declared for this action, determine the formatters that support the content-type for the given
            // response type.
            // 1. Responses that do not specify an type do not have any associated content-type. This usually is meant for status-code only responses such
            // as return NotFound();
            // 2. When a type is specified, use GetSupportedContentTypes to expand wildcards and get the range of content-types formatters support.
            // 3. When no formatter supports the specified content-type, use the user specified value as is. This is useful in actions where the user
            // dictates the content-type.
            // e.g. [Produces("application/pdf")] Action() => FileStream("somefile.pdf", "application/pdf");
            var responseType = apiResponse.Type;
            if (responseType == null || responseType == typeof(void))
            {
                return;
            }

            apiResponse.ModelMetadata = modelMetadataProvider?.GetMetadataForType(responseType);

            foreach (var contentType in declaredContentTypes)
            {
                var isSupportedContentType = false;

                if (responseTypeMetadataProviders != null)
                {
                    foreach (var responseTypeMetadataProvider in responseTypeMetadataProviders)
                    {
                        var formatterSupportedContentTypes = responseTypeMetadataProvider.GetSupportedContentTypes(
                            contentType,
                            responseType);

                        if (formatterSupportedContentTypes == null)
                        {
                            continue;
                        }

                        isSupportedContentType = true;

                        foreach (var formatterSupportedContentType in formatterSupportedContentTypes)
                        {
                            apiResponse.ApiResponseFormats.Add(new ApiResponseFormat
                            {
                                Formatter = (IOutputFormatter)responseTypeMetadataProvider,
                                MediaType = formatterSupportedContentType,
                            });
                        }
                    }
                }



                if (!isSupportedContentType && contentType != null)
                {
                    // No output formatter was found that supports this content type. Add the user specified content type as-is to the result.
                    apiResponse.ApiResponseFormats.Add(new ApiResponseFormat
                    {
                        MediaType = contentType,
                    });
                }
            }
        }

        private Type? GetDeclaredReturnType(ControllerActionDescriptor action)
        {
            var declaredReturnType = action.MethodInfo.ReturnType;
            if (declaredReturnType == typeof(void) ||
                declaredReturnType == typeof(Task) ||
                declaredReturnType == typeof(ValueTask))
            {
                return typeof(void);
            }

            // Unwrap the type if it's a Task<T>. The Task (non-generic) case was already handled.
            var unwrappedType = declaredReturnType;
            if (declaredReturnType.IsGenericType &&
                (declaredReturnType.GetGenericTypeDefinition() == typeof(Task<>) || declaredReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
            {
                unwrappedType = declaredReturnType.GetGenericArguments()[0];
            }

            // If the method is declared to return IActionResult or a derived class, that information
            // isn't valuable to the formatter.
            if (typeof(IActionResult).IsAssignableFrom(unwrappedType))
            {
                return null;
            }

            // If we get here, the type should be a user-defined data type or an envelope type
            // like ActionResult<T>. The mapper service will unwrap envelopes.
            unwrappedType = _mapper.GetResultDataType(unwrappedType);
            return unwrappedType;
        }

        private Type? GetRuntimeReturnType(Type? declaredReturnType)
        {
            // If we get here, then a filter didn't give us an answer, so we need to figure out if we
            // want to use the declared return type.
            //
            // We've already excluded Task, void, and IActionResult at this point.
            //
            // If the action might return any object, then assume we don't know anything about it.
            if (declaredReturnType == typeof(object))
            {
                return null;
            }

            return declaredReturnType;
        }

        private static bool IsClientError(int statusCode)
        {
            return statusCode >= 400 && statusCode < 500;
        }

        private static bool HasSignificantMetadataProvider(IReadOnlyList<IApiResponseMetadataProvider> providers)
        {
            for (var i = 0; i < providers.Count; i++)
            {
                var provider = providers[i];

                if (provider is ProducesAttribute producesAttribute && producesAttribute.Type is null)
                {
                    // ProducesAttribute that does not specify type is considered not significant.
                    continue;
                }

                // Any other IApiResponseMetadataProvider is considered significant
                return true;
            }

            return false;
        }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ProducesResponseTypeAttribute : Attribute, IApiResponseMetadataProvider
    {
        private readonly MediaTypeCollection? _contentTypes;

        /// <summary>
        /// Initializes an instance of <see cref="ProducesResponseTypeAttribute"/>.
        /// </summary>
        /// <param name="statusCode">The HTTP response status code.</param>
        public ProducesResponseTypeAttribute(int statusCode)
            : this(typeof(void), statusCode)
        {
            IsResponseTypeSetByDefault = true;
        }

        /// <summary>
        /// Initializes an instance of <see cref="ProducesResponseTypeAttribute"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of object that is going to be written in the response.</param>
        /// <param name="statusCode">The HTTP response status code.</param>
        public ProducesResponseTypeAttribute(Type type, int statusCode)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            StatusCode = statusCode;
            IsResponseTypeSetByDefault = false;
        }

        /// <summary>
        /// Initializes an instance of <see cref="ProducesResponseTypeAttribute"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of object that is going to be written in the response.</param>
        /// <param name="statusCode">The HTTP response status code.</param>
        /// <param name="contentType">The content type associated with the response.</param>
        /// <param name="additionalContentTypes">Additional content types supported by the response.</param>
        public ProducesResponseTypeAttribute(Type type, int statusCode, string contentType, params string[] additionalContentTypes)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            Type = type ?? throw new ArgumentNullException(nameof(type));
            StatusCode = statusCode;
            IsResponseTypeSetByDefault = false;

            MediaTypeHeaderValue.Parse(contentType);
            for (var i = 0; i < additionalContentTypes.Length; i++)
            {
                MediaTypeHeaderValue.Parse(additionalContentTypes[i]);
            }

            _contentTypes = GetContentTypes(contentType, additionalContentTypes);
        }

        /// <summary>
        /// Gets or sets the type of the value returned by an action.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code of the response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Used to distinguish a `Type` set by default in the constructor versus
        /// one provided by the user.
        ///
        /// When <see langword="false"/>, then <see cref="Type"/> is set by user.
        ///
        /// When <see langword="true"/>, then <see cref="Type"/> is set by by
        /// default in the constructor
        /// </summary>
        /// <value></value>
        internal bool IsResponseTypeSetByDefault { get; }

        // Internal for testing
        internal MediaTypeCollection? ContentTypes => _contentTypes;

        /// <inheritdoc />
        void IApiResponseMetadataProvider.SetContentTypes(MediaTypeCollection contentTypes)
        {
            if (_contentTypes is not null)
            {
                contentTypes.Clear();
                foreach (var contentType in _contentTypes)
                {
                    contentTypes.Add(contentType);
                }
            }
        }

        private static MediaTypeCollection GetContentTypes(string contentType, string[] additionalContentTypes)
        {
            var completeContentTypes = new List<string>(additionalContentTypes.Length + 1);
            completeContentTypes.Add(contentType);
            completeContentTypes.AddRange(additionalContentTypes);
            MediaTypeCollection contentTypes = new();
            foreach (var type in completeContentTypes)
            {
                var mediaType = new MediaType(type);
                if (mediaType.HasWildcard)
                {
                    throw new InvalidOperationException("");
                }

                contentTypes.Add(type);
            }

            return contentTypes;
        }
    }
    internal sealed class EndpointParameterDescriptor : ParameterDescriptor, IParameterInfoParameterDescriptor
    {
        public ParameterInfo ParameterInfo { get; set; } = default!;
    }
    internal readonly struct AwaitableInfo
    {
        public Type AwaiterType { get; }
        public PropertyInfo AwaiterIsCompletedProperty { get; }
        public MethodInfo AwaiterGetResultMethod { get; }
        public MethodInfo AwaiterOnCompletedMethod { get; }
        public MethodInfo AwaiterUnsafeOnCompletedMethod { get; }
        public Type ResultType { get; }
        public MethodInfo GetAwaiterMethod { get; }

        public AwaitableInfo(
            Type awaiterType,
            PropertyInfo awaiterIsCompletedProperty,
            MethodInfo awaiterGetResultMethod,
            MethodInfo awaiterOnCompletedMethod,
            MethodInfo awaiterUnsafeOnCompletedMethod,
            Type resultType,
            MethodInfo getAwaiterMethod)
        {
            AwaiterType = awaiterType;
            AwaiterIsCompletedProperty = awaiterIsCompletedProperty;
            AwaiterGetResultMethod = awaiterGetResultMethod;
            AwaiterOnCompletedMethod = awaiterOnCompletedMethod;
            AwaiterUnsafeOnCompletedMethod = awaiterUnsafeOnCompletedMethod;
            ResultType = resultType;
            GetAwaiterMethod = getAwaiterMethod;
        }

        public static bool IsTypeAwaitable(Type type, out AwaitableInfo awaitableInfo)
        {
            // Based on Roslyn code: http://source.roslyn.io/#Microsoft.CodeAnalysis.Workspaces/Shared/Extensions/ISymbolExtensions.cs,db4d48ba694b9347

            // Awaitable must have method matching "object GetAwaiter()"
            var getAwaiterMethod = type.GetRuntimeMethods().FirstOrDefault(m =>
                m.Name.Equals("GetAwaiter", StringComparison.OrdinalIgnoreCase)
                && m.GetParameters().Length == 0
                && m.ReturnType != null);
            if (getAwaiterMethod == null)
            {
                awaitableInfo = default(AwaitableInfo);
                return false;
            }

            var awaiterType = getAwaiterMethod.ReturnType;

            // Awaiter must have property matching "bool IsCompleted { get; }"
            var isCompletedProperty = awaiterType.GetRuntimeProperties().FirstOrDefault(p =>
                p.Name.Equals("IsCompleted", StringComparison.OrdinalIgnoreCase)
                && p.PropertyType == typeof(bool)
                && p.GetMethod != null);
            if (isCompletedProperty == null)
            {
                awaitableInfo = default(AwaitableInfo);
                return false;
            }

            // Awaiter must implement INotifyCompletion
            var awaiterInterfaces = awaiterType.GetInterfaces();
            var implementsINotifyCompletion = awaiterInterfaces.Any(t => t == typeof(INotifyCompletion));
            if (!implementsINotifyCompletion)
            {
                awaitableInfo = default(AwaitableInfo);
                return false;
            }

            // INotifyCompletion supplies a method matching "void OnCompleted(Action action)"
            var onCompletedMethod = typeof(INotifyCompletion).GetRuntimeMethods().Single(m =>
                m.Name.Equals("OnCompleted", StringComparison.OrdinalIgnoreCase)
                && m.ReturnType == typeof(void)
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(Action));

            // Awaiter optionally implements ICriticalNotifyCompletion
            var implementsICriticalNotifyCompletion = awaiterInterfaces.Any(t => t == typeof(ICriticalNotifyCompletion));
            MethodInfo unsafeOnCompletedMethod;
            if (implementsICriticalNotifyCompletion)
            {
                // ICriticalNotifyCompletion supplies a method matching "void UnsafeOnCompleted(Action action)"
                unsafeOnCompletedMethod = typeof(ICriticalNotifyCompletion).GetRuntimeMethods().Single(m =>
                    m.Name.Equals("UnsafeOnCompleted", StringComparison.OrdinalIgnoreCase)
                    && m.ReturnType == typeof(void)
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType == typeof(Action));
            }
            else
            {
                unsafeOnCompletedMethod = null;
            }

            // Awaiter must have method matching "void GetResult" or "T GetResult()"
            var getResultMethod = awaiterType.GetRuntimeMethods().FirstOrDefault(m =>
                m.Name.Equals("GetResult")
                && m.GetParameters().Length == 0);
            if (getResultMethod == null)
            {
                awaitableInfo = default(AwaitableInfo);
                return false;
            }

            awaitableInfo = new AwaitableInfo(
                awaiterType,
                isCompletedProperty,
                getResultMethod,
                onCompletedMethod,
                unsafeOnCompletedMethod,
                getResultMethod.ReturnType,
                getAwaiterMethod);
            return true;
        }
    }
}
