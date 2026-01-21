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
            //Console.WriteLine(payload.Time.ToString());
            //Console.WriteLine(payload.EventDate.ToString("dd/MM/yyyy HH:mm:ss"));
            //Console.WriteLine(payload.Event.ToString());
            
            _logger.LogInformation("Webhook recibido");

            if (payload == null)
            {
                return BadRequest("Payload vacío.");
            }
            
            bool isCustomerEmailValid = payload.CustomerEmail.Contains("@") && payload.CustomerEmail.Contains(".") && !string.IsNullOrWhiteSpace(payload.CustomerEmail);

            /*
            bool isNormalCustomer = payload.CustomerEmail.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase) ||
                              payload.CustomerEmail.EndsWith("@yahoo.com", StringComparison.OrdinalIgnoreCase) ||
                              payload.CustomerEmail.EndsWith("@outlook.com", StringComparison.OrdinalIgnoreCase) ||
                              payload.CustomerEmail.EndsWith("@hotmail.com", StringComparison.OrdinalIgnoreCase);
            */

            // Encolar trabajo pesado y responder inmediatamente
            _taskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    switch (payload.Event)
                    {
                        case "marketplace.purchase":
                            _logger.LogInformation("Procesando evento de compra...");
                            if (!string.IsNullOrEmpty(_ownerEmail))
                            {
                                await _emailService.SendEmailAsync(_ownerEmail, OwnerSubjectBuilder(payload), OwnerBodyBuilder(payload));
                                _logger.LogInformation($"Notificación enviada al owner: {_ownerEmail}");
                            }
                            else
                            {
                                _logger.LogWarning("OWNER_EMAIL no configurado. Se omite notificación al owner.");
                            }

                            if (isCustomerEmailValid)
                            {
                                _logger.LogInformation($"Correo del cliente válido. Procediendo a envío.");
                                await _emailService.SendEmailAsync(payload.CustomerEmail, CustomerSubjectBuilder(payload), CustomerBodyBuilder(payload));
                                _logger.LogInformation($"Notificación enviada al cliente.");
                            }
                            else
                            {
                                _logger.LogWarning($"Correo del cliente no válido. Se omite envío al cliente.");
                            }
                            break;

                        case "marketplace.refund":
                            _logger.LogInformation("Procesando evento de reembolso...");
                            await _emailService.SendEmailAsync(_ownerEmail, OwnerSubjectBuilder(payload), OwnerBodyBuilder(payload));
                            _logger.LogInformation($"Notificación enviada al owner: {_ownerEmail}");
                            await _emailService.SendEmailAsync(payload.CustomerEmail, CustomerSubjectBuilder(payload), CustomerBodyBuilder(payload));
                            _logger.LogInformation($"Notificación enviada al cliente.");

                            break;

                        case "webhook.test":
                            _logger.LogInformation("Procesando evento de testeo...");
                            _logger.LogInformation($"Notificación enviada al owner: {_ownerEmail}");
                            await _emailService.SendEmailAsync(_ownerEmail, OwnerSubjectBuilder(payload), OwnerBodyBuilder(payload));
                            break;

                        default:
                            _logger.LogInformation($"Evento no capturado: {payload.Event}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando correos en background.");
                }
            });
            
            return Ok();
        }

        private string OwnerSubjectBuilder(NotionPayload payload)
        {
            switch (payload.Event)
            {
                case "marketplace.purchase":
                    return $"Se {(payload.TotalCustomerPayment.HasValue && payload.TotalCustomerPayment.Value > 0 ? "compró" : "descargó")} tu plantilla de Notion";
                case "marketplace.refund":
                    return "Se reembolsó tu plantilla de Notion";
                case "webhook.test":
                    return "Notificación de prueba";
                default:
                    return "Evento desconocido";
            }
        }

        private string OwnerBodyBuilder(NotionPayload payload)
        {
            string action;

            switch (payload.Event)
            {
                case "marketplace.purchase":
                    action = payload.TotalCustomerPayment.HasValue && payload.TotalCustomerPayment.Value > 0 ? "Compra" : "Descarga";
                    break;
                case "marketplace.refund":
                   action = "Reembolso";
                    break;
                case "webhook.test":
                    action = "Testeo";
                    break;
                default:
                    return "Evento desconocido del webhook de Notion Marketplace";
            }
            
            return $@"
                <p>AcquisitionId: <strong>{payload.AcquisitionId}</strong></p>
                <p>Acción: <strong>{action}</strong></p> 
                <p>Fecha y hora: <strong>{payload.EventDate.ToString("dd/MM/yyyy HH:mm:ss")}</strong></p>
                <p>Cliente: <strong>{payload.CustomerEmail}</strong></p>
                <p>Plantilla: <strong>{payload.TemplateName}</strong></p>
                <p>Slug: <strong>{payload.TemplateSlug}</strong></p>
                <p>Idioma: <strong>{payload.Locale}</strong></p>
                <p>Cupón aplicado: <strong>{payload.CouponCode}</strong></p>
                <p>Precio de lista: <strong>{payload.ListingPrice / 100.0m} USD</strong></p>
                <p>Precio con descuento: <strong>{payload.DiscountedPrice / 100.0m} USD</strong></p>
                <p>Impuestos: <strong>{payload.TaxAmount / 100.0m} USD</strong></p>
                <p>Total pagado por el cliente: <strong>{payload.TotalCustomerPayment / 100.0m} USD</strong></p>
                <p>Monto de transferencia al vendedor: <strong>{payload.SellerTransferAmount / 100.0m} USD</strong></p>
            ";
        }

        private string CustomerSubjectBuilder(NotionPayload payload)
        {
            string customerSubject;
            bool isPaid = payload.TotalCustomerPayment.HasValue && payload.TotalCustomerPayment.Value > 0;
            bool isSpanish = !string.IsNullOrEmpty(payload.Locale) && payload.Locale.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            switch (payload.Event)
            {
                case "marketplace.purchase":
                    if (isPaid)
                    {
                        customerSubject = isSpanish
                            ? $"Gracias por comprar la plantilla: {payload.TemplateName}"
                            : $"Thank you for purchasing the template: {payload.TemplateName}";
                    }
                    else
                    {
                        customerSubject = isSpanish
                            ? $"Gracias por descargar la plantilla: {payload.TemplateName}"
                            : $"Thank you for downloading the template: {payload.TemplateName}";
                    }
                    break;
                case "marketplace.refund":
                    customerSubject = isSpanish
                        ? $"Reembolso de la plantilla: {payload.TemplateName}"
                        : $"Refund for the template: {payload.TemplateName}";
                    break;
                case "webhook.test":
                    customerSubject = isSpanish
                        ? $"Testeo de la plantilla: {payload.TemplateName}"
                        : $"Test for the template: {payload.TemplateName}";
                    break;
                default:
                    return "Comunicarse con lautaro.rojas02@gmail.com";
            }
            return customerSubject;
        }
        
        private string CustomerBodyBuilder(NotionPayload payload)
        {
            string customerBody;
            bool isPaid = payload.TotalCustomerPayment.HasValue && payload.TotalCustomerPayment.Value > 0;
            bool isSpanish = !string.IsNullOrEmpty(payload.Locale) && payload.Locale.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            string emailSignature = $@"
                <p>{(isSpanish ? "Gracias totales!" : "Best!")}<p>
                <span class='gmail_signature_prefix'>-- </span>
                <div dir='ltr' class='gmail_signature' data-smartmail='gmail_signature'>
                    <div dir='ltr'>Lautaro Rojas - Notion Builder - <a href='https://www.notion.com/@lautaro_rojas' target='_blank';source=gmail&amp;>{(isSpanish ? "Perfil de Notion Marketplace" : "Notion Marketplace profile")}</a>
                    </div>
                </div>
            ";

            switch (payload.Event)
            {
                case "marketplace.purchase":
                    if (isPaid)
                    {
                        customerBody = $@"
                        <h1>{(isSpanish ? "Hola! 👋" : "Hello! 👋")}</h1>
                        <p>{(isSpanish ? "Gracias por comprar la plantilla " : "Thanks for purchasing the template ")} <strong>{payload.TemplateName}</strong>! {(isSpanish ? "Me alegra mucho que te haya interesado." : "I'm so glad you were interested.")}</p>
                        <p>{(isSpanish ? "He diseñado esta plantilla para que puedas enfocarte en lo importante y eliminar el ruido de forma sencilla y eficiente. Espero que te aporte mucho valor desde el primer día." : "I built this tool to help you focus on what matters and clear the clutter in a simple and effective way. I hope you find it valuable!")}</p>
                        <p>{(isSpanish ? "Si te resulta útil, me ayudarías muchísimo escribiendo una valoración en Notion Marketplace." : "If you find it useful, it would help me a lot to write a review on Notion Marketplace.")}</p>
                        <p>{(isSpanish ? "Si tienes alguna pregunta o necesitas ayuda, no dudes en contactarme respondiendo a este correo." : "If you have any questions or need assistance, feel free to reach out by replying to this email.")}</p>
                        " + emailSignature;
                    }
                    else
                    {
                        customerBody = $@"
                        <h1>{(isSpanish ? "Hola! 👋" : "Hello! 👋")}</h1>
                        <p>{(isSpanish ? "Gracias por descargar la plantilla " : "Thanks for downloading the template ")} <strong>{payload.TemplateName}</strong>! {(isSpanish ? "Me alegra mucho que te haya interesado." : "I'm so glad you were interested.")}</p>
                        <p>{(isSpanish ? "He diseñado esta plantilla para que puedas enfocarte en lo importante y eliminar el ruido de forma sencilla y eficiente. Espero que te aporte mucho valor desde el primer día." : "I built this tool to help you focus on what matters and clear the clutter in a simple and effective way. I hope you find it valuable!")}</p>
                        <p>{(isSpanish ? "Si te resulta útil, me ayudarías muchísimo escribiendo una valoración en Notion Marketplace." : "If you find it useful, it would help me a lot to write a review on Notion Marketplace.")}</p>
                        <p>{(isSpanish ? "Si tienes alguna pregunta o necesitas ayuda, no dudes en contactarme respondiendo a este correo." : "If you have any questions or need assistance, feel free to reach out by replying to this email.")}</p>
                        " + emailSignature;
                    }
                    break;

                case "marketplace.refund":
                    customerBody = $@"
                        <h1>{(isSpanish ? "Hola! 👋" : "Hello! 👋")}</h1>
                        <p>{(isSpanish ? "Te escribo para confirmarte que he recibido tu solicitud de reembolso para la plantilla " : "I am writing to confirm that I have received your refund request for the template ")} <strong>{payload.TemplateName}</strong>.</p>
                        <p>{(isSpanish ? "La devolución ya ha sido procesada por mi parte. Notion Marketplace gestionará el reintegro a tu método de pago original (generalmente tarda entre 5 y 10 días hábiles dependiendo de tu banco)." : "The refund has already been processed on my end. Notion Marketplace will handle the refund to your original payment method (this usually takes 5-10 business days depending on your bank).")}</p> 
                        <p>{(isSpanish ? "Entiendo perfectamente que no todas las plantillas encajan con el flujo de trabajo de cada persona. Sin embargo, como creador independiente, valoro mucho la honestidad. ¿Te importaría contarme brevemente qué fue lo que no te funcionó o qué esperabas encontrar?" : "I completely understand that not all templates fit everyone's workflow. However, as an independent creator, I greatly value honesty. Would you mind briefly telling me what didn't work for you or what you were expecting?")}</p>
                        <p>{(isSpanish ? "Tu opinión me ayuda muchísimo a mejorar mis productos para futuras versiones." : "Your feedback helps me tremendously in improving my products for future versions.")}</p>
                        <p>{(isSpanish ? "Gracias por darle una oportunidad a mi trabajo." : "Thank you for giving my work a chance.")}</p>
                        " + emailSignature;
                    break;

                case "webhook.test":
                    customerBody = $@"
                        <h1>{(isSpanish ? "Hola! 👋" : "Hello! 👋")}</h1>
                        <p>{(isSpanish ? "Testeo de body del correo del cliente." : "Testing the body of the client's email.")}</p>
                        " + emailSignature;
                    break;

                default:
                    return "Comunicarse con lautaro.rojas02@gmail.com";
            }
            
            return customerBody;
        }
    }
}