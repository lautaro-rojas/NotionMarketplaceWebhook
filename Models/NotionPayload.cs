namespace NotionWebhookService.Models
{
    public class NotionPayload
    {
        public string? AcquisitionId { get; set; }
        public string? Event { get; set; } // "marketplace.purchase" (compra) o "marketplace.refund" (reembolso)
        public DateTime? Time { get; set; } // La fecha y hora en que se realizó la transacción del Marketplace.
        public string? CustomerEmail { get; set; }
        public string? TemplateName { get; set; }
        public string? TemplateSlug { get; set; }
        public string? Locale { get; set; } // Idioma del cliente. ej: "en-US", "es-LA"
        public string? CouponCode { get; set; } // Código de cupón aplicado, si corresponde
        public long? ListingPrice { get; set; } // Precio la plantilla publicada en el marketplace, en centavos
        public long? DiscountedPrice { get; set; } // Precio con descuento aplicado
        public long? TaxAmount { get; set; } // Impuestos aplicados
        public long? TotalCustomerPayment { get; set; } // Pago total del cliente. Usar para decidir compra vs descarga. Si es 0, es descarga gratuita.
        public long? SellerTransferAmount { get; set; } // Monto transferido al vendedor
    }
}