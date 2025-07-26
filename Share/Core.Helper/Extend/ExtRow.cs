using System.Data;

namespace Core.Helper.Extend
{
  public static class ExtRow
  {
    public static string GetValue_String(this DataRow row, string Collum)
    {
      return Convert.ToString(row[Collum]);
    }

    public static string GetValue_String(this DataRow row, int Collum)
    {
      return Convert.ToString(row[Collum]);
    }

    public static long GetValue_Long(this DataRow row, string Collum)
    {
      return Convert.ToInt64(row[Collum]);
    }

    public static long GetValue_Long(this DataRow row, int Coll)
    {
      return Convert.ToInt64(row[Coll]);
    }

    public static DateTime GetValue_DateTime(this DataRow row, string Collum)
    {
      return Convert.ToDateTime(row[Collum]);
    }

    public static DateTime GetValue_DateTime(this DataRow row, int Row, int Collum)
    {
      return Convert.ToDateTime(row[Collum]);
    }

    public static int GetValue_Int(this DataRow row, string Collum)
    {
      return Convert.ToInt32(row[Collum]);
    }

    public static int GetValue_Int(this DataRow row, int Collum)
    {
      return Convert.ToInt32(row[Collum]);
    }

    public static double GetValue_Double(this DataRow row, string Collum)
    {
      return Convert.ToDouble(row[Collum]);
    }

    public static double GetValue_Double(this DataRow row, int Collum)
    {
      return Convert.ToDouble(row[Collum]);
    }

    public static Decimal GetValue_Decimal(this DataRow row, string Collum)
    {
      return Convert.ToDecimal(row[Collum]);
    }

    public static Decimal GetValue_Decimal(this DataRow row, int Collum)
    {
      return Convert.ToDecimal(row[Collum]);
    }

    public static bool GetValue_Boolean(this DataRow row, string Collum)
    {
      return Convert.ToBoolean(row[Collum]);
    }

    public static bool GetValue_Boolean(this DataRow row, int Collum)
    {
      return Convert.ToBoolean(row[Collum]);
    }
  }
}
