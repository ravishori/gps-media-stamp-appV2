using GpsMediaStamp.Web.Models;
using GpsMediaStamp.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GpsMediaStamp.Web.Controllers
{
    [ApiController]
    [Route("api/video")]
    public class MediaController : ControllerBase
    {
        private readonly IFileStorageService _fileStorage;
        private readonly IVideoStampService _videoStamp;
        private readonly IHashService _hashService;
        private readonly IQrCodeService _qrService;
        private readonly ISigningService _signingService;
        private readonly ILogger<MediaController> _logger;

        private const long MaxFileSize = 100 * 1024 * 1024; // 100 MB

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
            ILogger<MediaController> logger)
        {
            _fileStorage = fileStorage;
            _videoStamp = videoStamp;
            _hashService = hashService;
            _qrService = qrService;
            _signingService = signingService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> Upload([FromForm] UploadVideoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 🔹 Null check
            if (request.Video == null || request.Video.Length == 0)
                return BadRequest(new { error = "No video file uploaded." });

            // 🔹 File size validation
            if (request.Video.Length > MaxFileSize)
                return BadRequest(new { error = "File size exceeds 100 MB limit." });

            // 🔹 MIME type validation (IMPORTANT)
            if (!request.Video.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Invalid file type. Only videos allowed." });

            // 🔹 Extension validation
            var extension = Path.GetExtension(request.Video.FileName).ToLowerInvariant();
            if (!AllowedVideoExtensions.Contains(extension))
                return BadRequest(new
                {
                    error = "Invalid file extension. Allowed: .mp4, .mov, .avi"
                });

            _logger.LogInformation("Video upload started: {FileName}", request.Video.FileName);

            string? qrPath = null;
            string? tempStampedPath = null;

            // 1️⃣ Save RAW video
            var rawPath = await _fileStorage.SaveRawAsync(request.Video);

            // 2️⃣ Generate SHA256 hash
            var rawHash = _hashService.GenerateSha256(rawPath);

            // 3️⃣ Sign hash
            var signature = _signingService.SignHash(rawHash);

            // 4️⃣ Visible truncated hash
            var visibleHash = rawHash.Substring(0, 20);

            // 5️⃣ Build stamp text
            var stampText =
                $"Lat: {request.Latitude} | Lon: {request.Longitude}\n" +
                $"{request.Timestamp.ToString("dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture)} IST\n" +
                $"SHA256: {visibleHash}...\n" +
                $"Developed By Ravi Shori";

            // 6️⃣ Build QR payload
            var qrPayload = JsonSerializer.Serialize(new
            {
                hash = rawHash,
                signature = signature,
                algorithm = "RSA-SHA256"
            });

            qrPath = await _qrService.GenerateQrAsync(qrPayload);

            // 7️⃣ Stamp video via FFmpeg
            tempStampedPath = await _videoStamp.StampVideoAsync(
                rawPath,
                stampText,
                qrPath);

            // 8️⃣ Save stamped file
            var finalStampedPath = await _fileStorage.SaveStampedAsync(
                tempStampedPath,
                request.Video.FileName);

            // 9️⃣ Generate stamped hash
            var stampedHash = _hashService.GenerateSha256(finalStampedPath);

            // 🔥 Cleanup temp files
            if (!string.IsNullOrEmpty(qrPath) && System.IO.File.Exists(qrPath))
                System.IO.File.Delete(qrPath);

            if (!string.IsNullOrEmpty(tempStampedPath) && System.IO.File.Exists(tempStampedPath))
                System.IO.File.Delete(tempStampedPath);

            _logger.LogInformation("Video upload completed: {FileName}", request.Video.FileName);

            return Ok(new UploadResponse
            {
                RawFilePath = rawPath,
                StampedFilePath = finalStampedPath,
                RawFileHash = rawHash,
                StampedFileHash = stampedHash,
                Signature = signature,
                Message = "Video stamped and digitally signed successfully."
            });
        }
    }
}