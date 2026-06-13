using Frame.Domain.Entities.Core.Security;
using Frame.Shared;

namespace Frame.App.Security
{
    /// <summary>
    /// Интерфейс получения данных о безопасности для заданного пользователя.
    /// </summary>
    public interface IUserSecurityDataManager
    {
        /// <summary>
        /// Получение информации о пользовательской сессии (при необходимости - инициализация/загрузка).
        /// Этот метот вызывается из <see cref="IClaimsTransformation.TransformAsync"/> в ходе идентификации пользователя
        /// </summary>
        /// <param name="login">Login пользователя</param>
        /// <param name="forceReload">true - принудительная перезагрузка (переинициализация) сессии</param>
        /// <returns></returns>
        public Task<Result<UserSession>> LoadUserSessionAsync(string login, bool forceReload = false);
        
        /// <summary>
        /// Получение информации о пользовательской сессии. Если сессия не инициализирована - возвращается
        /// Result.Success но с нулевой сессией. Загрузка из БД не осуществляется.
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public Result<UserSession> GetUserSession(string login);

        /// <summary>
        /// Принудительная перезагрузка (переинициализация) сессии пользователя.
        /// Это дополнительная обертка вокруг <see cref="LoadUserSessionAsync"/> с указанием forceReload = true 
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public Task<Result> ResetUserSessionAsync(string login);
        
        // public Result<SecurityProfile> GetUserSecurityProfile<TEntity>(string login) where TEntity : BaseEntity;
        //
        // public Result<SecurityProfile> GetUserSecurityProfile(string login, string entityTypeName);
        
        /// <summary>
        /// Перебор всех пользователей с данной ролью и сброс сессии. <b>Роль должна прийти полностью загруженная!</b> 
        /// В противном случае сброса не произойдет!
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public Task<Result> ResetRoleSessionsAsync(Role role);


    }
}
