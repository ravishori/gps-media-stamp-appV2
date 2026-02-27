using GpsMediaStamp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
            if (request?.Image == null)
                return BadRequest("No image uploaded.");

            if (request.Latitude < -90 || request.Latitude > 90 ||
                request.Longitude < -180 || request.Longitude > 180)
            {
                return BadRequest("Invalid latitude or longitude values.");
            }

            _logger.LogInformation("Image upload started: {File}", request.Image.FileName);

            // 1️⃣ Save raw image
            using var rawStream = request.Image.OpenReadStream();
            var rawPath = await _fileStorage.SaveRawAsync(rawStream, request.Image.FileName);

            // 2️⃣ Generate Hash + Signature
            var rawHash = _hashService.GenerateSha256(rawPath);
            var signature = _signingService.SignHash(rawHash);

            // 3️⃣ Reverse Geocode (returns full formatted address string)
            var address =
                await _locationService.ReverseGeocodeAsync(
                    request.Latitude,
                    request.Longitude);

            if (string.IsNullOrWhiteSpace(address))
            {
                _logger.LogWarning("Reverse geocoding failed for Lat:{Lat}, Lon:{Lon}",
                    request.Latitude, request.Longitude);

                return BadRequest("Unable to resolve address from coordinates.");
            }

            // 4️⃣ Generate Google Maps QR
            var qrPath = await _qrService
                .GenerateGoogleMapsQrAsync(request.Latitude, request.Longitude);

            // 5️⃣ Stamp Image (pass address string instead of model)
            var tempStampedPath = await _imageStamp.StampPremiumImageAsync(
                rawPath,
                address,
                request.Latitude,
                request.Longitude,
                DateTime.UtcNow,
                qrPath);

            // 6️⃣ Save stamped image
            using var stampedStream = System.IO.File.OpenRead(tempStampedPath);

            var finalStampedPath = await _fileStorage.SaveStampedAsync(
                stampedStream,
                request.Image.FileName);

            _logger.LogInformation("Image upload completed: {File}", request.Image.FileName);

            return Ok(new
            {
                RawFilePath = rawPath,
                StampedFilePath = finalStampedPath,
                RawHash = rawHash,
                Signature = signature,
                Address = address
            });
        }
    }
}