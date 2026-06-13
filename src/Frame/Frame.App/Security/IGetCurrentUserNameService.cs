using System.Security.Principal;

namespace Frame.App.Security
{
    /// <summary>
    /// Этот интерфейс - просто удобная обертка для получение из контекста имени текущего пользователя.
    /// Реализация может отличаться в зависимости от используемого фреймворка. 
    /// Для Blazor в реализации используется AuthenticationStateProvider.
    /// Для чистого asp.net core может использоваться HttpContext
    /// </summary>
    public interface IGetCurrentUserNameService
    {
        /// <summary>
        /// Предпочительный метод получения текущего пользователя. "Под капотом" использует AuthenticationStateProvider.
        /// </summary>
        /// <returns></returns>
        public Task<string> GetLoginAsync();
        public Task<IIdentity?> GetIdentityAsync();
        
        /// <summary>
        /// Синхронный метод использует IHttpContextAccessor. Будет работать только в среде Blazor Server
        /// </summary>
        /// <returns></returns>
        public string GetLogin();
    }
}
