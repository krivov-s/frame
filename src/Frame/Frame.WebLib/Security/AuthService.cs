// using Microsoft.AspNetCore.Authentication;
// using Microsoft.AspNetCore.Authentication.Cookies;
// using Microsoft.EntityFrameworkCore;
// using Frame.Infrastructure.DBContext;
// using Frame.App.Security;
// using Frame.Shared;
// using Microsoft.AspNetCore.Http;
// using Microsoft.Extensions.Logging;
//
// namespace Frame.WebLib.Security
// {
//     public class AuthService : IAuthService
//     {
//         private readonly NoSecurityDbContext _context;
//         private readonly IHttpContextAccessor _httpContextAccessor;
//         private readonly AuthStateProvider _authStateProvider;
//         private readonly ILogger<AuthService> _logger;
//
//         public AuthService( IHttpContextAccessor httpContextAccessor, 
//                             AuthStateProvider authStateProvider,
//                             NoSecurityDbContext context,
//                             ILogger<AuthService> logger)
//         {
//             ArgumentNullException.ThrowIfNull(context);
//             ArgumentNullException.ThrowIfNull(authStateProvider);
//             ArgumentNullException.ThrowIfNull(httpContextAccessor);
//             ArgumentNullException.ThrowIfNull(logger);
//
//             _context = context;
//             _httpContextAccessor = httpContextAccessor;
//             _authStateProvider = authStateProvider;
//             _logger = logger;
//         }
//
//         public async Task<bool> AuthenticateAsync(string login, string password)
//         {
//             try
//             {
//                 var user = await _context.User.FirstOrDefaultAsync(u => u.Login == login); // User равен Null  
//                 if (user == null) return false;
//
//                 return VerifyPassword(password, user.PasswordHash);
//             }
//             catch (Exception ex)
//             {
//                 string strErr = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
//                 _logger?.LogError(ex, strErr);
//                 return false;
//             }
//         }
//
//         private bool VerifyPassword(string password, string hash)
//         {
//             try
//             {
//                 if(password.Length >= 0 && hash.Length == 0)
//                 {
//                     _logger.LogSecurity("Попытка проверки пароля с пользователем, у которого в БД отсутствует хэш пароли!");
//                     return false;
//                 }
//                 return BCrypt.Net.BCrypt.Verify(password, hash);
//             }
//             catch (Exception ex)
//             {
//                 string strError = ex.ToString();
//                 _logger.LogSecurity(strError);
//                 _logger.LogError("{strError}", strError);
//                 return false;
//             }
//         }
//
//         public string HashPassword(string password)
//         {
//             try
//             {
//                 return BCrypt.Net.BCrypt.HashPassword(password);
//             }
//             catch (Exception ex)
//             {
//                 string strError = ex.ToString();
//                 _logger.LogError("{strError}", strError);
//                 return "";
//             }
//         }
//
//         public async Task LogoutAsync()
//         {
//             if(_httpContextAccessor.HttpContext != null)
//             {
//                 await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
//                 _authStateProvider.NotifyAuthenticationStateChanged();
//             }
//         }
//
//     }
// }
