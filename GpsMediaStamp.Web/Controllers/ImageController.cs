using GpsMediaStamp.Web.Models;
using GpsMediaStamp.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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

            _logger.LogInformation("Upload started: {File}", request.Image.FileName);

            // 1️⃣ Save raw image
            var rawPath = await _fileStorage.SaveRawAsync(request.Image);

            // 2️⃣ Hash + Sign
            var rawHash = _hashService.GenerateSha256(rawPath);
            var signature = _signingService.SignHash(rawHash);

            // 3️⃣ Reverse Geocode (OpenStreetMap)
            var location = await _locationService
                .ReverseGeocodeAsync(request.Latitude, request.Longitude);

            // 4️⃣ Build structured stamp text
            var stampText =
                $"{location.Road}\n" +
                $"{location.Suburb}\n" +
                $"{location.City}, {location.State} {location.Postcode}\n" +
                $"{location.Country}\n" +
                $"Lat: {request.Latitude} | Lon: {request.Longitude}";

            // 5️⃣ Generate Google Maps QR
            var qrPath = await _qrService
                .GenerateGoogleMapsQrAsync(request.Latitude, request.Longitude);

            // 6️⃣ Stamp image
            var tempStampedPath = await _imageStamp.StampImageAsync(
                rawPath,
                stampText,
                qrPath,
                null);

            // 7️⃣ Save stamped permanently
            var finalStampedPath = await _fileStorage.SaveStampedAsync(
                tempStampedPath,
                request.Image.FileName);

            _logger.LogInformation("Upload completed: {File}", request.Image.FileName);

            return Ok(new
            {
                RawFilePath = rawPath,
                StampedFilePath = finalStampedPath,
                RawHash = rawHash,
                Signature = signature
            });
        }
    }
}