using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace AccessDesk.Api.Security;

/// <summary>
/// Keycloak puts realm roles inside a nested "realm_access" JSON claim.
/// This maps them to standard role claims so [Authorize(Roles = ...)] and policies work.
/// </summary>
public class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
        {
            return Task.FromResult(principal);
        }

        try
        {
            using var doc = JsonDocument.Parse(realmAccess);
            if (doc.RootElement.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var value = role.GetString();
                    if (!string.IsNullOrEmpty(value) && !identity.HasClaim(identity.RoleClaimType, value))
                    {
                        identity.AddClaim(new Claim(identity.RoleClaimType, value));
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Malformed claim: ignore, the user simply gets no extra roles.
        }

        return Task.FromResult(principal);
    }
}
