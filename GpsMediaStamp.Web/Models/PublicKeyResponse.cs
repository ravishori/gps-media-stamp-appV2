namespace GpsMediaStamp.Web.Models
{
    public class PublicKeyResponse
    {
        public string Algorithm { get; set; } = "RSA-SHA256";
        public string PublicKey { get; set; } = string.Empty;
    }
}