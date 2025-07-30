using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Helper.Extend
{
    public static class DictionaryHelper
    {
        public static void SafeAdd<K,T>(this IDictionary<K, T> dict, K key, T value)
        {
            if (dict.ContainsKey(key))
            {
                dict.Remove(key);
            }
            dict.Add(key,value);
        }
    }
}
