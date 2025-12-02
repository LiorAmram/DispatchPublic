using System;
using System.Text.Json;
using System.Threading.Tasks;
using DispatchPublic.DTO;

namespace DispatchPublic.Services;

/// <summary>
/// Client for communicating with DispatchData service
/// </summary>
public class DataServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DataServiceClient> _logger;

    public DataServiceClient(HttpClient httpClient, ILogger<DataServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Validates a public token with the data service
    /// </summary>
    public async Task<ValidateInvoicePortalTokenResponseDTO?> ValidateTokenAsync(string token)
    {
        try
        {
            var requestUri = $"/internal/invoice-portal/validate?token={Uri.EscapeDataString(token)}";
            var response = await _httpClient.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ValidateInvoicePortalTokenResponseDTO>();
                return result;
            }
            else
            {
                _logger.LogWarning("Token validation failed with status {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token with data service");
            return null;
        }
    }

    /// <summary>
    /// Marks a token as viewed
    /// </summary>
    public async Task<bool> MarkTokenAsViewedAsync(Guid tokenId)
    {
        try
        {
            var requestUri = $"/internal/invoice-portal/{tokenId}/viewed";
            var response = await _httpClient.PostAsync(requestUri, null);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                _logger.LogWarning("Mark token as viewed failed with status {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking token as viewed");
            return false;
        }
    }

    /// <summary>
    /// Submits a signature for a token
    /// </summary>
    public async Task<bool> SubmitSignatureAsync(Guid invoiceId, string signaturePath)
    {
        try
        {
            var requestUri = $"/internal/invoice-portal/{invoiceId}/signature";
            var requestDto = new SubmitSignatureDTO { SignaturePath = signaturePath };
            var response = await _httpClient.PostAsJsonAsync(requestUri, requestDto);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                _logger.LogWarning("Submit signature failed with status {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting signature");
            return false;
        }
    }
}
