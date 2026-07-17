using System.Security.Claims;

namespace Sessions.Application.Common;

public static class CurrentUserClaims
{
    public static Guid? GetUserId(ClaimsPrincipal? user)
    {
        var claim = user?.FindFirst("sub") ?? user?.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }
}
