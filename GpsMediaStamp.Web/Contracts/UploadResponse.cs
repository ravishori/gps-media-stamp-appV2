namespace GpsMediaStamp.Web.Models
{
    public class UploadResponse
    {
        public string RawFilePath { get; set; } = string.Empty;
        public string StampedFilePath { get; set; } = string.Empty;

        public string RawFileHash { get; set; } = string.Empty;
        public string StampedFileHash { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }
}