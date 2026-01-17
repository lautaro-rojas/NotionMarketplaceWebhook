using System.Text.Json.Serialization;

namespace NotionWebhookService.Models
{
    public class NotionPayload
    {
        [JsonPropertyName("acquisitionId")]
        public string? AcquisitionId { get; set; }

        [JsonPropertyName("event")]
        public string? Event { get; set; } // "marketplace.purchase" (compra) o "marketplace.refund" (reembolso)
        
        [JsonPropertyName("time")]
        public long Time 
        { get; set; } // La fecha y hora en que se realizó la transacción del Marketplace.
        
        [JsonPropertyName("customerEmail")]
        public string? CustomerEmail { get; set; }
        
        [JsonPropertyName("templateName")]
        public string? TemplateName { get; set; }
        
        [JsonPropertyName("templateSlug")]
        public string? TemplateSlug { get; set; }
        
        [JsonPropertyName("locale")]
        public string? Locale { get; set; } // Idioma del cliente. ej: "en-us", "es-la"
        
        [JsonPropertyName("couponCode")]
        public string? CouponCode { get; set; } // Código de cupón aplicado, si corresponde
        
        [JsonPropertyName("listingPrice")]
        public long? ListingPrice { get; set; } // Precio la plantilla publicada en el marketplace, en centavos
        
        [JsonPropertyName("discountedPrice")]
        public long? DiscountedPrice { get; set; } // Precio con descuento aplicado
        
        [JsonPropertyName("taxAmount")]
        public long? TaxAmount { get; set; } // Impuestos aplicados
        
        [JsonPropertyName("totalCustomerPayment")]
        public long? TotalCustomerPayment { get; set; } // Pago total del cliente. Usar para decidir compra vs descarga. Si es 0, es descarga gratuita.
        
        [JsonPropertyName("sellerTransferAmount")]
        public long? SellerTransferAmount { get; set; } // Monto transferido al vendedor

        public DateTime EventDate
        {
            get 
            {
                // Convierte los milisegundos a un objeto fecha UTC
                return DateTimeOffset.FromUnixTimeMilliseconds(Time).UtcDateTime;
            }
        }
    }   
}