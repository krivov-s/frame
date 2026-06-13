using Frame.Shared;

namespace Frame.Infrastructure.DBContext;

public class DatabaseSettings
{
    public const string DbTypePostgres = "Postgres";
    public const string DbTypeSql = "MsSql";

    public string DbServerType { get; set; } = "";
    public string ConnectionString { get; set; } = "";
    /// <summary>
    /// Отдельно значение Username прописывается в переменных окружения в Vault. При этом в ConnectionString должно отсутствовать. 
    /// </summary>
    public string Username { get; set; } = "";
    /// <summary>
    /// Отдельно значение Password прописывается в переменных окружения в Vault. При этом в ConnectionString должно отсутствовать. 
    /// </summary>
    public string Password { get; set; } = "";

    public void Verify()
    {
        if (DbServerType != DbTypePostgres && DbServerType != DbTypeSql)
        {
            throw new FrameException(
                $"{nameof(DatabaseSettings)}: недопустимое значение {nameof(DbServerType)} = '{DbServerType}'");
        }

        if (ConnectionString == "")
        {
            throw new FrameException(
                $"{nameof(DatabaseSettings)}: отсутствует значение {nameof(ConnectionString)}");
        }
    }
    /// <summary>
    /// Итоговая строка подключения, которая учитывает наличие отдельных Username и Password.
    /// Если они указаны - то добавляются к строке как Username= и Password= 
    /// </summary>
    public string ResultConnectionString
    {
        get
        {
            string connectionString = ConnectionString;
            if (connectionString.Length > 0)
            {
                if (Username.Length > 0)
                {
                    connectionString += $";Username={Username}";
                }
            
                if (Password.Length > 0)
                {
                    connectionString += $";Password={Password}";
                }
            }

            return connectionString;
        }
    }
}