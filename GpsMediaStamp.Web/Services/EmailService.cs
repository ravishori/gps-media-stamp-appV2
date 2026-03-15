using GpsMediaStamp.Web.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace GpsMediaStamp.Web.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string subject, string body)
        {
            using var smtpClient = new SmtpClient(_settings.SmtpServer, _settings.Port)
            {
                Credentials = new NetworkCredential(
                    _settings.SenderEmail,
                    _settings.SenderPassword),

                EnableSsl = true
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail),
                Subject = subject,
                Body = body
            };

            mailMessage.To.Add(_settings.ReceiverEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}