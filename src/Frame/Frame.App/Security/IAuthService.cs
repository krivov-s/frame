using Frame.Shared;

namespace Frame.App.Security
{
    /// <summary>
    /// Аутентификация сервиса/пользователя в приложении. Реализации интерфейса могут осуществлять верификацию
    /// логина и пароля пользователя в разных системах, например проверка наличия пользователя с паролем в БД,
    /// или аутентификация и получение токена в Keycloak 
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Тип аутентификации, определяется реализацией. Варианты: Database, Keycloak, м.б. иные дополнительные
        /// </summary>
        public string AuthType { get; }
        
        /// <summary>
        /// Аутентификация клиента/пользователя.
        /// Для Keycloak возможна аутентификация только клиента, без пользователя, а пользователя позже.
        /// Также возможно сначала аутентифицировать клиента, и потом уже при наличии токена - аутентифицировать пользователей.
        /// Все это определяется конкретной реализацией сервиса.
        /// </summary>
        /// <param name="username">Имя пользователя (login)</param>
        /// <param name="password">Пароль</param>
        /// <param name="parameters">Дополнительные параметры для аутентификации.
        /// Например, для Keycloak ожидается ClientId. Набор определяется конкретной реализацией</param>
        /// <returns>Набор параметров, в зависимости от того, где аутентифицируемся.</returns>
        public Task<Result<Dictionary<string, string>>> AuthenticateAsync(string username, 
                                                                          string password, 
                                                                          Dictionary<string, string>? parameters = null);
        public Task<Result> LogoutAsync();
        public Result<string> HashPassword(string password);
    }
}
