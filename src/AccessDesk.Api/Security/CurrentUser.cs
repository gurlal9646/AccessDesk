using System.Security.Claims;

namespace AccessDesk.Api.Security;

public record CurrentUser(string Id, string Name, bool IsManager, bool IsAdmin)
{
    public static CurrentUser From(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue("sub")
                 ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? throw new InvalidOperationException("Authenticated user has no subject claim.");
        var name = principal.FindFirstValue("preferred_username")
                   ?? principal.FindFirstValue(ClaimTypes.Name)
                   ?? id;
        var isAdmin = principal.IsInRole(Roles.Admin);
        var isManager = isAdmin || principal.IsInRole(Roles.Manager);
        return new CurrentUser(id, name, isManager, isAdmin);
    }
}
