namespace Core.EF.WebApi
{
    public static class ServiceLocator
    {
        private static IHttpContextAccessor _httpContextAccessor = null!;
        private static ILoggerFactory _loggerFactory = null!;
        
        public static void Setup(IHttpContextAccessor httpContextAccessor, ILoggerFactory iLoggerFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _loggerFactory = iLoggerFactory;
        }
        public static T? GetService<T>()
        {
            if (_httpContextAccessor.HttpContext == null) return default(T);
            return _httpContextAccessor.HttpContext.RequestServices.GetService<T>()!;
           
        }

        public static IEnumerable<T> GetServices<T>()
        {
            if (_httpContextAccessor.HttpContext == null) return Enumerable.Empty<T>();
            return _httpContextAccessor.HttpContext.RequestServices.GetServices<T>();
        }
        public static object? GetService(Type type)
        {
            if (_httpContextAccessor.HttpContext == null) return null;
            return _httpContextAccessor.HttpContext.RequestServices.GetService(type)!;
        }
        public static IEnumerable<object> GetServices(Type type)
        {
            if (_httpContextAccessor.HttpContext == null) return Enumerable.Empty<object>();
            return _httpContextAccessor.HttpContext.RequestServices.GetServices(type)!;
        }
        public static HttpContext? HttpContext => _httpContextAccessor.HttpContext;

        public static ILogger Logger<T>()
        {            
            return _loggerFactory.CreateLogger<T>();
        }
    }
}