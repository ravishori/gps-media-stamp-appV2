using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace GpsMediaStamp.Web.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled Exception Occurred");

                await SendErrorEmailAsync(ex, context);

                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsync(
                        "{\"error\":\"Internal server error\"}");
                }
                else
                {
                    context.Response.Clear();
                    context.Response.Redirect("/Error");
                }
            }
        }

        private async Task SendErrorEmailAsync(Exception ex, HttpContext context)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(
                        "ravishori@gmail.com",
                        "nuva likv pfnw twlg"),
                    EnableSsl = true
                };

                var body = new StringBuilder();
                body.AppendLine("New Exception Occurred");
                body.AppendLine("---------------------------");
                body.AppendLine($"Time: {DateTime.Now}");
                body.AppendLine($"Path: {context.Request.Path}");
                body.AppendLine($"Message: {ex.Message}");
                body.AppendLine($"StackTrace: {ex.StackTrace}");

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("ravishori@gmail.com "),
                    Subject = "🚨 GpsMediaStamp System Error",
                    Body = body.ToString(),
                    IsBodyHtml = false
                };

                mailMessage.To.Add("ravi.shori.work@gmail.com");

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send error email.");
            }
        }
    }
}