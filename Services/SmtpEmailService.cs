using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace NotionWebhookService.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            var smtpHost = _config["SMTP_HOST"] ?? "smtp.gmail.com";
            var smtpPort = 587;
            if (!string.IsNullOrEmpty(_config["SMTP_PORT"]))
            {
                int.TryParse(_config["SMTP_PORT"], out smtpPort);
            }

            var smtpUser = _config["SMTP_USER"];
            var smtpPass = _config["SMTP_PASS"];

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUser, _config["SMTP_FROM_NAME"] ?? "Notion Marketplace"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);
            await client.SendMailAsync(mailMessage);
        }
    }
}