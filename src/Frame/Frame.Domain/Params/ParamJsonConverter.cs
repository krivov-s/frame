using System.Text.Json;
using System.Text.Json.Serialization;
using Frame.Domain.Entities.Core;

namespace Frame.Domain.Params;

public class ParamJsonConverter : JsonConverter<Dictionary<string, ParamData>>
{
    public override Dictionary<string, ParamData> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dictionary = new Dictionary<string, ParamData>();
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                // 1. Обрабатываем ключ
                string transformedKey = ParamList.NormalizeParamName(property.Name);
                
                // 2. Обрабатываем ParamData
                JsonElement jsonObject = property.Value;

                // Извлекаем Type и Value
                string? typeName = jsonObject.GetProperty(nameof(ParamData.TypeParam)).GetString();
                if (string.IsNullOrEmpty(typeName))
                {
                    throw new JsonException($"Не найдено или не заполнено поле '{nameof(ParamData.TypeParam)}' отсутствует или пусто.");
                }

                var type = Type.GetType(typeName) ?? throw new JsonException($"Тип '{typeName}' не найден.");

                object? value = null;
                if (typeof(BaseEntity).IsAssignableFrom(type))
                {
                    // Если это BaseObject или наследник, читаем только Id
                    if (jsonObject.TryGetProperty(nameof(ParamData.Value), out JsonElement elementIntId))
                    {
                        if (elementIntId.ValueKind == JsonValueKind.Number)
                        {
                            int entityId = elementIntId.GetInt32();
                            value = entityId;
                        }
                        // value = Activator.CreateInstance(type); // Создаём объект
                        // if (value is BaseEntity baseObject)
                        // {
                        //     baseObject.Id = entityId;
                        // }
                    }
                }
                else
                {
                    // Для других типов используем стандартный десериализатор
                    JsonElement element = jsonObject.GetProperty(nameof(ParamData.Value));
                    var text = element.GetRawText() ?? "";
                    if (text != null && text != "" && text != "null")
                    {
                        value = JsonSerializer.Deserialize(text, type, options);
                    }
                    else
                    {
                        value = null;
                    }
                    
                }

                bool hidden = false;
                if (jsonObject.TryGetProperty(nameof(ParamData.IsHidden), out JsonElement elementHidden))
                {
                    hidden = elementHidden.GetBoolean();
                }
        
                string humanName = "";
                if (jsonObject.TryGetProperty(nameof(ParamData.HumanName), out JsonElement humanNameElement))
                {
                    humanName = humanNameElement.GetString() ?? "";
                }

                string select = "";
                if (jsonObject.TryGetProperty(nameof(ParamData.Select), out JsonElement selectElement))
                {
                    select = selectElement.GetString() ?? "";
                }

                string where = "";
                if (jsonObject.TryGetProperty(nameof(ParamData.Where), out JsonElement whereElement))
                {
                    where = whereElement.GetString() ?? "";
                }

                string orderBy = "";
                if (jsonObject.TryGetProperty(nameof(ParamData.OrderBy), out JsonElement orderByElement))
                {
                    orderBy = orderByElement.GetString() ?? "";
                }

                string valuesList = "";
                if (jsonObject.TryGetProperty(nameof(ParamData.ValuesList), out JsonElement valuesListElement))
                {
                    valuesList = valuesListElement.GetString() ?? "";
                }

                ParamData data = new ParamData(typeName, value, hidden, humanName, select, where, orderBy, valuesList);
                dictionary[transformedKey] = data;
            }
        }
        return dictionary;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, ParamData> dictionary, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in dictionary)
        {
            string modifiedKey = kvp.Key.Replace(" ", "_"); // Преобразуем ключи
            ParamData data = kvp.Value;

            writer.WritePropertyName(modifiedKey);
            writer.WriteStartObject();
            // Записываем Type
            writer.WriteString(nameof(ParamData.TypeParam), data.TypeParam);

            // Сериализуем Value
            if (data.Value is BaseEntity baseObject)
            {
                // Для BaseObject записываем только Id
                writer.WriteNumber(nameof(ParamData.Value), baseObject.Id);
            }
            else
            {
                // Для других типов стандартная сериализация
                writer.WritePropertyName(nameof(ParamData.Value));
                JsonSerializer.Serialize(writer, data.Value, options);
            }

            writer.WriteBoolean(nameof(ParamData.IsHidden), data.IsHidden);
            writer.WriteString(nameof(ParamData.HumanName), data.HumanName);
            writer.WriteString(nameof(ParamData.Select), data.Select);
            writer.WriteString(nameof(ParamData.Where), data.Where);
            writer.WriteString(nameof(ParamData.OrderBy), data.OrderBy);
            writer.WriteString(nameof(ParamData.ValuesList), data.ValuesList);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}

public static class ParamListExtension
{
}
