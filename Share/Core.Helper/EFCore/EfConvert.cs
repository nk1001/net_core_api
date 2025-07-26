using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace Core.Helper.EFCore
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class EfJsonConvertAttribute : System.Attribute
    {
        public EfJsonConvertAttribute() { }
    }
    
    // Generic ValueConverter for List<T>
    public class JsonListConverter<T> : ValueConverter<List<T>, string>
    {
        public JsonListConverter()
            : base(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<T>>(v) ?? new List<T>())
        {
        }
    }
}
