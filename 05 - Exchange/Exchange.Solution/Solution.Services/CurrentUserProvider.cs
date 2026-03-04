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
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue("uid");
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("User not authenticated or invalid user ID");
        return userId;
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
