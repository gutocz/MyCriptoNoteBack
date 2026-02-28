using System.Security.Claims;

namespace MyCriptoNote.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Token inválido.");
        return Guid.Parse(claim.Value);
    }
}
