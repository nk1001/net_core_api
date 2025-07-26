using System.Text;
using Microsoft.AspNetCore.Http.Features;

namespace Core.API.Helpers
{
    public class RequestLoggingMiddleware
    {   
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var injectedRequestStream = new MemoryStream();            

            try
            {
                var requestLog = $"REQUEST HttpMethod: {context.Request.Method}, Path: {context.Request.Path}";


                var syncIOFeature = context.Features.Get<IHttpBodyControlFeature>();
                if (syncIOFeature != null)
                {
                    syncIOFeature.AllowSynchronousIO = true;

                    var req = context.Request;

                    req.EnableBuffering();

                    // read the body here as a workarond for the JSON parser disposing the stream
                    if (req.Body.CanSeek)
                    {
                        req.Body.Seek(0, SeekOrigin.Begin);

                        // if body (stream) can seek, we can read the body to a string for logging purposes
                        using (var reader = new StreamReader(
                                   req.Body,
                                   encoding: Encoding.UTF8,
                                   detectEncodingFromByteOrderMarks: false,
                                   bufferSize: 8192,
                                   leaveOpen: true))
                        {
                            var bodyAsText = reader.ReadToEnd();
                            if (string.IsNullOrWhiteSpace(bodyAsText) == false)
                            {
                                requestLog += $", Body : {bodyAsText}";
                            }
                            // store into the HTTP context Items["request_body"]

                        }

                        // go back to beginning so json reader get's the whole thing
                        req.Body.Seek(0, SeekOrigin.Begin);
                    }
                }


           

                _logger.LogInformation(requestLog);

                await _next.Invoke(context);                                
            }
            finally
            {
                injectedRequestStream.Dispose();
            }
        }       
    }
}
