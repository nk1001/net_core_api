using System.Globalization;

namespace Core.Helper.Extend
{
    public static class ExtDate
    {
        public static string ToEasyString(this DateTime value)
        {
            DateTime now = DateTime.Now;
            if (now < value)
                return value.ToString("yyyy/MM/dd");
            TimeSpan timeSpan = now - value;
            if (timeSpan.TotalMinutes < 10.0)
                return "just";
            if (timeSpan.TotalMinutes >= 10.0 && timeSpan.TotalMinutes < 60.0)
                return ((int)timeSpan.TotalMinutes).ToString() + " minutes ago";
            if (timeSpan.TotalHours < 24.0)
                return ((int)timeSpan.TotalHours).ToString() + " an hour ago";
            if (timeSpan.TotalDays < 5.0)
                return ((int)timeSpan.TotalDays).ToString() + " days ago";
            return value.ToString("yyyy/MM/dd");
        }

        public static string ToEasyString(this DateTime? value)
        {
            if (value.HasValue)
                return value.Value.ToEasyString();
            return string.Empty;
        }
        public static DateTime? ConvertStringToDateTime(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            string[] formats = { "dd/MM/yyyy", "dd/MM/yyyy HH:mm" };

            if (DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }

            return null;
        }
    }
}
