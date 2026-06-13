namespace Frame.WebLib.Security.Keycloak
{
    public static class KeycloakConstants
    {
        // Константы для claims токенов
        public const string RealmAccessPropName = "realm_access";
        public const string RolesPropName = "roles";
        public const string ResourceAccessPropName = "resource_access";

        public const string AccessTokenPropName = "access_token";
        public const string RefreshTokenPropName = "refresh_token";
        public const string ExpiresInPropName = "expires_in";
        public const string RefreshExpiresInPropName = "refresh_expires_in";
        public const string TokenTypePropName = "token_type";
        public const string SessionStatePropName = "session_state";
        public const string ScopePropName = "scope";

        // Константы для ключей в запросе
        public const string UsernamePropName = "username";
        public const string PasswordPropName = "password";
        public const string GrantTypePropName = "grant_type";
        public const string ClientIdPropName = "client_id";
        public const string ClientSecretPropName = "client_secret";

        // Константы для значений grant_type
        public const string GrantTypePasswordPropName = "password";
        public const string GrantTypeRefreshTokenPropName = "refresh_token";
        public const string GrantTypeClientCredentialsPropName = "client_credentials";
    }
}
