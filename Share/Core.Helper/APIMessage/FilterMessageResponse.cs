namespace Core.Helper.APIMessage
{
    [Serializable]
    public class FilterMessageResponse<T> where T : class
    {
        public FilterMessageResponse(FilterMessage filter, long totalRecord, IEnumerable<T> results)
        {
            Filter = filter;
            TotalRecord = totalRecord;
            Results = results;
        }

        public FilterMessage Filter { get; set; }
        public long TotalRecord { get; set; }
        public IEnumerable<T> Results { get; set; }
    }

    [Serializable]
    public class FilterLinqMessageResponse<T> where T : class
    {
        public FilterLinqMessageResponse(LinqFilterMessage filter, long totalRecord, IEnumerable<T> results)
        {
            Filter = filter;
            TotalRecord = totalRecord;
            Results = results;
        }

        public LinqFilterMessage Filter { get; set; }
        public long TotalRecord { get; set; }
        public IEnumerable<T> Results { get; set; }
    }
}
