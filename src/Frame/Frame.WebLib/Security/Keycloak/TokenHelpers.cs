using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Frame.WebLib.Security.Keycloak;

public static class TokenHelpers
{
    public static List<Claim> ExtractClaimsFromJwt(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        return jwtToken.Claims.ToList();
    }

    public static string GetCurrentUserFromTokenAsync(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ?? "UnknownSystemUser";
    }
}