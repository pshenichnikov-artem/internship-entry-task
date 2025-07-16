namespace Core.Models.DTOs.Common
{
    public class ErrorResponse
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string[]>? Details { get; set; }
    }
}