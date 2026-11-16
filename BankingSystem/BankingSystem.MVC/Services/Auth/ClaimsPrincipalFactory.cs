using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BankingSystem.MVC.Services.Auth;

public static class ClaimsPrincipalFactory
{
    public static ClaimsPrincipal Create(string accessToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        var claims = new List<Claim>();
        foreach (var claim in jwt.Claims)
        {
            var type = JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.TryGetValue(claim.Type, out var mapped)
                ? mapped
                : claim.Type;

            if (type is ClaimTypes.NameIdentifier or ClaimTypes.Name or ClaimTypes.Role)
                claims.Add(new Claim(type, claim.Value));
        }

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme,
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);

        return new ClaimsPrincipal(identity);
    }
}