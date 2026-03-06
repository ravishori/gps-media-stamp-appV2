using GpsMediaStamp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using GpsMediaStamp.Application.Interfaces;
using GpsMediaStamp.Application.Interfaces.Common;
using GpsMediaStamp.Application.Interfaces.Security;
using GpsMediaStamp.Application.Interfaces.Qr;
using GpsMediaStamp.Application.Interfaces.Video;

namespace GpsMediaStamp.Web.Controllers
{
    [ApiController]
    [Route("api/video")]
    public class MediaController : ControllerBase
    {
        private readonly IFileStorageService _fileStorage;
        private readonly IVideoStampService _videoStamp;
        private readonly IHashService _hashService;
        private readonly ISigningService _signingService;
        private readonly IQrCodeService _qrService;
        private readonly ILocationService _locationService;
        private readonly ILogger<MediaController> _logger;

        private const long MaxFileSize = 500 * 1024 * 1024;

        private readonly string[] AllowedVideoExtensions =
        {
            ".mp4", ".mov", ".avi"
        };

        public MediaController(
            IFileStorageService fileStorage,
            IVideoStampService videoStamp,
            IHashService hashService,
            ISigningService signingService,
            IQrCodeService qrService,
            ILocationService locationService,
            ILogger<MediaController> logger)
        {
            _fileStorage = fileStorage;
            _videoStamp = videoStamp;
            _hashService = hashService;
            _signingService = signingService;
            _qrService = qrService;
            _locationService = locationService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> Upload([FromForm] UploadVideoRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (request.Video == null || request.Video.Length == 0)
                    return BadRequest(new { error = "No video file uploaded." });

                if (request.Video.Length > MaxFileSize)
                    return BadRequest(new { error = "File size exceeds 500 MB limit." });

                var extension = Path.GetExtension(request.Video.FileName).ToLowerInvariant();
                if (!AllowedVideoExtensions.Contains(extension))
                    return BadRequest(new { error = "Invalid file extension." });

                if (request.Latitude < -90 || request.Latitude > 90 ||
                    request.Longitude < -180 || request.Longitude > 180)
                    return BadRequest(new { error = "Invalid latitude or longitude values." });

                _logger.LogInformation("Video upload started: {FileName}", request.Video.FileName);

                // 1️⃣ Save RAW
                using var rawStream = request.Video.OpenReadStream();
                var rawPath = await _fileStorage.SaveRawAsync(rawStream, request.Video.FileName);

                // 2️⃣ Hash
                var rawHash = _hashService.GenerateSha256(rawPath);

                // 3️⃣ Sign
                var signature = _signingService.SignHash(rawHash);

                // 4️⃣ Reverse Geocode
                string address = "Address unavailable";

                try
                {
                    address = await _locationService.ReverseGeocodeAsync(
                        request.Latitude,
                        request.Longitude) ?? "Address unavailable";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Reverse geocoding failed: {Message}", ex.Message);
                }

                string latText = request.Latitude.ToString("F5");
                string lonText = request.Longitude.ToString("F5");

                var stampText =
                    $"{address}\n" +
                    $"Latitude: {latText}\n" +
                    $"Longitude: {lonText}\n" +
                    $"{request.Timestamp:dd-MMM-yyyy HH:mm} IST\n";

                // 5️⃣ Generate QR
                string mapsUrl =
                    $"https://www.google.com/maps?q={latText},{lonText}";

                var qrPath = await _qrService.GenerateQrAsync(mapsUrl);

                // 6️⃣ Stamp Video
                var tempStampedPath = await _videoStamp.StampVideoAsync(
                    rawPath,
                    stampText,
                    qrPath);

                // 7️⃣ Save Final Stamped File
                string finalStampedPath;

                using (var stampedStream = System.IO.File.OpenRead(tempStampedPath))
                {
                    finalStampedPath = await _fileStorage.SaveStampedAsync(
                        stampedStream,
                        request.Video.FileName);
                }

                var stampedHash = _hashService.GenerateSha256(finalStampedPath);

                // Cleanup temporary files
                if (System.IO.File.Exists(qrPath))
                    System.IO.File.Delete(qrPath);

                if (System.IO.File.Exists(tempStampedPath))
                    System.IO.File.Delete(tempStampedPath);

                // 8️⃣ Build PUBLIC URL
                var fileName = Path.GetFileName(finalStampedPath);

                var stampedUrl =
                    $"{Request.Scheme}://{Request.Host}/storage/stamped/{fileName}";

                _logger.LogInformation("Video upload completed successfully.");

                return Ok(new
                {
                    StampedUrl = stampedUrl,
                    RawHash = rawHash,
                    StampedHash = stampedHash,
                    Signature = signature,
                    Message = "Video stamped with GPS location and digitally signed."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video processing failed");

                return StatusCode(500, new
                {
                    error = "Internal server error",
                    detail = ex.Message
                });
            }
        }
    }
}