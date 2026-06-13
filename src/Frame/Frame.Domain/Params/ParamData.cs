using System.Text.Json.Serialization;
using Frame.Domain.Entities.Metadata;
using Frame.Shared;

namespace Frame.Domain.Params;

public class ParamData
{
    public ParamData()
    {
    }

    public ParamData(ParamData source)
    {
        InitParamValue(source.TypeParam, source.Value, source.IsHidden, source.HumanName, 
            source.Select, source.Where, source.OrderBy);
    }
    
    public ParamData(string paramType, object? value, bool isHidden = false, string humanName = "", 
        string select = "", string where = "", string orderBy = "", string valuesList = "")
    {
        InitParamValue(paramType, value, isHidden, humanName, select, where, orderBy, valuesList);
    }

    public ParamData(object value, bool isHidden = false, string humanName = "",
        string select = "", string where = "", string orderBy = "", string valuesList = "")
    {
        InitParamValue("", value, isHidden, humanName, select, where, orderBy, valuesList);
    }

    /// <summary>
    /// Если задано значение объекта, тип игнорируется.
    /// </summary>
    /// <param name="paramType">Тип параметра (строка). Если задано значение - его тип является превалирующим, строковый будет проигнорирован.</param>
    /// <param name="value">Значение параметра</param>
    /// <param name="isHidden">Параметр скрыт от пользователя</param>
    /// <param name="humanName">"Человеческое" имя параметра для отображения в интерфейсе пользователя</param>
    /// <param name="select">Секция Select запроса для параметра - ссылки на объект</param>
    /// <param name="where">Секция Where запроса для параметра - ссылки на объект</param>
    /// <param name="orderBy">Секция OrderBy запроса для параметра - ссылки на объект</param>
    /// <param name="valuesList">Список допустимых значений, разделенных точкой с запятой. Используется только для простых скалярных типов.</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="Exception"></exception>
    private void InitParamValue(string paramType, object? value, bool isHidden, string humanName, 
        string select, string where, string orderBy, string valuesList = "")
    {
        if (paramType.Length == 0 && value == null)
        {
            throw new FrameException("Тип параметра не задан, значение = null, невозможно определить тип!");
        }

        if (paramType.Length == 0 && value != null)
        {
            paramType = EntityMetadata.BiuldExtTypeName(value.GetType());
        }
        
        TypeParam = paramType;
        Value = value;
        IsHidden = isHidden;
        HumanName = humanName;
        Select = select;
        Where = where;
        OrderBy = orderBy;
        ValuesList = valuesList;
    }

    /// <summary>
    /// Если задано значение объекта, тип игнорируется.
    /// </summary>
    /// <param name="value">Значение параметра</param>
    /// <param name="isHidden">Параметр скрыт от пользователя</param>
    /// <param name="humanName">"Человеческое" имя параметра для отображения в интерфейсе пользователя</param>
    /// <param name="select">Секция Select запроса для параметра - ссылки на объект</param>
    /// <param name="where">Секция Where запроса для параметра - ссылки на объект</param>
    /// <param name="orderBy">Секция OrderBy запроса для параметра - ссылки на объект</param>
    /// <param name="valuesList">Список допустимых значений, разделенных точкой с запятой. Используется только для простых скалярных типов.</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="Exception"></exception>
    private void InitParamValue<T>(T? value, bool isHidden, string humanName, 
        string select, string where, string orderBy, string valuesList)
    {
        Type type = typeof(T);
        string paramType = EntityMetadata.BiuldExtTypeName(type);

        TypeParam = paramType;
        Value = value;
        IsHidden = isHidden;
        HumanName = humanName;
        ValuesList = valuesList;
    }

    /// <summary>
    /// Тип параметра, в формате c#. Сервисный метод получения имени в нужном формате - <see cref="EntityMetadata.BiuldExtTypeName"/>
    /// </summary>
    [JsonPropertyName(nameof(TypeParam))] public string TypeParam { get; set; } = "";
    
    /// <summary>
    /// Значение параметра
    /// </summary>
    [JsonPropertyName(nameof(Value))] public object? Value { get; set; }
    
    /// <summary>
    /// Признак того, что параметр не показывается пользователю в диалоге
    /// </summary>
    [JsonPropertyName(nameof(IsHidden))] public bool IsHidden { get; set; }
    
    /// <summary>
    /// Человеческое имя параметра
    /// </summary>
    [JsonPropertyName(nameof(HumanName))] public string HumanName { get; set; } = "";
    
    /// <summary>
    /// Секция Select запроса для параметра - ссылки на объект
    /// </summary>
    [JsonPropertyName(nameof(Select))] public string Select { get; set; } = "";
    
    /// <summary>
    /// Секция Where запроса для параметра - ссылки на объект
    /// </summary>
    [JsonPropertyName(nameof(Where))] public string Where { get; set; } = "";
    
    /// <summary>
    /// Секция OrderBy запроса для параметра - ссылки на объект
    /// </summary>
    [JsonPropertyName(nameof(OrderBy))] public string OrderBy { get; set; } = "";

    /// <summary>
    /// Список допустимых значений для выбора, разделенных точкой с запятой. Используется только для простых скалярных типов.
    /// </summary>
    [JsonPropertyName(nameof(ValuesList))] public string ValuesList { get; set; } = "";

    /// <summary>
    /// Список допустимых значений из <see cref="ValuesList"/> в виде коллекции строк
    /// </summary>
    public List<string> StringValuesList
    {
        get
        {
            if (ValuesList.Length > 0)
            {
                return [..ValuesList.Split(';')];
            }
            else
            {
                return [];
            }
        }
    }
    
    public void ConvertValue()
    {
        if (Value != null && !string.IsNullOrEmpty(TypeParam))
        {
            var targetType = Type.GetType(TypeParam) ?? throw new FrameException($"Тип {TypeParam} не найден!");
            Value = Convert.ChangeType(Value, targetType);
        }
    }
}

