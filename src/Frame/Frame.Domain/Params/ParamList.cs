using System.Dynamic;
using System.Text.Encodings.Web;
using System.Text.Json;
using Frame.Domain.Entities.Core;
using Frame.Shared;

namespace Frame.Domain.Params
{
    public class ParamList : DynamicObject
    {
        public static class DefaultParamNames
        {
            public const string UiDarkTheme = "UIDarkTheme";
        }

        /// <summary>
        /// Dictionary со списком параметров
        /// </summary>
        private Dictionary<string, ParamData> _parameters = new();

        /// <summary>
        /// Уведомление об изменении параметра из списка (синхронная версия)
        /// </summary>
        public event Action<string, object?>? ParameterChanged;

        /// <summary>
        /// Уведомление об изменении параметра из списка (асинхронная версия)
        /// </summary>
        public event Func<string, object?, Task>? ParameterChangedAsync;

        /// <summary>
        /// Представление списка параметров в виде Dictionary
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, ParamData> AsDictionary() => _parameters;

        /// <summary>
        /// Представление списка параметров в виде Dictionary{string, dynamic} для использования в скриптах.
        /// При этом BaseEntity преобразуется в ExpandoObject вместе с динамическими атрибутами.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, dynamic?> AsDynDictionary()
        {
            dynamic? __getValue(ParamData paramData)
            {
                BaseEntity? entity = paramData.Value as BaseEntity; 
                if(entity != null)
                {
                    return entity.ToExpando();
                }

                return paramData.Value;
            }
            
            var dynamicDict = _parameters.ToDictionary(
                pair => pair.Key,
                pair => __getValue(pair.Value)
            );    
            return dynamicDict;
        }

        /// <summary>
        /// Общее количество параметров в списке
        /// </summary>
        public int CountAll => _parameters.Count;

        /// <summary>
        /// Количество параметров в списке, видимых пользователю (у которых IsHidden == false)
        /// </summary>
        public int CountVisible => _parameters.Count(p => p.Value is { IsHidden: false });

        /// <summary>
        /// Проверка наличия в списке параметра с заданным именем
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public bool IsParameterExists(string paramName) => _parameters.ContainsKey(paramName);

        /// <summary>
        /// В случае, если параметр не найден по имени - выбрасывает Exception
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public Result<bool> IsParameterHidden(string paramName)
        {
            if (_parameters.TryGetValue(paramName, out var paramValue))
            {
                return Result<bool>.Success(paramValue.IsHidden);
            }
            else
            {
                return Result<bool>.Error($"Параметр и именем {paramName} в списке параметров отсутствует!");
            }
        }

        /// <summary>
        /// Возвращает значение параметра (фактическое значение, не <see cref="ParamData"/>) 
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public Result<object?> GetByName(string paramName)
        {
            Result<ParamData?> resParamData = GetParamData(paramName);

            return resParamData.IsError 
                ? Result<object?>.Error(resParamData.ErrorResult) 
                : Result<object?>.Success(resParamData.Value?.Value);
        }

        private Result<ParamData?> GetParamData(string paramName)
        {
            if (string.IsNullOrEmpty(paramName))
                return Result<ParamData?>.Error("Не задано имя параметра");

            if (!_parameters.TryGetValue(paramName, out ParamData? paramValue))
                return Result<ParamData?>.Error($"Параметр '{paramName}' не найден в списке параметров");

            return Result<ParamData?>.Success(paramValue);
        }
        
        /// <summary>
        /// Добавление нового параметра или замена существующего
        /// </summary>
        /// <param name="paramName">Имя параметра</param>
        /// <param name="paramData">Структура с данными параметра <see cref="ParamData"/></param>
        public void AddParam(string paramName, ParamData paramData)
        {
            _checkParamName(paramName);
            paramName = NormalizeParamName(paramName);
            _parameters[paramName] = paramData;
            NotifySubscribers(paramName, paramData.Value);
        }

        /// <summary>
        /// Добавление параметра в список или замена значения существующего.
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="paramValue">Значение параметра</param>
        /// <param name="isHidden">Параметр скрыт от пользователя</param>
        /// <param name="humanName">"Человеческое" имя параметра для отображения в интерфейсе пользователя</param>
        /// <returns></returns>
        public void AddParam<T>(string paramName, T paramValue, bool isHidden = false, string humanName = "")
        {
            _checkParamName(paramName);
            if (string.IsNullOrEmpty(paramName))
            {
                throw new ArgumentNullException("Нельзя добавлять в список параметров параметр без имени!");
            }
            paramName = NormalizeParamName(paramName);
            _parameters[paramName] = new ParamData(paramValue!, isHidden, humanName);
            NotifySubscribers(paramName, paramValue);
        }

        /// <summary>
        /// Добавление параметра в список или замена значения существующего.
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="paramValue">Значение параметра</param>
        /// <param name="isHidden">Параметр скрыт от пользователя</param>
        /// <param name="humanName">"Человеческое" имя параметра для отображения в интерфейсе пользователя</param>
        /// <returns></returns>
        public async Task AddParamAsync<T>(string paramName, T paramValue, bool isHidden = false, string humanName = "")
        {
            _checkParamName(paramName);
            paramName = NormalizeParamName(paramName);
            _parameters[paramName] = new ParamData(paramValue!, isHidden, humanName);
            await NotifySubscribersAsync(paramName, paramValue);
        }

