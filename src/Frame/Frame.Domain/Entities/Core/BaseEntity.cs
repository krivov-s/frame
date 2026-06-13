
using System.Collections;
using System.Dynamic;
using System.Reflection;
using Frame.Shared;

namespace Frame.Domain.Entities.Core
{
    /// <summary>
    /// Базовый класс для всех сущностей системы
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Идентификатор объекта (первичный ключ)
        /// </summary>
        public int Id { get; set; }
        
        ///// <summary>
        ///// Дата создания объекта
        ///// </summary>
        public DateTime DateCreate { get; set; } = DateTime.UtcNow;
        
        ///// <summary>
        ///// Дата изменения объекта
        ///// </summary>
        public DateTime? DateModify { get; set; }
        
        /// <summary>
        /// Описание экземпляра (будет использоваться в списках и других местах, где нужно коротко охарактеризовать что это за запись)
        /// </summary>
        public virtual string Description { get; } = "";
        
        /// <summary>
        /// Ключ объекта, являющийся уникальным для всей системы: конкатенация имени класса и идентификатора объекта. 
        /// Может использоваться для организации нестрогих связок (например - с хранилищем файлов)
        /// </summary>
        public string Key
        {
            get
            {
                if (Id != 0)
                {
                    // Поиск и замена Proxy нужна для того, чтобы исключить влияние на имена LazyLoading, если таковое будет использоваться.
                    string strType = GetType().Name.Replace("Proxy", "", StringComparison.CurrentCulture).ToLower();
                    return $"{strType}_{Id}";
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Метод вызывается непосредственно перед записью в хранилище.
        /// Может использоваться для простых проверок и корректировки значений свойств объекта
        /// непосредственно перед сохраненнием/удалением. 
        /// </summary>
        public virtual Result OnBeforeSave(bool isDeleting){return Result.Success;}


        /// <summary>
        /// Метод вызывается непосредственно после записи в хранилище с указанием флага ошибки, если она была в ходе сохранения.
        /// Если запись осуществляется в транзакции то метод будет вызван дважды: после сохранения
        /// с установленным параметром <see cref="isTransactionActive"/> и после завершения транзакции
        /// с параметром <see cref="isTransactionActive"/> = false и установленным флагом ошибки.
        /// </summary>
        /// <param name="isTransactionActive">true если вызов выполняется внутри транзакции,
        /// false - если вне транзакции или после успешного завершения транзакции.</param>
        /// <param name="isError">true если в процесса записи или коммита произошла ошибка, иначе - false</param>
        public virtual void OnAfterSave(bool isTransactionActive, bool isError){}
        public virtual void OnAfterDelete(bool isTransactionActive, bool isError){}
        
        /// <summary>
        /// Дополнительные динамические свойства объекта. Не сохраняются в БД.
        /// Предназначены для вычисления в RunTime и сохранения вычисленных значений в течение срока жизни объекта.
        /// Могут использоваться, например, для предварительной подготовки (расчета) данных на уровне Appliction
        /// и их передачи в систему подготовки отчетов, для использования в отчетных формах.
        /// </summary>
        public dynamic DynAttrs { get; } = new ExpandoObject();

        /// <summary>
        /// Получение типизированного значения динамического атрибута
        /// </summary>
        /// <param name="name">Наименование атрибута</param>
        /// <param name="defaultValue">Значение по-умолчанию в случае отсутствия атрибута</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? GetDynAttr<T>(string name, T? defaultValue = default(T))
        {
            if (string.IsNullOrEmpty(name))
                return defaultValue;

            IDictionary<string, object?> dynAttrs = DynAttrs;
            
            if (!dynAttrs.TryGetValue(name, out object? paramValue))
                return defaultValue;
            
            if(paramValue == null) return defaultValue;
            
            return ServiceTools.ConvertValue<T>(paramValue, defaultValue);
        }


        /// <summary>
        /// Предельно допустимый уровень вложенности для конвертации объектов в Dictionary
        /// </summary>
        const int MAX_NEST_LEVEL = 5;

        /// <summary>
        /// Возвращает объект как набор словарей, причем вложенные объекты также преобразуются в словари,
        /// а коллекции (списки объектов) - в список словарей. Динамические атрибуты из DynAttrs также сохраняются
        /// в словарь в виде линейного списка (аналогично обычным атрибутам объекта)
        /// Аргумент nestLevel нужен для исключения зацикленности в конструкциях типа User - UserProfile - User
        /// По-умолчанию делаем предельно допустимый уровень вложенности = 3
        /// </summary>
        /// <param name="nestLevel">Уровень вложенности. При первом вызове должен быть установлен = 0</param>
        /// <returns></returns>
        public Dictionary<string, dynamic?> ToDictionary(int nestLevel = 0)
        {
            Dictionary<string, dynamic?> result = [];

            // 1. Добавляем обычные свойства
            var props = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                // На первый проход исключаем DynAttrs
                if (prop.Name == nameof(DynAttrs))
                    continue;

                string key = prop.Name;
                object? value =  prop.GetValue(this);
                _addValueToDictionary(key, value, result, nestLevel); 
            }
            
            // 2. Добавляем из DynAttrs
            foreach (var kv in DynAttrs)
            {
                string key = kv.Key;
                object? value =  kv.Value;
                _addValueToDictionary(key, value, result, nestLevel); 
            }

            return result;
        }

        private void _addValueToDictionary(string key, object? value, Dictionary<string, dynamic?> result, 
            int nestLevel, bool modifyKeyIfExists = true)
        {
            if (result.ContainsKey(key))
            {
                if (modifyKeyIfExists)
                {
                    for (int num = 1; num < 65535; num++)
                    {
                        string newKey = $"{key}_{num}";
                        if (!result.ContainsKey(newKey))
                        {
                            key = newKey;
                            break;
                        }
                    }
                }
            }
            switch (value)
            {
                case null:
                    result[key] = value;
                    break;
                case BaseEntity entity:
                    result[key] = (nestLevel < MAX_NEST_LEVEL) ? entity.ToDictionary(nestLevel + 1) : null;
                    break;
                default:
                {
                    if (_isListOfBaseEntity(value))
                    {
                        List<Dictionary<string, dynamic?>> list = _convertListToDictionary(value, nestLevel);
                        result[key] = list;
                    }
                    else
                    {
                        result[key] = value;
                    }

                    break;
                }
            }
        }

        private static List<Dictionary<string, dynamic?>> _convertListToDictionary(object value, int nestLevel)
        {
            var enumerable = (IEnumerable)value;
            var list = new List<Dictionary<string, dynamic?>>();
            if(enumerable == null) return list;
            if (nestLevel == MAX_NEST_LEVEL) return list;
            
            foreach (var item in enumerable)
            {
                BaseEntity? entity = item as BaseEntity;
                if (entity != null)
                {
                    list.Add(entity.ToDictionary(nestLevel + 1));
                }
            }

            return list;
        }

        /// <summary>
        /// Превращение объекта в ExpandoObject. Превращаются также и все связанные (считанные) BaseEntity.
        /// Все динамические атрибуты из DynAttrs сохраняются как линейные атрибуты - члены словаря
        /// </summary>
        /// <returns></returns>
        public ExpandoObject ToExpando()
        {
            var expando = new ExpandoObject() as IDictionary<string, object?>;
            Dictionary<string, dynamic?> objAsDict = ToDictionary();
            foreach (var kvp in objAsDict)
            {
                expando.Add(kvp.Key, kvp.Value);
            }

            return (ExpandoObject)expando;
        }        
        
        public static bool operator ==(BaseEntity? left, BaseEntity? right)
        {
            if (left is null && right is null)
                return true;
            else if (left is not null && right is not null)
                return left.Equals(right);
            else
                return false;
        }
        public static bool operator !=(BaseEntity? left, BaseEntity? right)
        {
            return !(left == right);
        }
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (GetType() != obj.GetType())
                return false;

            BaseEntity? otherEntity = (BaseEntity?)obj;
            if (otherEntity is null)
                return false;
            if (Id == 0 || otherEntity.Id == 0)
                return false;

            // И вот собственно основной принцип сравнения: если идентификаторы совпадают - объекты одинаковы, если различаются - разные.
            if (Id == otherEntity.Id)
                return true;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            string strDisplay = Description;

            if (strDisplay.Length == 0)
            {
                strDisplay = $"{this.GetType().Name} Id = {Id}";
            }

            return strDisplay;
        }

        public static TEntity CreateInstance<TEntity>() where TEntity : BaseEntity, new()
        {
            return new TEntity();
        }
        
        /// <summary>
        /// По-умолчанию метод создает поверхностную (shallow) копию объекта.
        /// Все атрибуты сохраняются, но обнуляется Id объекта, DateCreate и DateModify.
        /// <para>
        /// Внимание! Все списки также копируются, но это ссылки на те же списки в оригинальном объекте!
        /// Вызов метод List.Clear() вызывает очистку списка и в объекте-источнике, поэтому
        /// для обнуления списка в новом объекте нужно полностью пересоздавать List = [];
        /// </para>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public TEntity Clone<TEntity>() where TEntity : BaseEntity, new()
        {
            TEntity? newEntity = MemberwiseClone() as TEntity;
            if (newEntity == null)
            {
                throw new FrameException($"Поверхностное клонирование объекта {this} вернуло null");
            }
            // У клона зануляем Id и даты создания/изменения: он еще не в БД
            newEntity.Id = 0;
            newEntity.DateCreate = DateTime.UtcNow;
            newEntity.DateModify = null;
            return newEntity;
        }
        
        private bool _isListOfBaseEntity(object? obj)
        {
            if (obj == null) return false;

            var type = obj.GetType();

            // Проверяем, что это List<T>
            if (type.IsGenericType 
                && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type itemType = type.GetGenericArguments()[0];

                // Проверяем, что T наследует BaseEntity
                return typeof(BaseEntity).IsAssignableFrom(itemType);
            }

            return false;
        }

    }
}
