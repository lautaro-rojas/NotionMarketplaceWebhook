using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotionWebhookService.Models;
using NotionWebhookService.Services;
using System;
using System.Threading.Tasks;

namespace NotionWebhookService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<WebhookController> _logger;
        private readonly string _ownerEmail;
        private readonly IBackgroundTaskQueue _taskQueue;

        public WebhookController(IEmailService emailService, ILogger<WebhookController> logger, IConfiguration config, IBackgroundTaskQueue taskQueue)
        {
            _emailService = emailService;
            _logger = logger;
            _ownerEmail = config["OWNER_EMAIL"];
            _taskQueue = taskQueue;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] NotionPayload payload)
        {
            _logger.LogInformation("Webhook recibido");

            if (payload == null)
            {
                return BadRequest("Payload vacío.");
            }

            if (string.IsNullOrEmpty(payload.CustomerEmail))
            {
                return BadRequest("El payload no contiene customerEmail.");
            }

            bool isSpanish = !string.IsNullOrEmpty(payload.Locale) && payload.Locale.StartsWith("es", StringComparison.OrdinalIgnoreCase);
            bool isPaid = payload.TotalCustomerPayment.HasValue && payload.TotalCustomerPayment.Value > 0;

            // Construir correos
            // Correo al owner (notificación)
            string ownerAction = isPaid ? "Compra" : "Descarga";
            string ownerSubject = $"Se { (isPaid ? "compró" : "descargó") } tu plantilla de Notion";
            string ownerBody = $@"
                <p>AcquisitionId: <strong>{payload.AcquisitionId}</strong></p>
                <p>Acción: <strong>{ownerAction}</strong></p>
                <p>Fecha y hora: <strong>{payload.Time}</strong></p>
                <p>Usuario: <strong>{payload.CustomerEmail}</strong></p>
                <p>Plantilla: <strong>{payload.TemplateName}</strong></p>
                <p>Slug: <strong>{payload.TemplateSlug}</strong></p>
                <p>Idioma: <strong>{payload.Locale}</strong></p>
                <p>Cupón aplicado: <strong>{payload.CouponCode}</strong></p>
                <p>Precio de lista: <strong>{payload.ListingPrice}</strong></p>
                <p>Precio con descuento: <strong>{payload.DiscountedPrice}</strong></p>
                <p>Impuestos: <strong>{payload.TaxAmount}</strong></p>
                <p>Total pagado por el cliente: <strong>{payload.TotalCustomerPayment}</strong></p>
                <p>Monto de transferencia al vendedor: <strong>{payload.SellerTransferAmount}</strong></p>
                
            ";

            // Correo al cliente
            string userSubject = isPaid
                ? (isSpanish ? $"Gracias por comprar la plantilla: {payload.TemplateName}" : $"Thank you for your purchase: {payload.TemplateName}")
                : (isSpanish ? $"Gracias por descargar la plantilla: {payload.TemplateName}" : $"Thanks for downloading: {payload.TemplateName}");

            string actionText = isPaid ? (isSpanish ? "compra" : "purchase") : (isSpanish ? "descarga" : "download");
            string accessLink = (isSpanish ? "Abrir Plantilla" : "Open Template");
            string tipText = isSpanish ? "No olvides darle a 'Duplicate' para guardarla." : "Remember to click 'Duplicate' to save it.";

            string userBody = $@"
                <h1>{(isSpanish ? "Hola!" : "Hello!")}</h1>
                <p>{(isSpanish ? "Gracias por" : "Thanks for")} <strong>{(isPaid ? "tu compra de" : "downloading")}</strong> <strong>{payload.TemplateName}</strong>.</p>
                <ul>
                    <li><strong>Acceso:</strong> <a href='{(Request?.Host.HasValue == true ? $"{Request.Scheme}://{Request.Host}" : "")}/templates/{payload.TemplateName}'> {accessLink} </a></li>
                    <li><strong>Tip:</strong> {tipText}</li>
                </ul>
                <p>{(isSpanish ? "Saludos" : "Best")},<br/>Lautaro Rojas</p>
            ";

            // Encolar trabajo pesado y responder inmediatamente
            _taskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    // 1) Notificar al owner (si está configurado)
                    if (!string.IsNullOrEmpty(_ownerEmail))
                    {
                        await _emailService.SendEmailAsync(_ownerEmail, ownerSubject, ownerBody);
                        _logger.LogInformation($"Notificación enviada al owner: {_ownerEmail}");
                    }
                    else
                    {
                        _logger.LogWarning("OWNER_EMAIL no configurado. Se omite notificación al owner.");
                    }

                    /* TODO: Habilitar envío al cliente más adelante
                    // 2) Email al cliente
                    await _emailService.SendEmailAsync(payload.CustomerEmail, userSubject, userBody);
                    _logger.LogInformation($"Correo enviado al cliente: {payload.CustomerEmail}");
                    */
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando correos en background.");
                }
            });

            return Ok();
        }
    }
}