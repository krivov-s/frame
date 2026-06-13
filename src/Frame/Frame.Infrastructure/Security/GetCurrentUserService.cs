using Frame.App.IEntityLoaders;
using Frame.App.Security;
using Frame.Domain.Entities.Core.Security;
using Frame.Shared;

namespace Frame.Infrastructure.Security
{
    public class GetCurrentUserService : IGetCurrentUserService
    {
        private readonly IUserLoader _userLoader;
        private readonly IGetCurrentUserNameService _getCurrentUserNameService;

        private User? _currentUser;

        public GetCurrentUserService(IGetCurrentUserNameService getCurrentUserNameService, IUserLoader userLoader)
        {
            _userLoader = userLoader;
            _getCurrentUserNameService = getCurrentUserNameService;
        }

        public async Task<Result<User>> GetCurrentUserAsync()
        {
            if(_currentUser == null)
            {
                string userName = await _getCurrentUserNameService.GetLoginAsync();
                if(userName.Length > 0)
                {
                    Result<User?> resUser = await _userLoader.LoadByNameAsync(userName);
                    if(resUser.IsError || resUser.Value == null)
                    {
                        return Result<User>.Error( (resUser.ErrorResult.Length > 0) ? resUser.ErrorResult : $"Не удалось получить пользователия по имени {userName}");
                    }
                    else
                    {
                        _currentUser = resUser.Value;
                    }
                }
                else
                {
                    return Result<User>.Error("Не удалось получить имя пользователя от сервиса IGetCurrentUserNameService");
                }
            }

            return Result<User>.Success(_currentUser);
        }
    }
}
