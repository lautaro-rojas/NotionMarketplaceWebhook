namespace NotionWebhookService.Models
{
    public class NotionPayload
    {
        public string AcquisitionId { get; set; }
        public string Event { get; set; } // "marketplace.purchase" o "marketplace.refund"
        public string CustomerEmail { get; set; }
        public string TemplateName { get; set; }
        public string TemplateSlug { get; set; }
        public string Locale { get; set; } // ej: "en-US", "es-LA"
        public long? ListingPrice { get; set; } // centavos
        public long? TotalCustomerPayment { get; set; } // centavos, usar para decidir compra vs descarga
        public long? SellerTransferAmount { get; set; } 
        
    }
}