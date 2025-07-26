namespace Core.API.Models
{
    /// <summary>
    /// 
    /// </summary>
    public enum ApiCode
    {
        SUCCEED = 200,
        FAILED = 400,
        RELOAD = 300
    }
    /// <summary>
    /// 
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public ApiResult(ApiCode code, string message )
        {
            ResultCode = (int)code;
            Message = message;
            ResultData = null;
            BeginTime = null;
            EndTime = null;
        }
        public ApiResult(ApiCode code, string message, DateTime? beginTime, DateTime? endTime)
        {
            ResultCode = (int)code;
            Message = message;
            ResultData = null;
            BeginTime = beginTime;
            EndTime = endTime;
        }
        public int ResultCode { get; set; }
        public string Message { get; set; }
        public object? ResultData { get; set; }

        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
        public float TotalTimeExec
        {
            get
            {
                float totalTimeExec = 0;
                if (BeginTime.HasValue && EndTime.HasValue)
                {
                    //DateTime dt1 = DateTime.Parse(maskedTextBox1.Text);
                    //DateTime dt2 = DateTime.Parse(maskedTextBox2.Text);
                    TimeSpan span = EndTime.Value - BeginTime.Value;
                    //int ms = (int)span.TotalMilliseconds;
                    int ss = (int)span.TotalSeconds;
                    totalTimeExec = ss;
                }
                else totalTimeExec = -1;
                return totalTimeExec;
            }
        }
    }
}