        // Реализация коллекции Values
        public IEnumerable<KeyValuePair<string, object?>> Values
        {
            get
            {
                foreach (var param in _parameters)
                {
                    yield return new KeyValuePair<string, object?>(param.Key, param.Value.Value);
                }
            }
        }

        public object? this[string paramName]
        {
            get => GetByName(paramName).Value;
            set
            {
                _checkParamName(paramName);
                Result<ParamData?> resData = GetParamData(paramName);
                if (resData.IsErrorOrNull)
                {
                    if(value == null) return;
                    AddParam(paramName, value);
                }
                else
                {
                    ParamData data = resData.Value!;
                    data.Value = value;
                    AddParam(paramName, data);
                }
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            result = null;
            Result<object?> res = GetByName(binder.Name);
            if (res.IsError || res.Value == null)
            {
                return false;
            }

            result = res.Value;
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            AddParam(binder.Name, value);
            return true;
        }

        public void RemoveParameter(string paramName)
        {
            if (_parameters.Remove(paramName))
                NotifySubscribers(paramName, null);
        }

        public string ToJson()
        {
            try
            {
                return JsonSerializer.Serialize(_parameters, _serializerOptions);
            }
            catch (Exception ex)
            {
                string err = $"Ошибка сериализации списка параметров: {ex.Message}";
                throw new FrameParamListSerializeException(err);
            }
        }

        public void FromJson(string json)
        {
            try
            {
                if (json != null && json.Length > 0)
                {
                    _parameters = JsonSerializer.Deserialize<Dictionary<string, ParamData>>(json, _serializerOptions)
                                  ?? new();
                }
                else
                {
                    _parameters = new();
                }
            }
            catch (Exception ex)
            {
                string err = $"Ошибка десериализации списка параметров: {ex.Message}";
                throw new FrameParamListSerializeException(err);
            }
        }

        public static ParamList CreateFromJson(string json)
        {
            try
            {
                return new ParamList
                {
                    _parameters = JsonSerializer.Deserialize<Dictionary<string, ParamData>>(json, _serializerOptions) ??
                                  new()
                };
            }
            catch (Exception ex)
            {
                string err = $"Ошибка десериализации списка параметров: {ex.Message}";
                throw new FrameParamListSerializeException(err);
            }
        }

        private void NotifySubscribers(string paramName, object? paramValue)
        {
            ParameterChanged?.Invoke(paramName, paramValue);
        }

        private async Task NotifySubscribersAsync(string paramName, object? paramValue)
        {
            if (ParameterChangedAsync == null) return;

            foreach (var handler in ParameterChangedAsync.GetInvocationList().Cast<Func<string, object?, Task>>())
            {
                await handler.Invoke(paramName, paramValue);
            }
        }

        private static JsonSerializerOptions _serializerOptions => new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new ParamJsonConverter() }
        };

        public void SetParamValue(string name, ParamData data)
        {
            _parameters[name] = data;
        }

        public ParamData GetParamValue(string name)
        {
            return _parameters[name];
        }
        
        /// <summary>
        /// Получение фактического типизированного значения параметра.
        /// В случае ошибки, несовпадения типа или отсутствия параметра - возвращается defaultValue.
        /// Если в параметре сидит BaseEntity, а запрашивается int - возвращается Id объекта
        /// </summary>
        /// <param name="paramName">Имя параметра</param>
        /// <param name="defaultValue">Значение по-умолчанию (в случае ошибки, несовпадения типа или отсутствия параметра)</param>
        /// <typeparam name="T">Тип параметра</typeparam>
        /// <returns>Фактическое значение параметра</returns>
        public T? GetParamValue<T>(string paramName, T defaultValue)
        {
            Result<ParamData?> resParamData = GetParamData(paramName);

            if (resParamData.IsError || resParamData.Value == null)
            {
                return defaultValue;
            }

            var resultData = resParamData.Value;
            if (resultData.Value == null)
            {
                return default(T);
            }
            
            object paramValue = resultData.Value;

            // Если в параметре BaseEntity а запрашивается int - вернем Id объекта
            if (paramValue is BaseEntity baseEntity && typeof(T) == typeof(int))
            {
                object id = baseEntity.Id;
                return (T) id;
            }
            
            return ServiceTools.ConvertValue(paramValue, defaultValue);
        }
        

        /// <summary>
        /// Объединение списка параметров. В случае, если в списках есть параметры с одинаковыми именами,
        /// в результирующем списке останется значения параметра из <see cref="pl2"/> 
        /// </summary>
        /// <param name="pl1">Список параметров 1</param>
        /// <param name="pl2">Список параметров 2</param>
        /// <returns></returns>
        public static ParamList operator +(ParamList pl1, ParamList pl2)
        {
            ParamList result = new ParamList();
            foreach (var param in pl1.AsDictionary())
            {
                result.SetParamValue(param.Key, param.Value);
            }

            foreach (var param in pl2.AsDictionary())
            {
                result.SetParamValue(param.Key, param.Value);
            }

            return result;
        }

        public static string NormalizeParamName(string paramName)
        {
            return paramName.Replace(" ", "_");
        }
        
        private static void _checkParamName(string paramName)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                throw new ArgumentNullException(paramName, "Нельзя добавлять в список параметров параметр без имени!");
            }
        }
    }
}


