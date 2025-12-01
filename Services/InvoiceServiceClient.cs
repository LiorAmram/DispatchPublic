namespace DispatchPublic.Services;

/// <summary>
/// Client for communicating with DispatchInvoice service
/// </summary>
public class InvoiceServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InvoiceServiceClient> _logger;

    public InvoiceServiceClient(HttpClient httpClient, ILogger<InvoiceServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets a streaming URL for a PDF file
    /// </summary>
    public async Task<string?> GetPdfStreamUrlAsync(string storageKey)
    {
        try
        {
            var requestUri = $"/api/invoicefiles/stream-pdf?storage-key={Uri.EscapeDataString(storageKey)}";
            var response = await _httpClient.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                // For streaming, we return the URL that can be used to stream the file
                // In this case, since we're calling internally, we need to construct the external URL
                // This assumes the invoice service is accessible at the same base URL but different port
                var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "";
                return $"{baseUrl}{requestUri}";
            }
            else
            {
                _logger.LogWarning("Get PDF stream URL failed with status {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PDF stream URL");
            return null;
        }
    }

    /// <summary>
    /// Streams a PDF file directly
    /// </summary>
    public async Task<Stream?> StreamPdfAsync(string storageKey)
    {
        try
        {
            var requestUri = $"/api/invoicefiles/stream-pdf?storage-key={Uri.EscapeDataString(storageKey)}";
            var response = await _httpClient.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync();
            }
            else
            {
                _logger.LogWarning("Stream PDF failed with status {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming PDF");
            return null;
        }
    }
}
