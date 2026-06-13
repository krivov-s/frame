namespace Frame.WebLib.Security.Keycloak
{
    public class KeycloakSettings
    {
        public const string AuthType = "Keycloak";
        // public const string OAuth2Path = "OAUTH2_PATH";
        // public const string VAR_KeycloakClientId = "OAUTH2_CLIENT_SECRET";
        // public const string VAR_KeycloakSecretKey = "OAUTH2_CLIENT_ID";

        /// <summary>
        /// Адрес сервера keycloak, например http://localhost:8282
        /// </summary>
        public string Authority { get; set; } = "";
        
        /// <summary>
        /// Realm, зарегистрированный у Keycloak, к которому относится приложение
        /// </summary>
        public string Realm { get; set; } = "";
        
        /// <summary>
        /// Endpoint для получения ключей
        /// </summary>
        public string OAuth2TokenEndpoint { get; set; } = "/protocol/openid-connect/token";
        
        public string DefaultClientId { get; set; } = "";
        
        /// <summary>
        /// Список клиентов (client_id, client_secret), зарегистрированных в Keycloak, с которыми работает приложение
        /// </summary>
        public Dictionary<string, string> KeycloakClients { get; set; } = [];
        
        public void Verify()
        {
            if (Authority == "")
            {
                throw new Exception(
                    $"{nameof(KeycloakSettings)}: не задано значение {nameof(Authority)}.");
            }

            if (Realm == "")
            {
                throw new Exception(
                    $"{nameof(KeycloakSettings)}: не задано значение {nameof(Realm)}.");
            }

            if (KeycloakClients.Count == 0)
            {
                throw new Exception(
                    $"{nameof(KeycloakSettings)}: список клиентов пуст.");
            }
        }
        
    }
}
