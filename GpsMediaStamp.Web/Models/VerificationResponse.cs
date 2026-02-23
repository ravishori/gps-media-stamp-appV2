namespace GpsMediaStamp.Web.Models
{
    public class VerificationResponse
    {
        public string Hash { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}