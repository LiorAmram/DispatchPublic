using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DispatchPublic.DTO;

public class PublicInvoiceResponseDto
{
    [Required]
    public Guid InvoiceId { get; set; }

    [Required]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    public string Date { get; set; } = string.Empty;

    public string DueDate { get; set; }

    public string SignaturePath { get; set;  } = string.Empty;

    public bool Viewed { get; set; } = false;
}

public class SubmitSignatureRequestDto
{
    [Required]
    [StringLength(5000)]
    public string SignaturePath { get; set; } = string.Empty;
}

public class PublicActionResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
