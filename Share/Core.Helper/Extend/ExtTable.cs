using System.Data;

namespace Core.Helper.Extend
{
  public static class ExtTable
  {
    public static DataTable GetPage(
      this DataTable data,
      int PageIndex,
      int PageSize,
      out int AllPage)
    {
      AllPage = data.Rows.Count / PageSize;
      AllPage += data.Rows.Count % PageSize == 0 ? 0 : 1;
      DataTable dataTable = data.Clone();
      int num1 = PageIndex * PageSize;
      int num2 = num1 + PageSize > data.Rows.Count ? data.Rows.Count : num1 + PageSize;
      if (num1 < num2)
      {
        for (int index = num1; index < num2; ++index)
          dataTable.ImportRow(data.Rows[index]);
      }
      return dataTable;
    }

    public static string? GetValue_String(this DataTable table, int Row, string Collum)
    {
      return Convert.ToString(table.Rows[Row][Collum]);
    }

    public static string? GetValue_String(this DataTable table, int Row, int Collum)
    {
      return Convert.ToString(table.Rows[Row][Collum]);
    }

    public static long GetValue_Long(this DataTable table, int Row, string Collum)
    {
      return Convert.ToInt64(table.Rows[Row][Collum]);
    }

    public static long GetValue_Long(this DataTable table, int Row, int Coll)
    {
      return Convert.ToInt64(table.Rows[Row][Coll]);
    }

    public static DateTime GetValue_DateTime(this DataTable table, int Row, string Collum)
    {
      return Convert.ToDateTime(table.Rows[Row][Collum]);
    }

    public static DateTime GetValue_DateTime(this DataTable table, int Row, int Collum)
    {
      return Convert.ToDateTime(table.Rows[Row][Collum]);
    }

    public static int GetValue_Int(this DataTable table, int Row, string Collum)
    {
      return Convert.ToInt32(table.Rows[Row][Collum]);
    }

    public static int GetValue_Int(this DataTable table, int Row, int Collum)
    {
      return Convert.ToInt32(table.Rows[Row][Collum]);
    }

    public static double GetValue_Double(this DataTable table, int Row, string Collum)
    {
      return Convert.ToDouble(table.Rows[Row][Collum]);
    }

    public static double GetValue_Double(this DataTable table, int Row, int Collum)
    {
      return Convert.ToDouble(table.Rows[Row][Collum]);
    }

    public static Decimal GetValue_Decimal(this DataTable table, int Row, string Collum)
    {
      return Convert.ToDecimal(table.Rows[Row][Collum]);
    }

    public static Decimal GetValue_Decimal(this DataTable table, int Row, int Collum)
    {
      return Convert.ToDecimal(table.Rows[Row][Collum]);
    }

    public static bool GetValue_Boolean(this DataTable table, int Row, string Collum)
    {
      return Convert.ToBoolean(table.Rows[Row][Collum]);
    }

    public static bool GetValue_Boolean(this DataTable table, int Row, int Collum)
    {
      return Convert.ToBoolean(table.Rows[Row][Collum]);
    }

    public static DataTable GetLastNode(
      this DataTable table,
      int TopID,
      string IDColl,
      string PIDColl)
    {
      DataTable tableRe = table.Clone();
      ExtTable.FindLastNode(tableRe, table, TopID, IDColl, PIDColl);
      return tableRe;
    }

    private static void FindLastNode(
      DataTable tableRe,
      DataTable tableSource,
      int PID,
      string IDColl,
      string PIDColl)
    {
      DataRow[] dataRowArray = tableSource.Select(PIDColl + "=" + (object) PID);
      if (dataRowArray.Length == 0)
      {
        foreach (DataRow row in tableSource.Select(IDColl + "=" + (object) PID))
          tableRe.ImportRow(row);
      }
      else
      {
        foreach (DataRow dataRow in dataRowArray)
          ExtTable.FindLastNode(tableRe, tableSource, Convert.ToInt32(dataRow[IDColl]), IDColl, PIDColl);
      }
    }
  }
}
