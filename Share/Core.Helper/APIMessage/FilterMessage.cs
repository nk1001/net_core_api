namespace Core.Helper.APIMessage
{
    [Serializable]
    public class FilterMessage
    {
        public List<FilterParameter>? Parameters { get; set; }
        public int PageIndex { get; set; } = 20;
        public int PageSize { get; set; } = 1;
        public string? OrderBy { get; set; }
        public string? OrderByMethod { get; set; } = "ASC";

    }

    [Serializable]
    public class FilterParameter
    {
        public string? Type { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
        public string Condition { get; set; } = "AND";
        public string ConditionGroup { get; set; } = "AND";
         public List<FilterParameter>? FilterParameters { get; set; }
    }

  

    [Serializable]
    public class LinqFilterMessage
    {
        public List<LinqFilterParameter>? Parameters { get; set; }
        public string? Query { get; set; }
        public int PageIndex { get; set; } = int.MaxValue;
        public int PageSize { get; set; } = 1;
        public string? OrderBy { get; set; }
        public string? OrderByMethod { get; set; } = "ASC";
        public string? Includes { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    [Serializable]
    public class LinqFilterParameter
    {
        public string? Type { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
    }




}
