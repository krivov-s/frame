using Frame.App.Security;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Frame.Shared;
using System.Web;
using Frame.WebLib.Security.Keycloak;

namespace Frame.WebLib.Controllers
{
    //[Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    //[ApiController]
    public class LoginController : Controller
    {
        [Inject] protected ILogger<LoginController>? _logger { get; set; }
        [Inject] protected IAuthService? _authService { get; set; }
        [Inject] protected AuthenticationStateProvider? _authStateProvider { get; set; }
        [Inject] protected KeycloakSettings? _keycloakSettings { get; set; }
        [Inject] protected IHttpContextAccessor? _httpContextAccessor { get; set; }
        [Inject] protected IGetCurrentUserService? _getCurrentUserService { get; set; }

        public LoginController( ILogger<LoginController>? logger,
                                IAuthService? authService,
                                AuthenticationStateProvider? authStateProvider,
                                KeycloakSettings? keycloakSettings,
                                IHttpContextAccessor? httpContextAccessor, 
                                IGetCurrentUserService? getCurrentUserService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _getCurrentUserService = getCurrentUserService ?? throw new ArgumentNullException(nameof(getCurrentUserService));
            _keycloakSettings = keycloakSettings;
        }

        [HttpPost("/api/login")]
        public async Task<IActionResult> Login(string username, string userpass, string client_id = "")
        {
            // Обрабатываем данные, полученные из формы
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userpass))
            {
                string strMessage = "Имя и пароль пользователя не могут быть пустыми!";
                string strUri = $"/{nameof(Components.Login.Login)}?ErrorMessage={HttpUtility.UrlEncode(strMessage)}";
                return Redirect(strUri);
            }

            if (_authService == null || _httpContextAccessor == null || _authStateProvider == null)
            {
                string strMessage = "Какая-то хрень: недоступен сервис логина!";
                _logger?.LogSecurity(strMessage, LogLevel.Error);
                string strUri = $"/{nameof(Components.Login.Login)}?ErrorMessage={HttpUtility.UrlEncode(strMessage)}";
                return Redirect(strUri);
            }

            Dictionary<string, string> parameters = [];
            if (_authService.AuthType == KeycloakSettings.AuthType)
            {
                if (_keycloakSettings == null)
                {
                    string strMessage = "Не переданы настройки Keycloak!";
                    string strUri = $"/{nameof(Components.Login.Login)}?ErrorMessage={HttpUtility.UrlEncode(strMessage)}";
                    return Redirect(strUri);
                }
                
                // Имеем аутентификацию Keycloak, нужны доп. параметры
                string clientId = (client_id != "" ? client_id : _keycloakSettings.DefaultClientId);
                parameters.Add(KeycloakConstants.ClientIdPropName, clientId);
            }

            Result<Dictionary<string, string>> result =
                await _authService.AuthenticateAsync(username, userpass, parameters);
            if (result.IsError)
            {
                // Обработка ошибки аутентификации
                string strMessage = result.ErrorResult;
                string strUri = $"/{nameof(Components.Login.Login)}?ErrorMessage={HttpUtility.UrlEncode(strMessage)}";
                return Redirect(strUri);
            }

            ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, username)], CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal = new(identity);

            if (_httpContextAccessor.HttpContext != null)
            {
                await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                _logger?.LogSecurity($"Успешная аутентификации пользователя с именем {username}");
                
                // Проверяем, а не нужно ли ему сменить пароль?
                AuthenticationState authState = await _authStateProvider.GetAuthenticationStateAsync();
                Claim? claimReqChangePass = authState.User.Claims.FirstOrDefault(c => 
                    c.Type == nameof(Domain.Entities.Core.Security.User.ChangePassOnNextLogin));
                if (claimReqChangePass == null)
                {
                    return Redirect("/");
                }
                else
                {
                    return Redirect("/ChangePassword");
                }
            }
            else
            {
                string strMessage = "Не удалось выполнить SignIn: не определен HttpContext!";
                _logger?.LogSecurity(strMessage, LogLevel.Error);
                string strUri = $"/{nameof(Components.Login.Login)}?ErrorMessage={HttpUtility.UrlEncode(strMessage)}";
                return Redirect(strUri);
            }
        }
    }
}
