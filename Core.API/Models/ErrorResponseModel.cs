namespace Core.API.Models
{
    public partial record ErrorResponseModel
    {
        public List<string>? Errors { get; set; }
    }
}
