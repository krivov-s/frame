using Frame.App.Security;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Frame.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace Frame.WebLib.Controllers
{
    //[Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    //[ApiController]
    public class LogoutController(
        ILogger<LogoutController>? logger,
        IGetCurrentUserNameService? currentUserService,
        IAuthService? authService,
        AuthenticationStateProvider? authStateProvider,
        IHttpContextAccessor? httpContextAccessor)
        : Controller
    {
        [Inject] protected ILogger<LogoutController> _logger { get; set; } = logger ?? throw new ArgumentNullException(nameof(logger));
        [Inject] IGetCurrentUserNameService _currentUserService { get; set; } = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        [Inject] protected IAuthService _authService { get; set; } = authService ?? throw new ArgumentNullException(nameof(authService));
        [Inject] protected AuthenticationStateProvider _authStateProvider { get; set; } = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
        [Inject] protected IHttpContextAccessor _httpContextAccessor { get; set; } = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        [HttpGet("/api/logout")]
        public async Task<IActionResult> Logout()
        {
            string strUser = await _currentUserService.GetLoginAsync();
            if(strUser.Length == 0)
            {
                _logger.LogSecurity($"Выдана команда Logout, при этом текущего пользователя нет");
                return Redirect("/");
            }
            // if (_authService != null && _authStateProvider != null)
            // {
            await _authService.LogoutAsync();
            
            if(_httpContextAccessor.HttpContext != null)
            {
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                //_authStateProvider.NotifyAuthenticationStateChanged();
            }                
            _logger.LogSecurity($"Выполнена команда Logout для пользователя с именем {strUser}");
            await _authStateProvider.GetAuthenticationStateAsync();
            return Redirect("/");
            // }
            // else
            // {
            //     _logger?.LogSecurity($"\"Невозможно сделать LogOut для пользователя с именем {strUser}: нужные сервисы не инициализированы.\"", LogLevel.Warning);
            //     return BadRequest("Невозможно сделать LogOut: нужные сервисы не инициализированы.");
            // }
        }
    }
}
