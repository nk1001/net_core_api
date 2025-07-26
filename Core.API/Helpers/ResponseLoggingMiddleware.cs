namespace Core.API.Helpers
{
    public class ResponseLoggingMiddleware
    {   
        private readonly RequestDelegate _next;
        private readonly ILogger<ResponseLoggingMiddleware> _logger;

        public ResponseLoggingMiddleware(RequestDelegate next, ILogger<ResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
           
          
                var requestLog = $"{"Response".ToUpper()} HttpMethod: {context.Request.Method}, Path: {context.Request.Path}";


                Stream originalBody = context.Response.Body;

                try
                {
                    using (var memStream = new MemoryStream())
                    {
                        context.Response.Body = memStream;

                        await _next(context);

                        memStream.Position = 0;
                        string bodyAsText = new StreamReader(memStream).ReadToEnd();

                        memStream.Position = 0;
                        await memStream.CopyToAsync(originalBody);
                        if (string.IsNullOrWhiteSpace(bodyAsText) == false)
                        {
                            requestLog += $", Body : {bodyAsText}";
                        }
                        _logger.LogInformation(requestLog);
                    }


                }
                finally
                {
                    context.Response.Body = originalBody;
                }
            
           
        }       
    }
}
