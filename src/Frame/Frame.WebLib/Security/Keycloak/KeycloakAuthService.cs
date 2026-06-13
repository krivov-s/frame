using System.Text.Json;
using Frame.App.Security;
using Frame.Shared;
using Microsoft.Extensions.Logging;

namespace Frame.WebLib.Security.Keycloak;

public class KeycloakAuthService(
    HttpClient httpClient,
    KeycloakSettings keycloakSettings,
    ILogger<KeycloakAuthService> logger) : IAuthService
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly KeycloakSettings _settings = keycloakSettings ?? throw new ArgumentNullException(nameof(keycloakSettings));
    private readonly ILogger<KeycloakAuthService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // private string _accessToken = "";
    // private List<Claim> _claims = [];
    // private ClaimsIdentity _claimsIdentity = new();
    private Dictionary<string, string> _parameters = [];

    public string AuthType => KeycloakSettings.AuthType;
    
    public async Task<Result<Dictionary<string, string>>> AuthenticateAsync(string username, 
                                                                            string password, 
                                                                            Dictionary<string, string>? parameters = null)
    {
        string err;
        if (parameters == null)
        {
            err = $"Keycloak: необходимо наличие дополнительных параметров";
            _logger.LogError(err);
            return Result<Dictionary<string, string>>.Error(err);
        }

        if (!parameters.TryGetValue(KeycloakConstants.ClientIdPropName, out string? clientId))
        {
            err = $"Keycloak: необходимо наличие параметра {KeycloakConstants.ClientIdPropName}";
            _logger.LogError(err);
            return Result<Dictionary<string, string>>.Error(err);
        }

        if (clientId == "")
        {
            err = $"Keycloak: параметр {KeycloakConstants.ClientIdPropName} не может быть пустым.";
            _logger.LogError(err);
            return Result<Dictionary<string, string>>.Error(err);
        }

        if (!_settings.KeycloakClients.TryGetValue(clientId, out string? clientSecret))
        {
            err = $"Keycloak: в списке зарегистрированных клиентов отсутствует {clientId}.";
            _logger.LogError(err);
            return Result<Dictionary<string, string>>.Error(err);
        }

        if (clientSecret == "")
        {
            err = $"Keycloak: для клиента {clientId} не задан ClientSecret.";
            _logger.LogError(err);
            return Result<Dictionary<string, string>>.Error(err);
        }

        var tokenEndpoint = $"{_settings.Authority}/realms/{_settings.Realm}{_settings.OAuth2TokenEndpoint}";

        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("grant_type", KeycloakConstants.GrantTypePasswordPropName),
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("client_secret", clientSecret)
        ]);

        HttpResponseMessage response = await _httpClient.PostAsync(tokenEndpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            err = $"Ошибка при отправке запроса в Keycloak. Код: {response.StatusCode} Описание: {response.ReasonPhrase}";
            _logger.LogError(err);
            return Result<Dictionary<string, string>>.Error(err);
        }
        
        string responseBody = await response.Content.ReadAsStringAsync();
        JsonDocument json = JsonDocument.Parse(responseBody);

        Dictionary<string, string> retParams = new()
        {
            { KeycloakConstants.ClientIdPropName, clientId },
            { KeycloakConstants.ClientSecretPropName, clientSecret },
            { KeycloakConstants.AccessTokenPropName, json.RootElement.GetProperty(KeycloakConstants.AccessTokenPropName).GetString() ?? "" },
            { KeycloakConstants.ExpiresInPropName, json.RootElement.GetProperty(KeycloakConstants.ExpiresInPropName).ToString()},
            { KeycloakConstants.RefreshTokenPropName, json.RootElement.GetProperty(KeycloakConstants.RefreshTokenPropName).GetString() ?? "" },
            { KeycloakConstants.RefreshExpiresInPropName, json.RootElement.GetProperty(KeycloakConstants.RefreshExpiresInPropName).ToString()},
        };
        
        // Сохраняем полученные параметры для Logout-а или обновления токена
        _parameters = retParams;
        
        return Result<Dictionary<string, string>>.Success(retParams);
        
        // // _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 30);
        //
        // // string responseString = await response.Content.ReadAsStringAsync();
        // // TokenResponse? tokenData = JsonSerializer.Deserialize<TokenResponse>(responseString);
        // //
        // // if (tokenData == null || string.IsNullOrEmpty(tokenData.access_token)) return false;
        //
        // // _accessToken = tokenData.access_token;
        // List<Claim> claims = ExtractClaimsFromJwt(accessToken);
        // foreach (Claim claim in claims)
        // {
        //     Console.WriteLine($"{claim.Type}: {claim.Value}");    
        // }
        //
        // _claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        // var authProperties = new AuthenticationProperties
        // {
        //     IsPersistent = true,
        //     ExpiresUtc = DateTime.UtcNow.AddSeconds(expiresIn)
        // };
        //
        // // await _httpContextAccessor.HttpContext.SignInAsync(
        // //     CookieAuthenticationDefaults.AuthenticationScheme,
        // //     new ClaimsPrincipal(_claimsIdentity),
        // //     authProperties
        // // );
        //
        // return new ClaimsPrincipal(_claimsIdentity);
    }

    public async Task<Result> LogoutAsync()
    {
        /*
         по информации от DeepSeek
        POST http://sbox:8282/realms/ccps/protocol/openid-connect/logout
        Content-Type: application/x-www-form-urlencoded
            client_id=ccps-frontend
            client_secret=MY_CLIENT_SECRET
            refresh_token=REFRESH_TOKEN
        */
        
        // TODO: убедиться, что экземпляр сервиса сохраняется между Login и Logout, иначе _parameters будет пустым, и logout не произойдет
        
        var tokenEndpoint = $"{_settings.Authority}/realms/{_settings.Realm}/protocol/openid-connect/logout";

        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>(KeycloakConstants.ClientIdPropName, _parameters[KeycloakConstants.ClientIdPropName]),
            new KeyValuePair<string, string>(KeycloakConstants.ClientSecretPropName, _parameters[KeycloakConstants.ClientSecretPropName]),
            new KeyValuePair<string, string>(KeycloakConstants.RefreshTokenPropName, _parameters[KeycloakConstants.RefreshTokenPropName]),
        ]);

        HttpResponseMessage response = await _httpClient.PostAsync(tokenEndpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            string err = $"Ошибка при отправке logout - запроса в Keycloak. Код: {response.StatusCode} Описание: {response.ReasonPhrase}";
            _logger.LogError(err);
            return Result.Error(err);
        }

        return Result.Success;
    }

    public Result<string> HashPassword(string password)
    {
        throw new NotImplementedException();
    }
}

