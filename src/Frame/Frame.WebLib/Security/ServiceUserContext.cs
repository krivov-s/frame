using System.Security.Claims;
using Frame.App.Cores;
using Frame.App.Security;
using Frame.Domain.Entities.Core.Security;
using Frame.Shared;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.WebLib.Security;

/// <summary>
/// Настройка текущего scope для работы с заданным системным (сервисным) пользователем.
/// В идеале здесь нужно получать IAuthService, логиниться, получать токен, и потом только выполнять действия.
/// Причем в случае Keycloak нужно получать токен клиента и сервисного пользователя.
/// А вот какого клиента - нужно задавать в параметрах метода <see cref="UseServiceUserNameAsync"/>.
/// </summary>
/// <param name="serviceProvider"></param>
public class ServiceUserContext(IServiceProvider serviceProvider) : IServiceUserContext, IDisposable
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    
    /// <summary>
    /// Указание применить для текущего scope заданное имя пользователя. 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public async Task<bool> UseServiceUserNameAsync()
    {
        // IAuthService authService = _serviceProvider.GetRequiredService<IAuthService>();
        // return authService.AuthenticateAsync(User.SystemUserName, SystemUserPassword);
        
        IGetCurrentUserNameService userNameService = _serviceProvider.GetRequiredService<IGetCurrentUserNameService>();
        
        // Проверка повторного вызова. Если пользователь уже установлен - выходим.
        string userName = await userNameService.GetLoginAsync();
        if (userName != "")
        {
            return true;
        }
        
        ClaimsPrincipal user = CreatePrincipal(User.SystemUserName);
        
        // 1. Создаем AuthStateProvider, в который вручную запихиваем пользователя system 
        AuthStateProvider authStateProvider = _serviceProvider.GetRequiredService<AuthStateProvider>();
        await authStateProvider.SetUserAsync(user);
        
        IUserSecurityDataManager userSecurityDataManager = _serviceProvider.GetRequiredService<IUserSecurityDataManager>();
        Result<UserSession> resSession = await userSecurityDataManager.LoadUserSessionAsync(User.SystemUserName);
        if (resSession.IsError || resSession.Value == null)
        {
            string err = $"Ошибка чтения сессии пользователя {User.SystemUserName}! ";
            throw new UnauthorizedAccessException(err);
        }
        
        // 2. Вызываем асинхронно GetCurrentUserNameService, чтобы хотя бы раз передать ему пользователя
        userNameService = _serviceProvider.GetRequiredService<IGetCurrentUserNameService>();
        userName = await userNameService.GetLoginAsync();
        if (userName != User.SystemUserName)
        {
            string err = $"Ошибка установки пользователя {User.SystemUserName} в контекст выполнения! ";
            throw new UnauthorizedAccessException(err);
        }

        // 3. Инициализируем UserCore
        IUserCore userCore = _serviceProvider.GetRequiredService<IUserCore>();
        await userCore.InitializeAsync();
        
        return true;
    }
    
    private ClaimsPrincipal CreatePrincipal(string userName)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userName),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme); //
        return new ClaimsPrincipal(identity);
    }

    public void Dispose()
    {
        // TODO: здесь нужно будет делать Logout от IAuthService
    }
}