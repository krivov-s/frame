using Frame.App.Security;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Infrastructure.Security
{
    /// <summary>
    /// <b>Singleton</b>, хранящий карту соответствия текущего пользователя и его объекта <see cref="UserSession"/> 
    /// Предназначен только для хранения, с возможностью threadsafe доступа (с блокировками lock)
    /// </summary>
    public class UserSecurityData : IUserSecurityData
    {
        private readonly object _lockUsersMap = new();
        private readonly Dictionary<string, UserSession> _usersMap = [];

        public UserSession? GetUserSession(string sLogin)
        {
            lock (_lockUsersMap)
            {
                return _usersMap.GetValueOrDefault(sLogin);
            }
        }

        public void InitUserSession(User user)
        {
            UserSession session = new(user);

            lock (_lockUsersMap)
            {
                _usersMap[user.Login] = session;
            }
        }
        
    }
}
