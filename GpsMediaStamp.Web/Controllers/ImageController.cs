using GpsMediaStamp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using GpsMediaStamp.Application.Interfaces;
using GpsMediaStamp.Application.Interfaces.Common;
using GpsMediaStamp.Application.Interfaces.Image;
using GpsMediaStamp.Application.Interfaces.Security;
using GpsMediaStamp.Application.Interfaces.Qr;

namespace GpsMediaStamp.Web.Controllers
{
    [ApiController]
    [Route("api/image")]
    public class ImageController : ControllerBase
    {
        private readonly IFileStorageService _fileStorage;
        private readonly IImageStampService _imageStamp;
        private readonly IHashService _hashService;
        private readonly ISigningService _signingService;
        private readonly ILocationService _locationService;
        private readonly IGoogleMapsQrService _qrService;
        private readonly ILogger<ImageController> _logger;

        private readonly string[] AllowedImageExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public ImageController(
            IFileStorageService fileStorage,
            IImageStampService imageStamp,
            IHashService hashService,
            ISigningService signingService,
            ILocationService locationService,
            IGoogleMapsQrService qrService,
            ILogger<ImageController> logger)
        {
            _fileStorage = fileStorage;
            _imageStamp = imageStamp;
            _hashService = hashService;
            _signingService = signingService;
            _locationService = locationService;
            _qrService = qrService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request)
        {
            try
            {
                if (request?.Image == null || request.Image.Length == 0)
                    return BadRequest(new { error = "No image uploaded." });

                if (request.Latitude < -90 || request.Latitude > 90 ||
                    request.Longitude < -180 || request.Longitude > 180)
                {
                    return BadRequest(new { error = "Invalid latitude or longitude values." });
                }

                var extension = Path.GetExtension(request.Image.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(extension))
                    return BadRequest(new { error = "Invalid image file type." });

                _logger.LogInformation("Image upload started: {File}", request.Image.FileName);

                // 1️⃣ Save RAW image
                using var rawStream = request.Image.OpenReadStream();
                var rawPath = await _fileStorage.SaveRawAsync(rawStream, request.Image.FileName);

                // 2️⃣ Generate Hash + Signature
                var rawHash = _hashService.GenerateSha256(rawPath);
                var signature = _signingService.SignHash(rawHash);

                // 3️⃣ Reverse Geocode
                var address = await _locationService.ReverseGeocodeAsync(
                    request.Latitude,
                    request.Longitude);

                if (string.IsNullOrWhiteSpace(address))
                {
                    _logger.LogWarning("Reverse geocoding failed. Using coordinates fallback.");
                    address = $"Lat: {request.Latitude}, Lon: {request.Longitude}";
                }

                // 4️⃣ Generate Google Maps QR
                var qrPath = await _qrService.GenerateGoogleMapsQrAsync(
                    request.Latitude,
                    request.Longitude);

                // 5️⃣ Stamp image
                var tempStampedPath = await _imageStamp.StampPremiumImageAsync(
                    rawPath,
                    address,
                    request.Latitude,
                    request.Longitude,
                    DateTime.UtcNow,
                    qrPath);

                // 6️⃣ Save final stamped image
                string finalStampedPath;
                using (var stampedStream = System.IO.File.OpenRead(tempStampedPath))
                {
                    finalStampedPath = await _fileStorage.SaveStampedAsync(
                        stampedStream,
                        request.Image.FileName);
                }

                // Cleanup temporary files
                if (System.IO.File.Exists(tempStampedPath))
                    System.IO.File.Delete(tempStampedPath);

                if (System.IO.File.Exists(qrPath))
                    System.IO.File.Delete(qrPath);

                // 7️⃣ Build PUBLIC URL (VERY IMPORTANT)
                var fileName = Path.GetFileName(finalStampedPath);

                var stampedUrl =
                    $"{Request.Scheme}://{Request.Host}/storage/stamped/{fileName}";

                _logger.LogInformation("Image upload completed successfully.");

                return Ok(new
                {
                    StampedUrl = stampedUrl,
                    RawHash = rawHash,
                    Signature = signature,
                    Address = address,
                    Message = "Image stamped with GPS location and digitally signed."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image processing failed");

                return StatusCode(500, new
                {
                    error = "Internal server error",
                    detail = ex.Message
                });
            }
        }
    }
}