// using System.IdentityModel.Tokens.Jwt;
// using System.Text.Json;
// using Frame.App.Security;
// using Frame.Shared;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Logging;
//
// namespace Frame.WebLib.Security.Keycloak
// {
//     public class TokenService : ITokenService
//     {
//         private readonly IConfiguration _configuration;
//         private readonly HttpClient _httpClient;
//         private readonly ILogger<TokenService> _logger;
//
//         public TokenService(IConfiguration configuration, HttpClient httpClient, ILogger<TokenService> logger)
//         {
//             _configuration = configuration;
//             _httpClient = httpClient;
//             _httpClient.Timeout = TimeSpan.FromSeconds(30);
//             _logger = logger;
//         }
//
//         /// <summary>
//         /// Отправляет запрос с логином и паролем пользователя в Keycloak, получает токен доступа
//         /// </summary>
//         /// <param name="username">Логин пользователя</param>
//         /// <param name="password">Пароль пользователя</param>
//         /// <returns>Возвращает объект TokenResponse с информацией о токене, либо null в случае ошибки</returns>
//         public async Task<TokenResponse?> GetTokenAsync(string username, string password)
//         {
//             var content = new Dictionary<string, string>
//             {
//                 { KeycloakConstants.Username, username },
//                 { KeycloakConstants.Password, password }
//             };
//
//             var request = CreateTokenRequest(content, KeycloakConstants.GrantTypePassword);
//             if (request == null)
//             {
//                 _logger.LogError("Error while getting token.");
//                 return null;
//             }
//
//             var response = await _httpClient.SendAsync(request);
//
//             if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
//             {
//                 _logger.LogWarning("Error while sending request.");
//                 return null;
//             }
//
//             if (!response.IsSuccessStatusCode)
//             {
//                 _logger.LogError("Response status error", response.StatusCode);
//                 return null;
//             }
//
//             var responseContent = await response.Content.ReadAsStringAsync();
//             _logger.LogInformation(responseContent);
//
//             var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
//
//             if (tokenResponse == null)
//             {
//                 _logger.LogError("Error while deserialize token.");
//             }
//
//             return tokenResponse;
//         }
//
//         /// <summary>
//         /// Обновляет токен доступа, отправляя запрос с использованием рефреш-токена
//         /// </summary>
//         /// <param name="refreshToken">Рефреш-токен</param>
//         /// <returns>Возвращает объект TokenResponse с обновленным токеном, либо null в случае ошибки</returns>
//         public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken)
//         {
//
//             var content = new Dictionary<string, string>
//             {
//                 { KeycloakConstants.RefreshToken, refreshToken }
//             };
//
//             var request = CreateTokenRequest(content, KeycloakConstants.GrantTypeRefreshToken);
//             if (request == null)
//             {
//                 _logger.LogError("Error while token request creation");
//                 return null;
//             }
//
//             var response = await _httpClient.SendAsync(request);
//
//             if (!response.IsSuccessStatusCode)
//             {
//                 _logger.LogError("Error while sennding request", $"Status code:{response.StatusCode}");
//                 return null;
//             }
//
//             var responseContent = await response.Content.ReadAsStringAsync();
//             var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
//
//             if (tokenResponse == null)
//             {
//                 _logger.LogError("Error while deserialize token");
//             }
//
//             return tokenResponse;
//         }
//
//         /// <summary>
//         /// Создаёт запрос для получения или обновления токена с использованием переданных параметров
//         /// </summary>
//         /// <param name="content">Словарь с параметрами запроса (логин, пароль или рефреш-токен)</param>
//         /// <param name="grantType">Тип grant, например, "password" для получения токена или "refresh_token" для его обновления</param>
//         /// <returns>Возвращает объект HttpRequestMessage для отправки запроса на сервер, либо null в случае ошибки конфигурации</returns>
//         private HttpRequestMessage? CreateTokenRequest(Dictionary<string, string> content, string grantType)
//         {
//             var oauthPath = _configuration[KeycloakSettings.OAuth2Path];
//             var clientId = _configuration[KeycloakSettings.VAR_KeycloakClientId];
//             var clientSecret = _configuration[KeycloakSettings.VAR_KeycloakSecretKey];
//
//             if (string.IsNullOrEmpty(oauthPath) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
//             {
//                 _logger.LogError("Error while getting token");
//                 return null;
//             }
//
//             var request = new HttpRequestMessage(HttpMethod.Post, $"{oauthPath}{KeycloakSettings.OAuth2TokenEndpoint}");
//             content[KeycloakConstants.GrantType] = grantType;
//             content[KeycloakConstants.ClientId] = clientId;
//             content[KeycloakConstants.ClientSecret] = clientSecret;
//             request.Content = new FormUrlEncodedContent(content);
//             return request;
//         }
//
//         /// <summary>
//         /// Извлекает роль пользователя из токена доступа, анализируя секции realm_access и resource_access
//         /// </summary>
//         /// <param name="accessToken">Токен доступа, из которого извлекаются роли</param>
//         /// <param name="roleMappings">Словарь для сопоставления ролей токена с ролями приложения</param>
//         /// <param name="clientId">Идентификатор клиента для поиска ролей в секции resource_access токена</param>
//         /// <returns>Возвращает сопоставленную роль, либо null, если роль не найдена</returns>
//         public List<string> GetRolesFromToken(string accessToken, IReadOnlyDictionary<string, string> roleMappings, string clientId)
//         {
//             var handler = new JwtSecurityTokenHandler();
//             var jwtToken = handler.ReadJwtToken(accessToken);
//
//             var roles = new List<string>();
//
//             var clientRoles = jwtToken.Claims.FirstOrDefault(c => c.Type == KeycloakConstants.ResourceAccess)?.Value;
//             if (clientRoles != null)
//             {
//                 var jsonDoc = JsonDocument.Parse(clientRoles);
//                 if (jsonDoc.RootElement.TryGetProperty(clientId, out var clientElement))
//                 {
//                     var clientRoleList = clientElement.GetProperty(KeycloakConstants.Roles)
//                                                       .EnumerateArray()
//                                                       .Select(role => role.GetString());
//
//                     foreach (var role in clientRoleList)
//                     {
//                         if (!string.IsNullOrEmpty(role) && roleMappings.TryGetValue(role, out var mappedRole))
//                         {
//                             roles.Add(mappedRole);
//                         }
//                     }
//                 }
//             }
//
//             var realmRoles = jwtToken.Claims.FirstOrDefault(c => c.Type == KeycloakConstants.RealmAccess)?.Value;
//             if (realmRoles != null)
//             {
//                 var realmRoleList = JsonDocument.Parse(realmRoles)
//                                                 .RootElement.GetProperty(KeycloakConstants.Roles)
//                                                 .EnumerateArray()
//                                                 .Select(role => role.GetString());
//
//                 foreach (var role in realmRoleList)
//                 {
//                     if (!string.IsNullOrEmpty(role) && roleMappings.TryGetValue(role, out var mappedRole))
//                     {
//                         roles.Add(mappedRole);
//                     }
//                 }
//             }
//
//             if (!roles.Any())
//             {
//                 _logger.LogWarning("Role was not find in token");
//             }
//
//             return roles;
//         }
//     }
// }
