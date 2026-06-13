using Frame.App.Security;
using Frame.Domain.Entities.Core.Security;
using Frame.Infrastructure.DBContext;
using Frame.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frame.Infrastructure.Security
{
    public class DatabaseAuthService : IAuthService
    {
        private readonly NoSecurityDbContext _context;
        private readonly ILogger<DatabaseAuthService> _logger;

        public DatabaseAuthService(NoSecurityDbContext context,
                                    ILogger<DatabaseAuthService> logger)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(logger);

            _context = context;
            _logger = logger;
        }

        public string AuthType => DatabaseAuthSettings.AuthType;
        
        public async Task<Result<Dictionary<string, string>>> AuthenticateAsync(string username, 
                                                                                string password, 
                                                                                Dictionary<string, string>? parameters = null)
        {
            if (username == "")
            {
                string err = "Не передано имя пользователя для аутентификации";
                _logger.LogSecurity(err);
                return Result<Dictionary<string, string>>.Error(err);
            }
            
            if (username == User.SystemUserName)
            {
                string err = "Непосредственная аутентификации системного пользователя запрещена";
                _logger.LogSecurity(err);
                return Result<Dictionary<string, string>>.Error(err);
            }

            string notAuthenticatedError = $"Ошибка аутентификации пользователя {username}";
            try
            {
                var user = await _context.User.FirstOrDefaultAsync(u => u.Login == username); // User равен Null  
                if (user == null)
                {
                    string err = $"{notAuthenticatedError}: пользователь не найден.";
                    _logger.LogSecurity(err);
                    return Result<Dictionary<string, string>>.Error(err);
                }
                
                if (user.Disabled)
                {
                    string err = $"{notAuthenticatedError}: пользователь заблокирован.";
                    _logger.LogSecurity(err);
                    return Result<Dictionary<string, string>>.Error(err);
                }

                Result<bool> r = VerifyPassword(password, user);
                if (r.IsError)
                {
                    string err = $"{notAuthenticatedError} : {r.ErrorResult}";
                    _logger.LogSecurity(err);
                    return Result<Dictionary<string, string>>.Error(err);
                }
                else if(r.Value)
                {
                    return Result<Dictionary<string, string>>.Success([]);
                }
                else
                {
                    _logger.LogSecurity(notAuthenticatedError);
                    return Result<Dictionary<string, string>>.Error(notAuthenticatedError);
                }
            }
            catch (Exception ex)
            {
                string err = $"{notAuthenticatedError}: {DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex)}";
                _logger.LogSecurity(err);
                _logger.LogError(ex, err);
                return Result<Dictionary<string, string>>.Error(err, ex);
            }
        }

        private Result<bool> VerifyPassword(string password, User user)
        {
            try
            {
                return Result<bool>.Success(user.VerifyPassword(password));
            }
            catch (Exception ex)
            {
                string strError = ex.ToString();
                _logger.LogSecurity(strError);
                _logger.LogError(ex, "{Error}", strError);
                return Result<bool>.Error(strError, ex);
            }
        }

        public Result<string> HashPassword(string password)
        {
            try
            {
                return Result<string>.Success(User.HashPassword(password));
            }
            catch (Exception ex)
            {
                string strError = ex.ToString();
                _logger.LogError(ex, "{Error}", strError);
                return Result<string>.Error(strError, ex);
            }
        }

        public Task<Result> LogoutAsync()
        {
            // При работе с БД Logout не требуется
            return Task.FromResult(Result.Success);
        }

    }
}
