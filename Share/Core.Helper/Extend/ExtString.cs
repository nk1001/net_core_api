using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Helper.Extend
{
    public static class ExtString
    {
        private static readonly Regex emailExpression = new Regex("^([0-9a-zA-Z]+[-._+&])*[0-9a-zA-Z]+@([-0-9a-zA-Z]+[.])+[a-zA-Z]{2,6}$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        private static readonly Regex webUrlExpression = new Regex("(http|https)://([\\w-]+\\.)+[\\w-]+(/[\\w- ./?%&=]*)?", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        private static readonly Regex stripHTMLExpression = new Regex("<\\S[^><]*>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        public static string GetInnerContent(this string value, string start, string end, int index)
        {
            List<string> innerContent = value.GetInnerContent(start, end);
            if (innerContent.Count > 0)
                return innerContent[index];
            return string.Empty;
        }

        public static List<string> GetInnerContent(this string value, string start, string end)
        {
            List<string> stringList = new List<string>();
            int startIndex = 0;
            int num = 0;
            while (num >= startIndex)
            {
                startIndex = value.IndexOf(start, startIndex, StringComparison.InvariantCultureIgnoreCase);
                if (startIndex >= 0)
                {
                    num = value.IndexOf(end, startIndex + start.Length + 1, StringComparison.InvariantCultureIgnoreCase);
                    if (num >= 0)
                    {
                        if (num > startIndex)
                        {
                            stringList.Add(value.Substring(startIndex + start.Length, num - startIndex - start.Length));
                            startIndex = num;
                        }
                    }
                    else
                        break;
                }
                else
                    break;
            }
            return stringList;
        }

        public static string GetHtmlInner(this string value, string tag, int? index = 0)
        {
            string str1 = string.Format("</{0}>", (object)tag);
            tag = string.Format("<{0}", (object)tag);
            int num1 = 0;
            string str2 = string.Empty;
            int startIndex = 0;
            while (true)
            {
                int num2 = num1;
                int? nullable = index;
                int valueOrDefault = nullable.GetValueOrDefault();
                if ((num2 <= valueOrDefault ? (nullable.HasValue ? 1 : 0) : 0) != 0)
                {
                    int num3 = value.IndexOf(tag, startIndex, StringComparison.InvariantCultureIgnoreCase);
                    startIndex = value.IndexOf(">", num3 + 1, StringComparison.InvariantCultureIgnoreCase) + 1;
                    if (startIndex >= 0)
                    {
                        int num4 = value.IndexOf(tag, startIndex + 1, StringComparison.InvariantCultureIgnoreCase);
                        int num5 = value.IndexOf(str1, startIndex + 1, StringComparison.InvariantCultureIgnoreCase);
                        while (num4 < num5 && num4 >= 0)
                        {
                            num4 = value.IndexOf(tag, num4 + 1, StringComparison.InvariantCultureIgnoreCase);
                            int num6 = value.IndexOf(str1, num5 + 1, StringComparison.InvariantCultureIgnoreCase);
                            if (num6 >= 0)
                                num5 = num6;
                        }
                        if (startIndex < num5)
                        {
                            str2 = value.Substring(startIndex, num5 - startIndex);
                            startIndex = num5 - startIndex;
                        }
                        ++num1;
                    }
                    else
                        break;
                }
                else
                    goto label_11;
            }
            return string.Empty;
        label_11:
            return str2;
        }

        public static int GetMatchCount(this string value, string target, StringComparison comparison)
        {
            int num1 = 0;
            int num2 = -1;
            do
            {
                num2 = value.IndexOf(target, num2 + 1, comparison);
                ++num1;
            }
            while (num2 >= 0);
            int num3;
            return num3 = num1 - 1;
        }

        public static string NoHTML(this string Htmlstring)
        {
            Htmlstring = Htmlstring.Replace("\r\n", "");
            Htmlstring = Regex.Replace(Htmlstring, "<script.*?</script>", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "<style.*?</style>", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "<.*?>", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "<(.[^>]*)>", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "([\\r\\n])[\\s]+", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "-->", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "<!--.*", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(quot|#34);", "\"", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(amp|#38);", "&", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(lt|#60);", "<", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(gt|#62);", ">", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(nbsp|#160);", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(iexcl|#161);", "¡", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(cent|#162);", "¢", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(pound|#163);", "£", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(copy|#169);", "©", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&#(\\d+);", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "\\s{2,}", "", RegexOptions.IgnoreCase);
            Htmlstring = Htmlstring.Replace("<", "");
            Htmlstring = Htmlstring.Replace(">", "");
            Htmlstring = Htmlstring.Replace("\r\n", "");
            Htmlstring = Htmlstring.Replace("\n", "");
            return Htmlstring;
        }

        public static string ToFileName(this string value)
        {
            return value.Replace("/", "_").Replace("\\", "_").Replace(":", "_").Replace("*", "_").Replace("?", "_").Replace("\"", "_").Replace("<", "_").Replace(">", "_").Replace("|", "_");
        }

        public static byte[] ToByte(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public static string HtmlDecode(this string value)
        {
            return WebUtility.HtmlDecode(value);
        }

        public static string HtmlEncode(this string value)
        {
            return WebUtility.HtmlEncode(value);
        }

        public static string UrlEncode(this string value)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte num in Encoding.UTF8.GetBytes(value))
                stringBuilder.Append("%" + Convert.ToString(num, 16));
            return stringBuilder.ToString();
        }

        public static string ToUnicode(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index < value.Length; ++index)
                stringBuilder.Append("\\u" + ((int)value[index]).ToString("x"));
            return stringBuilder.ToString();
        }

        public static string FormatWith(this string instance, params object[] args)
        {
            return string.Format(instance, args);
        }

        public static string Hash(this string instance)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = Encoding.Unicode.GetBytes(instance);
                return Convert.ToBase64String(md5.ComputeHash(bytes));
            }
        }

        public static T ToEnum<T>(this string instance, T defaultValue) where T : struct, IComparable, IFormattable
        {
            T result = defaultValue;
            if (!string.IsNullOrWhiteSpace(instance) && !System.Enum.TryParse<T>(instance.Trim(), true, out result))
                result = defaultValue;
            return result;
        }

        public static T ToEnum<T>(this int instance, T defaultValue) where T : struct, IComparable, IFormattable
        {
            T result;
            if (!System.Enum.TryParse<T>(instance.ToString(), true, out result))
                result = defaultValue;
            return result;
        }

        public static string StripHtml(this string instance)
        {
            return ExtString.stripHTMLExpression.Replace(instance, string.Empty);
        }

        public static bool IsEmail(this string instance)
        {
            if (!string.IsNullOrWhiteSpace(instance))
                return ExtString.emailExpression.IsMatch(instance);
            return false;
        }

        public static bool IsWebUrl(this string instance)
        {
            if (!string.IsNullOrWhiteSpace(instance))
                return ExtString.webUrlExpression.IsMatch(instance);
            return false;
        }

        public static bool IsIPAddress(this string instance)
        {
            if (!string.IsNullOrWhiteSpace(instance))
            {
                IPAddress address;
                return IPAddress.TryParse(instance, out address);
            }
            return false;
        }

        public static bool AsBool(this string instance)
        {
            bool result = false;
            bool.TryParse(instance, out result);
            return result;
        }

        public static DateTime AsDateTime(this string instance)
        {
            DateTime result = DateTime.MinValue;
            DateTime.TryParse(instance, out result);
            return result;
        }

        public static Decimal AsDecimal(this string instance)
        {
            Decimal result = new Decimal();
            Decimal.TryParse(instance, out result);
            return result;
        }

        public static int AsInt(this string instance)
        {
            int result = 0;
            int.TryParse(instance, out result);
            return result;
        }

        public static bool IsInt(this string instance)
        {
            int result;
            return int.TryParse(instance, out result);
        }
        public static bool IsLong(this string instance)
        {
            long result;
            return long.TryParse(instance, out result);
        }
        public static bool IsDateTime(this string instance)
        {
            DateTime result;
            return DateTime.TryParse(instance, out result);
        }

        public static bool IsFloat(this string instance)
        {
            float result;
            return float.TryParse(instance, out result);
        }

        public static bool IsNullOrWhiteSpace(this string? instance)
        {
            return string.IsNullOrWhiteSpace(instance);
        }

        public static bool IsNotNullAndWhiteSpace(this string instance)
        {
            return !string.IsNullOrWhiteSpace(instance);
        }


        public static bool HasSpecialChar(this string instance)
        {
            string specialChar = @"\|!#$%&/()=?»«@£§€{}.-;'<>_,";
            foreach (var item in specialChar)
            {
                if (instance.Contains(item)) return true;
            }
            return false;
        }
        public static bool HasNumber(this string instance)
        {
            var hasNumber = new Regex(@"[0-9]+");
            return hasNumber.IsMatch(instance);
        }
        public static bool HasUpperChar(this string instance)
        {
            var hasUpperChar = new Regex(@"[A-Z]+");
            return hasUpperChar.IsMatch(instance);
        }

        public static bool HasLowChar(this string instance)
        {
            var hasUpperChar = new Regex(@"[a-z]+");
            return hasUpperChar.IsMatch(instance);
        }
        public static string Take(this string theString, int count, bool ellipsis = false)
        {
            int length = Math.Min(count, theString.Length);
            string str = theString.Substring(0, length);
            if (ellipsis && length < theString.Length)
                str += "...";
            return str;
        }

        public static string Skip(this string theString, int count)
        {
            int num = Math.Min(count, theString.Length);
            return theString.Substring(num - 1);
        }

        public static string Reverse(this string input)
        {
            char[] charArray = input.ToCharArray();
            Array.Reverse((Array)charArray);
            return new string(charArray);
        }

        public static bool IsNullOrEmpty(this string? theString)
        {
            return string.IsNullOrEmpty(theString);
        }

        public static bool Match(this string value, string pattern)
        {
            return Regex.IsMatch(value, pattern);
        }

        public static string[] SplitIntoChunks(this string toSplit, int chunkSize)
        {
            if (string.IsNullOrEmpty(toSplit))
                return new string[1] { "" };
            int length1 = toSplit.Length;
            int length2 = (int)Math.Ceiling((Decimal)length1 / (Decimal)chunkSize);
            string[] strArray = new string[length2];
            int val1 = length1;
            for (int index = 0; index < length2; ++index)
            {
                int length3 = Math.Min(val1, chunkSize);
                int startIndex = chunkSize * index;
                strArray[index] = toSplit.Substring(startIndex, length3);
                val1 -= length3;
            }
            return strArray;
        }

        public static string Join(this object[] array, string seperator)
        {
            if (array == null)
                return "";
            return string.Join(seperator, array);
        }
        public static string ToFilePath(this string path)
        {
            return Path.Combine(path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries));
        }
        public static string CombinePath(this string p, string path)
        {
            return $"{p.TrimEnd(Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}{path.ToFilePath()}";
        }
        public static bool TryCheckContains(this string source, string check)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return false;
            }
            return source.Contains(check);
        }
    }
}
