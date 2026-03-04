using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Solution.Services;

/// <summary>
/// Provider for getting the current user in different application contexts (Web API vs Desktop)
/// </summary>
public interface ICurrentUserProvider
{
    Guid GetCurrentUserId();
}

/// <summary>
/// Web API implementation using HttpContext
/// </summary>
public class WebCurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WebCurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        if (user == null || !user.Identity?.IsAuthenticated == true)
            throw new UnauthorizedAccessException("User is not authenticated.");
        
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? user.FindFirst("sub")?.Value
                       ?? user.FindFirst("userId")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User ID claim not found in token.");
        
        return Guid.Parse(userIdClaim);
    }
}

/// <summary>
/// Desktop/MAUI implementation - uses a static user ID for development
/// </summary>
public class DesktopCurrentUserProvider : ICurrentUserProvider
{
    // In a real desktop app, you would retrieve this from:
    // - Secure storage
    // - Configuration file
    // - User login form
    // For now, we use a development default
    private static readonly Guid DefaultUserId = new("00000000-0000-0000-0000-000000000001");

    public Guid GetCurrentUserId()
    {
        return DefaultUserId;
    }
}
