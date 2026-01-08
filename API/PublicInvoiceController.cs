using DispatchPublic.DTO;
using DispatchPublic.Services;
using Microsoft.AspNetCore.Mvc;

namespace DispatchPublic.API;

/// <summary>
/// Public-facing controller for invoice access without authentication
/// </summary>
[ApiController]
[Route("public/invoices/{invoiceId}/{token}")]
public class PublicInvoiceController : ControllerBase
{
    private readonly DataServiceClient _dataServiceClient;
    private readonly InvoiceServiceClient _invoiceServiceClient;
    private readonly ContextService _contextService;
    private readonly ILogger<PublicInvoiceController> _logger;

    public PublicInvoiceController(
        DataServiceClient dataServiceClient,
        InvoiceServiceClient invoiceServiceClient,
        ContextService contextService,
        ILogger<PublicInvoiceController> logger)
    {
        _dataServiceClient = dataServiceClient;
        _invoiceServiceClient = invoiceServiceClient;
        _contextService = contextService;
        _logger = logger;
    }

    /// <summary>
    /// Gets invoice information and PDF URL for public viewing
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetInvoice(Guid invoiceId, string token)
    {
        try
        {
            // Validate the token
            ValidateInvoicePortalTokenResponseDTO validationResult = await _dataServiceClient.ValidateTokenAsync(token);

            if (validationResult == null || !validationResult.IsValid)
            {
                _logger.LogWarning("Invalid token access attempt for invoice {InvoiceId}", invoiceId);
                return Unauthorized(new PublicActionResponseDto
                {
                    Success = false,
                    Error = validationResult?.Error ?? "Invalid token"
                });
            }

            if (validationResult.InvoiceId != invoiceId)
            {
                _logger.LogWarning("Token invoice mismatch: requested {RequestedId}, token for {TokenInvoiceId}", invoiceId, validationResult.InvoiceId);
                return BadRequest(new PublicActionResponseDto
                {
                    Success = false,
                    Error = "Token does not match invoice"
                });
            }

            if (string.IsNullOrEmpty(validationResult.PdfStorageKey))
            {
                _logger.LogError("No PDF storage key found for valid token");
                return StatusCode(500, new PublicActionResponseDto
                {
                    Success = false,
                    Error = "PDF not available"
                });
            }

            // Return basic invoice info and PDF URL
            // Note: In a real implementation, you might want to fetch more invoice details
            var response = new PublicInvoiceResponseDto
            {
                InvoiceId = invoiceId,
                InvoiceNumber = validationResult.InvoiceNumber,
                Date = validationResult.InvoiceDate?.ToLongDateString() ?? "", // TODO: check why it nullable
                DueDate = validationResult.InvoiceDueDate?.ToLongDateString() ?? "On receipt",
                SignaturePath = validationResult.SignaturePath ?? string.Empty,
                Viewed = validationResult.Viewed
            };

            _logger.LogInformation("Successful public access for invoice {InvoiceId}", invoiceId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing public invoice request for {InvoiceId}", invoiceId);
            return StatusCode(500, new PublicActionResponseDto
            {
                Success = false,
                Error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Streams the PDF file directly
    /// </summary>
    [HttpGet("file")]
    public async Task<IActionResult> GetInvoiceFile(Guid invoiceId, string token)
    {
        try
        {
            // Validate the token
            ValidateInvoicePortalTokenResponseDTO validationResult = await _dataServiceClient.ValidateTokenAsync(token);

            if (validationResult == null || !validationResult.IsValid)
            {
                _logger.LogWarning("Invalid token access attempt for invoice file {InvoiceId}", invoiceId);
                return Unauthorized("Invalid or expired token");
            }

            if (validationResult.InvoiceId != invoiceId)
            {
                _logger.LogWarning("Token invoice mismatch for file: requested {RequestedId}, token for {TokenInvoiceId}", invoiceId, validationResult.InvoiceId);
                return BadRequest("Token does not match invoice");
            }

            // Ensure PDF is current (may regenerate if invalidated)
            // Use token for validation - org ID is extracted on the data service side
            string pdfStorageKey = validationResult.PdfStorageKey;
            try
            {
                var result = await _dataServiceClient.EnsurePdfCurrentAsync(token);
                if (result != null && !string.IsNullOrEmpty(result.StorageKey))
                {
                    pdfStorageKey = result.StorageKey;
                    if (result.WasRegenerated)
                    {
                        _logger.LogInformation("PDF was regenerated for invoice {InvoiceId}", invoiceId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to ensure PDF current, using cached key for invoice {InvoiceId}", invoiceId);
            }

            if (string.IsNullOrEmpty(pdfStorageKey))
            {
                _logger.LogError("No PDF storage key found for valid token");
                return StatusCode(500, "PDF not available");
            }

            // Stream the PDF directly from invoice service
            var pdfStream = await _invoiceServiceClient.StreamPdfAsync(pdfStorageKey);

            if (pdfStream == null)
            {
                _logger.LogError("Failed to stream PDF for storage key {StorageKey}", pdfStorageKey);
                return StatusCode(500, "PDF temporarily unavailable");
            }

            _logger.LogInformation("Streaming PDF file for invoice {InvoiceId}", invoiceId);

            return File(pdfStream, "application/pdf", enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming PDF file for {InvoiceId}", invoiceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Marks the invoice as viewed
    /// </summary>
    [HttpPost("viewed")]
    public async Task<IActionResult> MarkAsViewed(Guid invoiceId, string token)
    {
        try
        {
            // Validate the token first to check invoice ID match
            ValidateInvoicePortalTokenResponseDTO validationResult = await _dataServiceClient.ValidateTokenAsync(token);

            if (validationResult == null || !validationResult.IsValid)
            {
                _logger.LogWarning("Invalid token for viewed marking {InvoiceId}", invoiceId);
                return Unauthorized(new PublicActionResponseDto
                {
                    Success = false,
                    Error = "Invalid token"
                });
            }

            if (validationResult.InvoiceId != invoiceId)
            {
                return BadRequest(new PublicActionResponseDto
                {
                    Success = false,
                    Error = "Token does not match invoice"
                });
            }

            // Use token for the internal call - org ID is extracted on the data service side
            var success = await _dataServiceClient.MarkAsViewedAsync(token);

            if (!success)
            {
                return StatusCode(500, new PublicActionResponseDto
                {
                    Success = false,
                    Error = "Failed to mark as viewed"
                });
            }

            _logger.LogInformation("Marked invoice {InvoiceId} as viewed", invoiceId);

            return Ok(new PublicActionResponseDto
            {
                Success = true,
                Message = "Invoice marked as viewed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking invoice as viewed {InvoiceId}", invoiceId);
            return StatusCode(500, new PublicActionResponseDto
            {
                Success = false,
                Error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Submits a signature for the invoice
    /// </summary>
    [HttpPost("signature")]
    public async Task<IActionResult> SubmitSignature(Guid invoiceId, string token, [FromBody] SubmitSignatureRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new PublicActionResponseDto
            {
                Success = false,
                Error = "Invalid request data"
            });
        }

        try
        {
            // Validate the token first to check invoice ID match
            ValidateInvoicePortalTokenResponseDTO validationResult = await _dataServiceClient.ValidateTokenAsync(token);

            if (validationResult == null || !validationResult.IsValid)
            {
                _logger.LogWarning("Invalid token for signature submission {InvoiceId}", invoiceId);
                return Unauthorized(new PublicActionResponseDto
                {
                    Success = false,
                    Error = "Invalid token"
                });
            }

            if (validationResult.InvoiceId != invoiceId)
            {
                return BadRequest(new PublicActionResponseDto
                {
                    Success = false,
                    Error = "Token does not match invoice"
                });
            }

            // Use token for the internal call - org ID is extracted on the data service side
            var success = await _dataServiceClient.SubmitSignatureAsync(token, request.SignaturePath);

            if (!success)
            {
                return StatusCode(500, new PublicActionResponseDto
                {
                    Success = false,
                    Error = "Failed to save signature"
                });
            }

            _logger.LogInformation("Signature submitted for invoice {InvoiceId}", invoiceId);

            return Ok(new PublicActionResponseDto
            {
                Success = true,
                Message = "Signature submitted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting signature for invoice {InvoiceId}", invoiceId);
            return StatusCode(500, new PublicActionResponseDto
            {
                Success = false,
                Error = "Internal server error"
            });
        }
    }
}
