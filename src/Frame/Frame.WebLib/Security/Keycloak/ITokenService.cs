
namespace Frame.WebLib.Security.Keycloak
{
    public interface ITokenService
    {
        Task GetTokenAsync(string username, string password);
        Task<TokenResponse?> RefreshTokenAsync(string refreshToken);
        public List<string> GetRolesFromToken(string accessToken, IReadOnlyDictionary<string, string> roleMappings, string clientId);
    }
}
