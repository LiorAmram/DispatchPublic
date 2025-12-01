using DispatchPublic.Utils;

namespace DispatchPublic.Services;

/// <summary>
/// Service to provide request context for logging and tracking in the public service
/// </summary>
public class ContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the client IP address for rate limiting and logging
    /// </summary>
    public string GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return "unknown";
        }

        // Try to get the real IP from various headers
        var ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? httpContext.Request.Headers["X-Real-IP"].FirstOrDefault()
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return ipAddress;
    }

    /// <summary>
    /// Gets the user agent for logging
    /// </summary>
    public string GetUserAgent()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return "unknown";
        }

        return httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
    }
}
