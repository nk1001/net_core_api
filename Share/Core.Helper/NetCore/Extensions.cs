
using Microsoft.Extensions.Configuration;

namespace Core.Helper.NetCore
{
    public static class Extensions
    {
        public static string? GetByPath(this IConfiguration? configuration, string name)
        {

            var config =  Environment.GetEnvironmentVariable(name.Replace(":", "_").ToUpper()) ?? configuration[name];
            return config;
        }
    }
}
