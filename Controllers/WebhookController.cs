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
            string userSubject;
            string userBody;
            string emailSignature = $@"
                <p>{(isSpanish ? "Gracias totales!" : "Best!")}<p>
                <span class='gmail_signature_prefix'>-- </span>
                <div dir='ltr' class='gmail_signature' data-smartmail='gmail_signature'>
                    <div dir='ltr'>Lautaro Rojas - Notion Builder - <a href='https://www.notion.com/@lautaro_rojas' target='_blank';source=gmail&amp;>Marketplace</a>
                        <div>
                            <img width='85' height='88' src=''https://notionmarketplacewebhook.72.60.155.43.nip.io/Images/Logo-block-stickerized.png' style='margin-right:0px' class='CToWUd' data-bit='iit'>
                        </div>
                    </div>
                </div>
            ";

            if (isPaid)
            {
                userSubject = isSpanish
                    ? $"Gracias por comprar la plantilla: {payload.TemplateName}"
                    : $"Thank you for purchasing the template: {payload.TemplateName}";

                userBody = $@"
                    <h1>{(isSpanish ? "Hola!" : "Hello!")}</h1>
                    <p>{(isSpanish ? "Gracias por tu compra de" : "Thanks for purchasing")} <strong>{payload.TemplateName}</strong>.</p>
                    <ul>
                        <li><strong>Tip:</strong> {(isSpanish ? "No olvides hacer clic en 'Duplicate' para guardarla." : "Remember to click 'Duplicate' to save it.")}</li>
                    </ul>
                    " + emailSignature;
            }
            else
            {
                userSubject = isSpanish
                    ? $"Gracias por descargar la plantilla: {payload.TemplateName}"
                    : $"Thank you for downloading the template: {payload.TemplateName}";

                userBody = $@"
                    <h1>{(isSpanish ? "Hola! 👋" : "Hello! 👋")}</h1>
                    <p>{(isSpanish ? "Gracias por descargar la plantilla " : "Thanks for downloading the template ")} <strong>{payload.TemplateName}</strong>! {(isSpanish ? "Me alegra mucho que te haya interesado." : "I'm so glad you were interested.")}</p>
                    <p>{(isSpanish ? "He diseñado esta herramienta para que puedas enfocarte en lo importante y eliminar el ruido de forma sencilla y eficiente. Espero que te aporte mucho valor desde el primer día." : "I built this tool to help you focus on what matters and clear the clutter in a simple and effective way. I hope you find it valuable!")}</p>
                    <ul>
                        <li><strong>Tip:</strong> {(isSpanish ? "No olvides hacer clic en 'Duplicate' para guardarla." : "Remember to click 'Duplicate' to save it.")}</li>
                    </ul>
                    <P>{(isSpanish ? "Si te resulta útil, me ayudarías muchísimo escribiendo una valoración en Notion Marketplace." : "If you find it useful, it would help me a lot to write a review on Notion Marketplace.")}</p>
                    <p>{(isSpanish ? "Si tienes alguna pregunta o necesitas ayuda, no dudes en contactarme respondiendo a este correo." : "If you have any questions or need assistance, feel free to reach out by replying to this email.")}</p>
                    " + emailSignature;
            }

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

                    /* TODO: Habilitar envío al cliente en producción
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