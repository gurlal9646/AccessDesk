using System.Security.Claims;
using AccessDesk.Api.Security;

namespace AccessDesk.Tests;

public class KeycloakRolesClaimsTransformationTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "Test", "preferred_username", ClaimTypes.Role));

    [Fact]
    public async Task MapsRealmAccessRolesToRoleClaims()
    {
        var principal = PrincipalWith(
            new Claim("sub", "u1"),
            new Claim("realm_access", "{\"roles\":[\"manager\",\"employee\"]}"));

        var result = await new KeycloakRolesClaimsTransformation().TransformAsync(principal);

        Assert.True(result.IsInRole("manager"));
        Assert.True(result.IsInRole("employee"));
        Assert.False(result.IsInRole("admin"));
    }

    [Fact]
    public async Task IgnoresMalformedRealmAccess()
    {
        var principal = PrincipalWith(new Claim("realm_access", "not json"));

        var result = await new KeycloakRolesClaimsTransformation().TransformAsync(principal);

        Assert.False(result.IsInRole("manager"));
    }

    [Fact]
    public async Task DoesNotDuplicateExistingRoleClaims()
    {
        var principal = PrincipalWith(
            new Claim(ClaimTypes.Role, "manager"),
            new Claim("realm_access", "{\"roles\":[\"manager\"]}"));

        var result = await new KeycloakRolesClaimsTransformation().TransformAsync(principal);

        Assert.Single(result.FindAll(ClaimTypes.Role));
    }

    [Fact]
    public async Task LeavesAnonymousPrincipalUntouched()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await new KeycloakRolesClaimsTransformation().TransformAsync(anonymous);

        Assert.Empty(result.Claims);
    }
}