// public class TokenResponse
// {
//     public string access_token { get; set; } = "";
//     public int expires_in { get; set; }
//     public int refresh_expires_in { get; set; }
//     public string refresh_token { get; set; } = "";
//     public string token_type { get; set; } = "";
//     // public int not-before-policy { get; set; }
//     public string session_state { get; set; } = "";
//     public string scope { get; set; } = "";
// }

// using System.IdentityModel.Tokens.Jwt;
// using System.Linq;
// using System.Threading.Tasks;
//
// namespace MockCrm;
//
// public class KeycloakAuthService
// {
//     private readonly HttpClient _httpClient;
//     private readonly IConfiguration _config;
//
//     private string _accessToken;
//     private DateTime _tokenExpiry;
//
//     public KeycloakAuthService(HttpClient httpClient, IConfiguration config)
//     {
//         _httpClient = httpClient;
//         _config = config;
//     }
//
//     public async Task<string> GetAccessTokenAsync()
//     {
//         if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiry > DateTime.UtcNow)
//         {
//             return _accessToken;
//         }
//
//         var clientId = _config["Keycloak:ClientId"];
//         var clientSecret = _config["Keycloak:ClientSecret"];
//         var tokenUrl = _config["Keycloak:TokenUrl"];
//
//         var content = new FormUrlEncodedContent(new[]
//         {
//             new KeyValuePair<string, string>("grant_type", "client_credentials"),
//             new KeyValuePair<string, string>("client_id", clientId),
//             new KeyValuePair<string, string>("client_secret", clientSecret),
//         });
//
//         var response = await _httpClient.PostAsync(tokenUrl, content);
//         if (!response.IsSuccessStatusCode)
//         {
//             throw new Exception($"Ошибка получения токена: {response.StatusCode}");
//         }
//
//         var responseBody = await response.Content.ReadAsStringAsync();
//         var json = System.Text.Json.JsonDocument.Parse(responseBody);
//         _accessToken = json.RootElement.GetProperty("access_token").GetString();
//         var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
//         _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 30);
//
//         return _accessToken;
//     }
//
//     public async Task<string> GetCurrentUserFromTokenAsync()
//     {
//         var token = await GetAccessTokenAsync();
//         var handler = new JwtSecurityTokenHandler();
//         var jwtToken = handler.ReadJwtToken(token);
//         return jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ?? "UnknownSystemUser";
//     }
// }
