using Frame.Domain.Entities.Core.Security;

namespace Frame.App.Security
{
    public interface IUserSecurityData
    {
        /// <summary>
        /// Получение сессии пользователя из карты. Чтение из БД не осуществляется, при отсутствии в карте вернется null
        /// </summary>
        /// <param name="sLogin"></param>
        /// <returns></returns>
        public UserSession? GetUserSession(string sLogin);
        
        
        // /// <summary>
        // /// Внесение сессии пользователя в карту.
        // /// </summary>
        // /// <param name="sLogin"></param>
        // /// <param name="session"></param>
        // public void SetUserSession(string sLogin, UserSession session);

        /// <summary>
        /// Инициализация сессии пользователя. Если существует - будет перезаписана.
        /// </summary>
        /// <param name="user"></param>
        public void InitUserSession(User user);

    }
}
