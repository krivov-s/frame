using System.CodeDom.Compiler;
using System.Reflection;
using Frame.Domain.Entities.Core;
using Frame.Shared;

namespace Frame.Domain.Entities.Metadata;

/// <summary>
/// Хранение метаданных по объектам системы, списков имен и типов, карта связка имя-тип.
/// Имена типов здесь - короткие, без namespace и assembly. 
/// </summary>
public static class EntityMetadata
{
    private static readonly List<string> _descendantClassNames = [];
    private static List<Type> _descendantClassTypes = [];
    private static readonly Dictionary<string, EntityMetaInfo> _mapNameToMeta = [];

    private static readonly object _lockMaps = new object();
    
    public static List<string> GetAllEntitiesClassNames()
    {
        CheckLists();
        return _descendantClassNames;
    }

    public static List<Type> GetAllEntitiesClassTypes()
    {
        CheckLists();
        return _descendantClassTypes;
    }

    public static List<EntityMetaInfo> GetAllEntitiesMeta()
    {
        CheckLists();

        return _mapNameToMeta.Values.ToList();
    }
    
    public static bool IsProxy(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return IsProxy(entity.GetType());
    }

    public static bool IsProxy<TEntity>()
    {
        return IsProxy(typeof(TEntity));
    }

    public static bool IsProxy(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        // Метод 1: Проверка имени типа
        var typeName = entityType.Name;
        if (typeName.Contains("Castle.Proxies.") || typeName.EndsWith("Proxy"))
            return true;

        // Метод 2: Проверка наличия специфических атрибутов
        var attrs = entityType.GetCustomAttributes(typeof(GeneratedCodeAttribute), true);
        if (attrs.Length > 0)
            return true;

        return false;
    }

    /// <summary>
    /// Получение "чистого" типа объекта (уход от Lazy Loading Proxy)
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static Type GetEntityType<TEntity>()
    {
        Type type = typeof(TEntity);
        return GetEntityType(type);
    }

    /// <summary>
    /// Получение "чистого" типа объекта (уход от Lazy Loading Proxy)
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static Type GetEntityType(Type type)
    {
        return IsProxy(type) ? type.BaseType ?? type : type;
    }

    /// <summary>
    /// Получение "чистого" типа объекта (уход от Lazy Loading Proxy)
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static Type GetEntityType(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return GetEntityType(entity.GetType());
    }

    public static Type? GetEntityType(string type)
    {
        CheckLists();
        EntityMetaInfo? metaInfo = _mapNameToMeta.GetValueOrDefault(type);
        return metaInfo?.DotNetType;
    }

    public static EntityMetaInfo? GetEntityMeta<TEntity>()
    {
        CheckLists();
        string type = typeof(TEntity).Name;
        EntityMetaInfo? metaInfo = _mapNameToMeta.GetValueOrDefault(type);
        return metaInfo;
    }

    /// <summary>
    /// Возвращает true, если тип подходит под определение справочника (менее 10 полей, содержит поля Name и Descr/Comment)
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    public static bool IsEntitySprav(Type  entityType)
    {
        CheckLists();
        string type = entityType.Name;
        
        // Если метаданные по типу содержаи поля Имя и Descr или Comment - то считаем что это справочник
        EntityMetaInfo? metaInfo = _mapNameToMeta.GetValueOrDefault(type);
        if(metaInfo != null && metaInfo.Fields.Count < 10 &&  
           metaInfo.Fields.Any(f => f.FrameField.Name == "Name") && 
           (
               metaInfo.Fields.Any(f => f.FrameField.Name == "Descr") ||
               metaInfo.Fields.Any(f => f.FrameField.Name == "Comment")
           )
        )
        {
            return true;
        }
        return false;
    }
    
    
    public static EntityMetaInfo? GetEntityMeta(string type)
    {
        CheckLists();
        EntityMetaInfo? metaInfo = _mapNameToMeta.GetValueOrDefault(type);
        return metaInfo;
    }

    /// <summary>
    /// Возвращает тип объекта в формате "{typeName}, {assemblyName}".
    /// Такой формат нужен для последующего использования в вызове Type.GetType   
    /// </summary>
    /// <param name="type">Тип, для которого нужно построить имя</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static string BiuldExtTypeName(Type? type)
    {
        if (type == null) return "";
        string typeName = type.FullName ?? type.Name;
        string assemblyName = type.Assembly.GetName().Name 
                              ?? throw new FrameException($"У типа {type} не удалось определить имя сборки!");
        string paramType = $"{typeName}, {assemblyName}";
        return paramType;
    }
    
    /// <summary>
    /// Метод возвращает "человеческое" имя класса, определенное во вложенном статическом классе Meta
    /// <code>
    /// public static class Meta
    /// {
    ///     public static readonly string HumanName = "Человеческое имя";
    ///     ...
    /// }
    /// </code>
    /// Если вложенный класс Meta отсутствует - возвращается простое имя класса Type.Name.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static string GetHumanName<TEntity>()
    {
        return GetHumanName(typeof(TEntity));
    }

