namespace GpsMediaStamp.Web.Models
{
    public class FfmpegOptions
    {
        public string Path { get; set; } = string.Empty;
        public int RepeatIntervalSeconds { get; set; } = 5;
        public int RepeatDurationSeconds { get; set; } = 2;
        public bool EnableCenterWatermark { get; set; } = true;
    }
}