namespace Core.Helper.APIMessage
{
    public class MessageResponse<T>
    {
        public int? Status { get; set; } = 200;
        public string? Message { get; set; }
        public T? Data { get; set; }
        public Dictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();
    }
}
