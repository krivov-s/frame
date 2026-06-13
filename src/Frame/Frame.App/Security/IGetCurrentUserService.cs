using Frame.Domain.Entities.Core.Security;
using Frame.Shared;

namespace Frame.App.Security
{
    /// <summary>
    /// Интерфейс доступа к параметрам текущего пользователя <see cref="User"/>.
    /// </summary>
    public interface IGetCurrentUserService
    {
        public Task<Result<User>> GetCurrentUserAsync();
    }
}
