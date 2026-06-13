using System.Text.Encodings.Web;
using System.Text.Json;
using Frame.Domain.Entities.Core.Params;
using Frame.Domain.Params;
using Frame.WebLib.Components.Shared;

namespace Frame.WebLib.Shared;

/// <summary>
/// Класс описывает структуру параметров интерфейса пользователя, которые сохраняются в профиле пользователя
/// в <see cref="UserProfile.UIParamsJson"/>
/// </summary>
public class UserUiParams
{
    /// <summary>
    /// Настройки (параметры) пользователя для списков на основе компонента <see cref="EntitiesListDetail{TEntity,TParentComponent}"/> 
    /// </summary>
    public Dictionary<string, EntitiesListDetailSettings> EntitiesListDetailSettings { get; set; } = [];

    #region ========== Сериализация/десериализация ==========
    
    private static JsonSerializerOptions _serializerOptions => new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new ParamJsonConverter() }
    };
    
    public static string ToJson(UserUiParams userUiParams)
    {
        try
        {
            return JsonSerializer.Serialize(userUiParams, _serializerOptions);
        }
        catch (Exception ex)
        {
            string err = $"Ошибка сериализации списка параметров интерфейса пользователя: {ex.Message}";
            throw new Exception(err);
        }
    }

    public static UserUiParams FromJson(string json)
    {
        try
        {
            if (json != null && json.Length > 0)
            {
                return JsonSerializer.Deserialize<UserUiParams>(json, _serializerOptions)
                              ?? new();
            }
            else
            {
                return new();
            }
        }
        catch (Exception ex)
        {
            string err = $"Ошибка десериализации списка параметров интерфейса пользователя: {ex.Message}";
            throw new Exception(err);
        }
    }
    
    #endregion
}

/// <summary>
/// Настройки <see cref="EntitiesListDetail{TEntity,TParentComponent}"/>, которые сохраняются в БД
/// и считываются при отрисовке списка
/// </summary>
public class EntitiesListDetailSettings
{
    public int? FilterId { get; set; }
    public string FilterParamListJson { get; set; } = "";
    public int RowsPerPage { get; set; } = 10;
}
