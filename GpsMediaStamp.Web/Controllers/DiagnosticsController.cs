using GpsMediaStamp.Application.Interfaces.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GpsMediaStamp.Web.Controllers
{
    /// <summary>
    /// Health-check and diagnostic endpoints.
    /// </summary>
    [ApiController]
    [Route("api/diagnostics")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly IEmailAlertService  _emailService;
        private readonly IConfiguration      _config;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(
            IEmailAlertService  emailService,
            IConfiguration      config,
            ILogger<DiagnosticsController> logger)
        {
            _emailService = emailService;
            _config       = config;
            _logger       = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET api/diagnostics/email-health
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Send a test email to verify that Twilio SendGrid (and SMTP fallback)
        /// are correctly configured and operational.
        /// </summary>
        /// <remarks>
        /// Successful response means the email was submitted to the provider
        /// without error. Check your inbox to confirm delivery.
        /// </remarks>
        [HttpGet("email-health")]
        public async Task<IActionResult> EmailHealth()
        {
            var sw = Stopwatch.StartNew();

            _logger.LogInformation("Email health-check triggered from {IP}",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

            try
            {
                var subject = "✅ GpsMediaStamp — Email Service Health Check";

                var html = $@"
<html>
<head>
  <style>
    body  {{ font-family: Arial, sans-serif; padding: 24px; background:#f9f9f9; }}
    h2    {{ color: #27ae60; }}
    table {{ border-collapse:collapse; width:100%; }}
    td, th{{ padding:8px 12px; border:1px solid #ddd; }}
    th    {{ background:#2c3e50; color:#fff; text-align:left; }}
  </style>
</head>
<body>
  <h2>&#x2705; GpsMediaStamp Email Service Health Check</h2>
  <p>This is an automated test email. If you received it, the email alert
     pipeline is working correctly.</p>
  <table>
    <tr><th>Property</th><th>Value</th></tr>
    <tr><td>Sent At (UTC)</td><td>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}</td></tr>
    <tr><td>Server Host</td><td>{Request.Host}</td></tr>
    <tr><td>Environment</td><td>{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"}</td></tr>
    <tr><td>SendGrid Configured</td><td>{IsSendGridConfigured()}</td></tr>
    <tr><td>SMTP Configured</td><td>{IsSmtpConfigured()}</td></tr>
    <tr><td>Triggered By IP</td><td>{HttpContext.Connection.RemoteIpAddress}</td></tr>
  </table>
</body>
</html>";

                await _emailService.SendErrorReportAsync(subject, html);

                sw.Stop();

                _logger.LogInformation(
                    "Email health-check completed in {Ms}ms.", sw.ElapsedMilliseconds);

                return Ok(new
                {
                    status      = "healthy",
                    message     = "Test email submitted successfully — check your inbox.",
                    elapsedMs   = sw.ElapsedMilliseconds,
                    timestamp   = DateTime.UtcNow,
                    sendGridConfigured = IsSendGridConfigured(),
                    smtpConfigured     = IsSmtpConfigured(),
                });
            }
            catch (Exception ex)
            {
                sw.Stop();

                _logger.LogError(ex,
                    "Email health-check FAILED in {Ms}ms: {Msg}",
                    sw.ElapsedMilliseconds, ex.Message);

                return StatusCode(500, new
                {
                    status    = "unhealthy",
                    message   = ex.Message,
                    elapsedMs = sw.ElapsedMilliseconds,
                    timestamp = DateTime.UtcNow,
                });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET api/diagnostics/ping
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Simple liveness probe — returns 200 OK immediately.</summary>
        [HttpGet("ping")]
        public IActionResult Ping() =>
            Ok(new
            {
                status    = "ok",
                timestamp = DateTime.UtcNow,
                host      = Request.Host.Value,
            });

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private bool IsSendGridConfigured()
        {
            var key = _config["SendGrid:ApiKey"];
            return !string.IsNullOrWhiteSpace(key) &&
                   !key.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSmtpConfigured()
        {
            return !string.IsNullOrWhiteSpace(_config["Smtp:Host"]) &&
                   !string.IsNullOrWhiteSpace(_config["Smtp:Username"]) &&
                   !string.IsNullOrWhiteSpace(_config["Smtp:Password"]);
        }
    }
}
