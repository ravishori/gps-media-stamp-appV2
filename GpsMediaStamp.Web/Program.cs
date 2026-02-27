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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
// 🔒 GLOBAL 100MB REQUEST LIMIT
//
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
    options.ListenAnyIP(5039);
});

//
// 🔧 CORE SERVICES
//
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Location Service (OpenStreetMap)
builder.Services.AddHttpClient<GoogleGeocodingService>();
builder.Services.AddScoped<IEmailAlertService, EmailAlertService>(); 
builder.Services.AddHttpClient<ILocationService, GoogleGeocodingService>();
builder.Services.AddScoped<IImageStampService, ImageOverlayService>();
builder.Services.AddScoped<FFmpegVideoProcessor>();
builder.Services.AddRazorPages();
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();
//
// 🌍 CORS POLICY
//
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7124",
                "http://localhost:5039"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

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
// 🛡 MIDDLEWARE
//
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapRazorPages();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.Run();