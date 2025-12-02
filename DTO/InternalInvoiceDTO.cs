using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DispatchPublic.DTO
{
    public class ValidateInvoicePortalTokenResponseDTO
    {
        [JsonPropertyName("is_valid")]
        public bool IsValid { get; set; }
        [JsonPropertyName("error")]
        public string? Error { get; set; }
        [JsonPropertyName("invoice_id")]
        public Guid? InvoiceId { get; set; }
        [JsonPropertyName("pdf_storage_key")]
        public string? PdfStorageKey { get; set; }

        [JsonPropertyName("invoice_number")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("invoice_date")]
        public DateTime? InvoiceDate { get; set; }

        [JsonPropertyName("invoice_due_date")]
        public DateTime? InvoiceDueDate { get; set; }

        [JsonPropertyName("signature_path")]
        public string? SignaturePath { get; set; }

        public bool Viewed { get; set; }
    }

    public class SubmitSignatureDTO
    {
        [Required]
        [StringLength(5000)]
        [JsonPropertyName("signature_path")]
        public string SignaturePath { get; set; } = string.Empty;
    }
}
