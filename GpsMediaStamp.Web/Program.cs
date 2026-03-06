using GpsMediaStamp.Application.Interfaces;
using GpsMediaStamp.Application.Interfaces.Common;
using GpsMediaStamp.Application.Interfaces.Image;
using GpsMediaStamp.Application.Interfaces.Qr;
using GpsMediaStamp.Application.Interfaces.Security;
using GpsMediaStamp.Application.Interfaces.Video;
using GpsMediaStamp.Infrastructure.Services;
using GpsMediaStamp.Infrastructure.Services.Common;
using GpsMediaStamp.Infrastructure.Services.Email;
using GpsMediaStamp.Infrastructure.Services.Media;
using GpsMediaStamp.Infrastructure.Services.Qr;
using GpsMediaStamp.Infrastructure.Services.Security;
using GpsMediaStamp.Infrastructure.Services.Video;
using GpsMediaStamp.Web.Middleware;

using Microsoft.Extensions.FileProviders;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

//
// 🔥 SERILOG CONFIGURATION
//
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

//
// 🔒 GLOBAL REQUEST LIMIT (500MB)
//
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024;
    options.ListenAnyIP(5039);
});

//
// 🔧 CORE SERVICES
//
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRazorPages();

//
// 📦 BUSINESS SERVICES
//

// Storage & Hashing
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IHashService, HashService>();

// Signing
builder.Services.AddSingleton<ISigningService, RsaSigningService>();

// QR Services
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IGoogleMapsQrService, GoogleMapsQrService>();

// Media Services
builder.Services.AddScoped<IVideoStampService, VideoStampService>();
builder.Services.AddScoped<IImageStampService, ImageOverlayService>();
builder.Services.AddScoped<FFmpegVideoProcessor>();

// Location Service
builder.Services.AddHttpClient<GoogleGeocodingService>();
builder.Services.AddHttpClient<ILocationService, GoogleGeocodingService>();

// Email Service
builder.Services.AddScoped<IEmailAlertService, EmailAlertService>();

//
// 🌍 CORS POLICY
//
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

//
// ⚙ CONFIGURATION
//
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddControllers();


var app = builder.Build();

//
// 🧪 DEVELOPMENT TOOLS
//
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//
// 📂 STATIC FILE CONFIGURATION (CRITICAL PART)
// Makes /storage publicly accessible
//
var storagePath = Path.Combine(Directory.GetCurrentDirectory(), "storage");

if (!Directory.Exists(storagePath))
{
    Directory.CreateDirectory(storagePath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storagePath),
    RequestPath = "/storage"
});

//
// 🛡 MIDDLEWARE PIPELINE
//
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();