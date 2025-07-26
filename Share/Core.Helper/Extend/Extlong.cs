

namespace Core.Helper.Extend
{
  public static class Extlong
  {
    public static string ToDisplayFileSize(this long value)
    {
      if (value < 1000L)
        return string.Format("{0} Byte", (object) value);
      if (value >= 1000L && value < 1000000L)
        return string.Format("{0:F2} Kb", (object) ((double) value / 1024.0));
      if (value >= 1000L && value < 1000000000L)
        return string.Format("{0:F2} M", (object) ((double) value / 1048576.0));
      if (value >= 1000000000L && value < 1000000000000L)
        return string.Format("{0:F2} G", (object) ((double) value / 1073741824.0));
      return string.Format("{0:F2} T", (object) ((double) value / 1099511627776.0));
    }
  }
}
