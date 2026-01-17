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
            Console.WriteLine(payload.AcquisitionId);
            Console.WriteLine(payload.Time);
            Console.WriteLine(payload.CouponCode);
            
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
            bool isCustomerEmailValid = payload.CustomerEmail.Contains("@") && payload.CustomerEmail.Contains(".") && payload.CustomerEmail.Length >= 5 && !string.IsNullOrWhiteSpace(payload.CustomerEmail);
            /*
            bool isPurchaseEvent = payload.Event == "marketplace.purchase";
            bool isNormalCustomer = payload.CustomerEmail.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase) ||
                              payload.CustomerEmail.EndsWith("@yahoo.com", StringComparison.OrdinalIgnoreCase) ||
                              payload.CustomerEmail.EndsWith("@outlook.com", StringComparison.OrdinalIgnoreCase) ||
                              payload.CustomerEmail.EndsWith("@hotmail.com", StringComparison.OrdinalIgnoreCase);
            */

            // Construir correos
            // Correo al owner (notificación)
            string ownerAction = isPaid ? "Compra" : "Descarga";
            string ownerSubject = $"Se { (isPaid ? "compró" : "descargó") } tu plantilla de Notion";
            string ownerBody = $@"
                <p>AcquisitionId: <strong>{payload.AcquisitionId}</strong></p>
                <p>Acción: <strong>{ownerAction}</strong></p>
                <p>Fecha y hora: <strong>{payload.Time}</strong></p>
                <p>Usuario: <strong>{(isCustomerEmailValid ? $"{payload.CustomerEmail}" : "Sin correo")}</strong></p>
                <p>Plantilla: <strong>{payload.TemplateName}</strong></p>
                <p>Slug: <strong>{payload.TemplateSlug}</strong></p>
                <p>Idioma: <strong>{payload.Locale}</strong></p>
                <p>Cupón aplicado: <strong>{payload.CouponCode}</strong></p>
                <p>Precio de lista: <strong>{payload.ListingPrice} USD</strong></p>
                <p>Precio con descuento: <strong>{payload.DiscountedPrice} USD</strong></p>
                <p>Impuestos: <strong>{payload.TaxAmount} USD</strong></p>
                <p>Total pagado por el cliente: <strong>{payload.TotalCustomerPayment} USD</strong></p>
                <p>Monto de transferencia al vendedor: <strong>{payload.SellerTransferAmount} USD</strong></p>
            ";

            // Correo al cliente
            string userSubject;
            string userBody;
            string emailSignature = $@"
                <p>{(isSpanish ? "Gracias totales!" : "Best!")}<p>
                <span class='gmail_signature_prefix'>-- </span>
                <div dir='ltr' class='gmail_signature' data-smartmail='gmail_signature'>
                    <div dir='ltr'>Lautaro Rojas - Notion Builder - <a href='https://www.notion.com/@lautaro_rojas' target='_blank';source=gmail&amp;>{(isSpanish ? "Perfil de Notion Marketplace" : "Notion Marketplace profile")}</a>
                    </div>
                </div>
            ";

            if (isPaid)
            {
                userSubject = isSpanish
                    ? $"Gracias por comprar la plantilla: {payload.TemplateName}"
                    : $"Thank you for purchasing the template: {payload.TemplateName}";

                userBody = $@"
                    <h1>{(isSpanish ? "Hola! 👋" : "Hello! 👋")}</h1>
                    <p>{(isSpanish ? "Gracias por comprar la plantilla " : "Thanks for purchasing the template ")} <strong>{payload.TemplateName}</strong>! {(isSpanish ? "Me alegra mucho que te haya interesado." : "I'm so glad you were interested.")}</p>
                    <p>{(isSpanish ? "He diseñado esta plantilla para que puedas enfocarte en lo importante y eliminar el ruido de forma sencilla y eficiente. Espero que te aporte mucho valor desde el primer día." : "I built this tool to help you focus on what matters and clear the clutter in a simple and effective way. I hope you find it valuable!")}</p>
                    <P>{(isSpanish ? "Si te resulta útil, me ayudarías muchísimo escribiendo una valoración en Notion Marketplace." : "If you find it useful, it would help me a lot to write a review on Notion Marketplace.")}</p>
                    <p>{(isSpanish ? "Si tienes alguna pregunta o necesitas ayuda, no dudes en contactarme respondiendo a este correo." : "If you have any questions or need assistance, feel free to reach out by replying to this email.")}</p>
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
                    <p>{(isSpanish ? "He diseñado esta plantilla para que puedas enfocarte en lo importante y eliminar el ruido de forma sencilla y eficiente. Espero que te aporte mucho valor desde el primer día." : "I built this tool to help you focus on what matters and clear the clutter in a simple and effective way. I hope you find it valuable!")}</p>
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
                        // await _emailService.SendEmailAsync(_ownerEmail, ownerSubject, ownerBody);
                        _logger.LogInformation($"1) Notificación enviada al owner: {_ownerEmail}");
                    }
                    else
                    {
                        _logger.LogWarning("1) OWNER_EMAIL no configurado. Se omite notificación al owner.");
                    }

                    // 2) Email al cliente
                    if (isCustomerEmailValid)
                    {
                        _logger.LogInformation($"2) Correo del cliente válido. Procediendo a envío.");
                        // await _emailService.SendEmailAsync(payload.CustomerEmail, userSubject, userBody);
                        _logger.LogInformation($"3) Correo enviado al cliente: {payload.CustomerEmail}");
                    }
                    else
                    {
                       _logger.LogWarning($"2) Correo del cliente no válido. Se omite envío al cliente.");
                        return;
                    }
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