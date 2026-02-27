using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using GpsMediaStamp.Application.Interfaces.Security;
namespace GpsMediaStamp.Infrastructure.Services.Email
{
    public class EmailAlertService : IEmailAlertService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailAlertService> _logger;

        public EmailAlertService(
            IConfiguration config,
            ILogger<EmailAlertService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string subject, string body)
        {
            try
            {
                var host = _config["Smtp:Host"];

                var portString = _config["Smtp:Port"];

                if (string.IsNullOrWhiteSpace(portString))
                {
                    _logger.LogError("SMTP Port is missing in configuration.");
                    return;
                }

                if (!int.TryParse(portString, out int port))
                {
                    _logger.LogError("SMTP Port is invalid.");
                    return;
                }
                var username = _config["Smtp:Username"];
                var password = _config["Smtp:Password"];
                var from = _config["Smtp:From"];
                var to = _config["Smtp:To"];

                using var smtpClient = new SmtpClient(host)
                {
                    Port = port,
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(from),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                mailMessage.To.Add(to);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sending failed.");
                // IMPORTANT: Do NOT rethrow
            }
        }
    }
}