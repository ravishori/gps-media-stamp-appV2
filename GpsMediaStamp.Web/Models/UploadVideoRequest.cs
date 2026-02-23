using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace GpsMediaStamp.Web.Models
{
    public class UploadVideoRequest
    {
        [Required(ErrorMessage = "Video file is required.")]
        public IFormFile Video { get; set; } = default!;

        [Required(ErrorMessage = "Latitude is required.")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required.")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double Longitude { get; set; }

        [Required(ErrorMessage = "Timestamp is required.")]
        public DateTime Timestamp { get; set; }

        // Optional but recommended for traceability
        [MaxLength(100)]
        public string? DeviceId { get; set; }
    }
}