    /// <summary>
    /// Метод возвращает "человеческое" имя класса, определенное во вложенном статическом классе Meta
    /// <code>
    /// public static class Meta
    /// {
    ///     public static readonly string HumanName = "Человеческое имя";
    ///     ...
    /// }
    /// </code>
    /// Если вложенный класс Meta отсутствует - возвращается простое имя класса Type.Name.
    /// </summary>
    /// <param name="tEntity"></param>
    /// <returns></returns>
    public static string GetHumanName(Type? tEntity)
    {
        if (tEntity == null)
        {
            return "";
        }

        Type? typeMeta = tEntity.GetNestedTypes()
            .FirstOrDefault(t => t.Name == "Meta");

        FieldInfo? fieldInfo = typeMeta?.GetField("HumanName", BindingFlags.Static | BindingFlags.Public);

        string? value = fieldInfo?.GetValue(null) as string;
        
        return value ?? tEntity.Name;
    }
    
    public static Result<bool> IsEntityReference(Type type, string propName)
    {
        // Получаем информацию о свойстве по имени
        PropertyInfo? propertyInfo = type.GetProperty(propName);
        
        // Проверяем существование свойства
        if (propertyInfo == null)
            return Result<bool>.Error($"Свойство с именем '{propName}' не найдено в классе {type.FullName}.");

        // Проверяем, является ли тип свойства производным от BaseEntity
        bool isEntity = propertyInfo.PropertyType.IsSubclassOf(typeof(BaseEntity)) ||
               propertyInfo.PropertyType == typeof(BaseEntity);
        return Result<bool>.Success(isEntity);
    }
    
    private static void CheckLists()
    {
        lock (_lockMaps)
        {
            if (_descendantClassNames.Count == 0 || _descendantClassTypes.Count == 0 || _mapNameToMeta.Count == 0)
            {
                _descendantClassNames.Clear();
                _descendantClassTypes.Clear();
                _mapNameToMeta.Clear();

                Type tBaseEntity = typeof(BaseEntity);

                // Получаем все загруженные сборки уровня Domain
                var assemblies = ServiceTools.GetAssemblies(".Domain");

                foreach (var assembly in assemblies)
                {
                    IEnumerable<Type> list = assembly.GetTypes().Where(type => type.IsSubclassOf(tBaseEntity));
                    foreach (Type itm in list)
                    {
                        if (!itm.IsGenericType)
                        {
                            // Добавляем только не generic types (BaseGenericEntity нам не нужен в этом списке)
                            _descendantClassNames.Add(itm.Name);
                            _descendantClassTypes.Add(itm);
                            _mapNameToMeta.Add(itm.Name, _createEntityMetaInfo(itm));
                        }
                    }
                }

                _descendantClassNames.Sort();
                _descendantClassTypes = _descendantClassTypes.OrderBy(t => t.Name).ToList();
            }
        }
    }
    
    /// <summary>
    /// Создает список метаданных для заданного типа сущности
    /// </summary>
    /// <param name="entityType">Тип сущности</param>
    /// <returns>Список метаданных сущности</returns>
    private static EntityMetaInfo _createEntityMetaInfo(Type entityType)
    {
        // Проверяем наличие статического класса Meta
        var metaClass = entityType.GetNestedType("Meta", BindingFlags.Public | BindingFlags.Static);
        if (metaClass == null)
        {
            throw new ArgumentException($"Тип {entityType.Name} не содержит статический класс Meta");
        }

        // Получаем все публичные свойства типа IField из класса Meta
        var metaFields = metaClass.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => typeof(IField).IsAssignableFrom(f.FieldType))
            .ToDictionary(f => f.Name, f => f.GetValue(null));

        // Собираем информацию о полях сущности
        var fields = new List<FieldMetaInfo>();
        
        foreach (var fieldInfo in metaFields)
        {
            var fieldValue = fieldInfo.Value as IField;
            if (fieldValue == null) continue;

            // Получаем информацию о соответствующем поле в классе сущности
            PropertyInfo? propertyInfo = entityType.GetProperty(fieldInfo.Key);
            if (propertyInfo == null)
            {
                throw new FrameException(
                    $"Поле {fieldInfo.Key} не найдено в типе {entityType.Name}");
            }

            fields.Add(new FieldMetaInfo
            {
                DotNetField = propertyInfo,
                FrameField = fieldValue
            });
        }

        // Получаем человекочитаемое имя сущности из класса Meta
        var humanNameField = metaClass.GetField("HumanName", 
            BindingFlags.Public | BindingFlags.Static);
        if (humanNameField == null)
        {
            throw new FrameException($"Поле HumanName не найдено в классе Meta типа {entityType.Name}");
        }
        string humanName = humanNameField?.GetValue(null) as string ?? entityType.Name;
        if (humanName == "")
        {
            throw new FrameException($"Поле HumanName в классе Meta типа {entityType.Name} не заполнено");
        }

        return new EntityMetaInfo 
        {
            HumanName = humanName,
            DotNetType = entityType,
            Fields = fields
        };
    }
}
