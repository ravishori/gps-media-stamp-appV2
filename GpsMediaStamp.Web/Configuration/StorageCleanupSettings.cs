namespace GpsMediaStamp.Web.Configuration
{
    public class StorageCleanupSettings
    {
        public string LogsFolder { get; set; } = "Logs";

        public string TempFolder { get; set; } = "Temp";

        /// <summary>
        /// Folder that holds server-side stamped output files.
        /// These are cleaned up on the same retention schedule as logs/temp.
        /// </summary>
        public string StampedFolder { get; set; } = "storage/stamped";

        public int RetentionHours { get; set; } = 24;
    }
}