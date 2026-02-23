using GpsMediaStamp.Web.Models;
using GpsMediaStamp.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using GpsMediaStamp.Web.Middleware;



var builder = WebApplication.CreateBuilder(args);

// 🔥 Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// 🔒 Global 100MB request limit
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
});
// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register services
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IHashService, HashService>();
builder.Services.AddScoped<IVideoStampService, VideoStampService>();
builder.Services.AddHttpClient<ILocationService, OpenStreetMapLocationService>();
builder.Services.AddScoped<IGoogleMapsQrService, GoogleMapsQrService>(); builder.Services.AddSingleton<ISigningService, RsaSigningService>();
builder.Services.AddHttpClient<ILocationService, OpenStreetMapLocationService>();
builder.Services.AddScoped<IGoogleMapsQrService, GoogleMapsQrService>();
builder.Services.AddScoped<IImageStampService, ImageStampService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.WithOrigins(
                   "https://localhost:7124",
                   "http://localhost:5039"
               )
           .AllowAnyHeader()
            .AllowAnyMethod();
        });
});
// 🔥 THIS LINE IS REQUIRED
builder.Services.AddScoped<IImageStampService, ImageStampService>();
//////////////////////////////////////////
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5039);
});


var app = builder.Build();

//////////////////////////////////////////

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<GlobalExceptionMiddleware>();
// app.UseHttpsRedirection();   🔥 Disable for now
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();