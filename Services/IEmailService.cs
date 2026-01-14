using System.Threading.Tasks;

namespace NotionWebhookService.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody);
    }
}