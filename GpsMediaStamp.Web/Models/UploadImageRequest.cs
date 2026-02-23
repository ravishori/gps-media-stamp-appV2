using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace GpsMediaStamp.Web.Models
{
    public class UploadImageRequest
    {
        [Required]
        public IFormFile Image { get; set; } = default!;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }
    }
}