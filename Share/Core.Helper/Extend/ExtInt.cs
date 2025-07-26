

namespace Core.Helper.Extend
{
  public static class ExtInt
  {
    public static string ToDisplayFileSize(this int value)
    {
      if (value < 1000)
        return string.Format("{0} Byte", (object) value);
      if (value >= 1000 && value < 1000000)
        return string.Format("{0:F2} Kb", (object) ((double) value / 1024.0));
      if (value >= 1000 && value < 1000000000)
        return string.Format("{0:F2} M", (object) ((double) value / 1048576.0));
      return string.Format("{0:F2} G", (object) ((double) value / 1073741824.0));
    }
  }
}
