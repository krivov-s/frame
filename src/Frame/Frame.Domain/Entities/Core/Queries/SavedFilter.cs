using Frame.Domain.Entities.Core.Reports;
using Frame.Domain.Entities.Core.Scripting;
using Frame.Domain.Entities.Metadata;

namespace Frame.Domain.Entities.Core.Queries;

public class SavedFilter : BaseEntity, IBaseGenericEntity<SavedFilter>
{
    public override string Description => Name;

    /// <summary>
    /// Наименование сохраненного фильтра
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Описание сохраненного фильтра
    /// </summary>
    public string Descr { get; set; } = "";

    /// <summary>
    /// Полное наименование типа, к которому может быть применен фильр. ВНИМАНИЕ! При его установке автоматически установится (перезапишется) <see cref="EntityType"/>
    /// </summary>
    public string EntityTypeName
    {
        get => _entityTypeName;
        set
        {
            _entityTypeName = value;
            _entityType = null;
            _entityType = InitEntityType();
        }
    }
    private string _entityTypeName = "";

    /// <summary>
    /// Тип, по для которого строится фильр. ВНИМАНИЕ! Если тип не равен null - при его установке автоматически установится (перезапишется) <see cref="EntityTypeName"/>
    /// </summary>
    public Type? EntityType
    {
        get => InitEntityType();
        set
        {
            _entityType = value;
            if (_entityType != null)
            {
                _entityTypeName = EntityMetadata.BiuldExtTypeName(_entityType);
            }
        }
    }
    private Type? _entityType;

    public string EntityTypeHumanName => EntityMetadata.GetHumanName(EntityType);
    /// <summary>
    /// Фильтр в виде текстовой строки
    /// </summary>
    public string Where { get; set; } = "";

    /// <summary>
    /// Дополнительный тег, который может устанавливаться и использоваться системой
    /// для фильтрации и настройки систем безопасности
    /// </summary>
    public string SystemTag { get; set; } = string.Empty;

    /// <summary>
    /// Дополнительный тег, который может устанавливаться и использоваться пользователями
    /// для поиска и фильтрации документов
    /// </summary>
    public string UserTag { get; set; } = string.Empty;

    public string ParamListJson { get; set; } = "";

    public List<SavedFilterGroupLink> SavedFilterGroups { get; set; } = [];

    //todo сделать свойство через get set
    //public string SavedFilterGroupNames => SavedFilterGroups.Select(x => x.SavedFilterGroup?.Name).Aggregate((first, second) => $"{first}, {second}");
    public string? SavedFilterGroupNames
    {
        get
        {
            string[] groupNames = [.. SavedFilterGroups
                .Select(x => x.SavedFilterGroup != null ? x.SavedFilterGroup.Name : string.Empty)];
            if (groupNames == null || groupNames.Length == 0)
                return string.Empty;

            return groupNames.Aggregate((first, second) => $"{first}, {second}");
        }
    }

    public List<Field<SavedFilter>> GetFields() { return Meta.Fields; }


    public static class Meta
    {
        public static readonly string HumanName = "Сохраненный фильтр";
        public static readonly Field<SavedFilter> Name = new() { Name = nameof(Name), StringGet = x => x.Name, StringSet = (x, val) => x.Name = val ?? "", Required = true, MaxLength = 255, HumanName = "Наименование" };
        public static readonly Field<SavedFilter> Descr = new() { Name = nameof(Descr), StringGet = x => x.Descr, StringSet = (x, val) => x.Descr = val ?? "", Required = false, MaxLength = 2048, HumanName = "Описание" };
        public static readonly Field<SavedFilter> EntityTypeName = new() { Name = nameof(EntityTypeName), StringGet = x => x.EntityTypeName, StringSet = (x, val) => x.EntityTypeName = val ?? "", Required = true, MaxLength = 255, HumanName = "Тип объекта" };
        public static readonly Field<SavedFilter> Where = new() { Name = nameof(Where), StringGet = x => x.Where, StringSet = (x, val) => x.Where = val ?? "", Required = false, MaxLength = 2048, HumanName = "Условие отбора (фильтр)" };
        public static readonly Field<SavedFilter> SystemTag = new() { Name = nameof(SystemTag), StringGet = x => x.SystemTag, StringSet = (x, val) => x.SystemTag = val ?? "", Required = false, MaxLength = 512, HumanName = "Системный тег" };
        public static readonly Field<SavedFilter> UserTag = new() { Name = nameof(UserTag), StringGet = x => x.UserTag, StringSet = (x, val) => x.UserTag = val ?? "", Required = false, MaxLength = 512, HumanName = "Пользовательский тег" };
        public static readonly Field<SavedFilter> ParamListJson = new() { Name = nameof(ParamListJson), StringGet = x => x.ParamListJson, StringSet = (x, val) => x.ParamListJson = val ?? "", Required = false, MaxLength = 2048, HumanName = "Список параметров" };

        public static readonly Field<SavedFilter> EntityType = new()
        {
            Name = nameof(EntityType),
            StringGet = x => x.EntityType != null ? x.EntityType.Name : "",
            Required = false,
            HumanName = "Тип объекта (Type)",
            IsPersistent = false
        };
        public static readonly Field<SavedFilter> EntityTypeHumanName = new()
        {
            Name = nameof(EntityTypeHumanName),
            StringGet = x => x.EntityTypeHumanName,
            Required = false,
            HumanName = "Имя типа объекта",
            IsPersistent = false
        };

        public static readonly Field<SavedFilter> SavedFilterGroupNames = new() { Name = nameof(SavedFilterGroupNames), StringGet = x => x.SavedFilterGroupNames, Required = false, MaxLength = 200, HumanName = "Группы" };

        public static readonly List<Field<SavedFilter>> Fields = [Name, Descr, EntityTypeName, Where, SystemTag, UserTag, ParamListJson, EntityType, EntityTypeHumanName];
    }

    /// <summary>
    /// Получение объекта Type по имени типа.
    /// Если имя типа некорректно, если тип не удалось создать - будет Exception.
    /// <see cref="EntityMetadata"/> не используется, поскольку там только короткие имена типов,
    /// а сюда могут и будут приходить полные имена, с namespace и assembly 
    /// </summary>
    /// <returns></returns>
    private Type? InitEntityType()
    {
        if (_entityType != null)
        {
            return _entityType;
        }

        if (EntityTypeName == "")
        {
            _entityType = null;
            return _entityType;
        }

        _entityType = Type.GetType(EntityTypeName);

        return _entityType;
    }
}