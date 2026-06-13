using Frame.Domain.Entities.Core.Reports;
using Frame.Domain.Entities.Metadata;

namespace Frame.Domain.Entities.Core.Queries;

public class SavedQuery : BaseEntity, IBaseGenericEntity<SavedQuery>
{
    public override string Description => Name;

    /// <summary>
    /// Наименование сохраненного запроса
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Описание сохраненного запроса
    /// </summary>
    public string Descr { get; set; } = "";

    /// <summary>
    /// Полное наименование типа, к которому построен запрос. ВНИМАНИЕ! При его установке автоматически установится (перезапишется) <see cref="EntityType"/>
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
    /// Тип, по которому построен запрос. ВНИМАНИЕ! Если тип не равен null - при его установке автоматически установится (перезапишется) <see cref="EntityTypeName"/>
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
    
    /// <summary>
    /// Список полей, разделенных запятыми
    /// </summary>
    public string Select { get; set; } = "";
    
    /// <summary>
    /// Фильтр в виде текстовой строки
    /// </summary>
    public string Where { get; set; } = "";
    
    /// <summary>
    /// Порядок сортировки
    /// </summary>
    public string OrderBy { get; set; } = "";

    /// <summary>
    /// Дополнительный тег к документу, который может устанавливаться и использоваться системой
    /// для фильтрации и настройки систем безопасности
    /// </summary>
    public string SystemTag { get; set; } = string.Empty;
        
    /// <summary>
    /// Дополнительный тег к документу, который может устанавливаться и использоваться пользователями
    /// для поиска и фильтрации документов
    /// </summary>
    public string UserTag { get; set; } = string.Empty;
    
    public string ParamListJson { get; set; } = "";

    public List<SavedQueryGroupLink> SavedQueryGroups { get; set; } = [];

    //public string SavedQueryNames => SavedQueryGroups.Select(x => x.SavedQueryGroup?.Name).Aggregate((first, second) => $"{first}, {second}");
    public string SavedQueryGroupNames
    {
        get
        {
            string[] groupNames = [.. SavedQueryGroups
                .Select(x => x.SavedQueryGroup != null ? x.SavedQueryGroup.Name : string.Empty)];
            if (groupNames == null || groupNames.Length == 0)
                return string.Empty;

            return groupNames.Aggregate((first, second) => $"{first}, {second}");
        }
    }


    public List<Field<SavedQuery>> GetFields() { return Meta.Fields; }
    
    
    public static class Meta
    {
        public static readonly string HumanName = "Сохраненный запрос";
        public static readonly Field<SavedQuery> Name = new() { Name = nameof(Name), StringGet = x => x.Name, StringSet = (x, val) => x.Name = val ?? "", Required = true, MaxLength = 255, HumanName = "Наименование" };
        public static readonly Field<SavedQuery> Descr = new() { Name = nameof(Descr), StringGet = x => x.Descr, StringSet = (x, val) => x.Descr = val ?? "", Required = false, MaxLength = 2048, HumanName = "Описание" };
        public static readonly Field<SavedQuery> EntityTypeName = new() { Name = nameof(EntityTypeName), StringGet = x => x.EntityTypeName, StringSet = (x, val) => x.EntityTypeName = val ?? "", Required = true, MaxLength = 255, HumanName = "Тип объекта" };
        public static readonly Field<SavedQuery> Select = new() { Name = nameof(Select), StringGet = x => x.Select, StringSet = (x, val) => x.Select = val ?? "", Required = false, MaxLength = 2048, HumanName = "Список полей" };
        public static readonly Field<SavedQuery> Where = new() { Name = nameof(Where), StringGet = x => x.Where, StringSet = (x, val) => x.Where = val ?? "", Required = false, MaxLength = 2048, HumanName = "Условие отбора (фильтр)" };
        public static readonly Field<SavedQuery> OrderBy = new() { Name = nameof(OrderBy), StringGet = x => x.OrderBy, StringSet = (x, val) => x.OrderBy = val ?? "", Required = false, MaxLength = 2048, HumanName = "Порядок сортировки" };
        public static readonly Field<SavedQuery> SystemTag = new() { Name = nameof(SystemTag), StringGet = x => x.SystemTag, StringSet = (x, val) => x.SystemTag = val ?? "", Required = false, MaxLength = 512, HumanName = "Системный тег" };
        public static readonly Field<SavedQuery> UserTag = new() { Name = nameof(UserTag), StringGet = x => x.UserTag, StringSet = (x, val) => x.UserTag = val ?? "", Required = false, MaxLength = 512, HumanName = "Пользовательский тег" };
        public static readonly Field<SavedQuery> ParamListJson = new() { Name = nameof(ParamListJson), StringGet = x => x.ParamListJson, StringSet = (x, val) => x.ParamListJson = val ?? "", Required = false, MaxLength = 2048, HumanName = "Список параметров" };
        public static readonly Field<SavedQuery> SavedQueryGroupNames = new()
        {
            Name = nameof(SavedQueryGroupNames),
            StringGet = x => x.SavedQueryGroupNames,
            Required = false,
            HumanName = "Группы",
            IsPersistent = false
        };

        public static readonly Field<SavedQuery> EntityType = new()
        {
            Name = nameof(EntityType), 
            StringGet = x => x.EntityType != null ? x.EntityType.Name : "", 
            Required = false, HumanName = "Тип объекта (Type)",
            IsPersistent = false
        };

        public static readonly List<Field<SavedQuery>> Fields = [Name, Descr, EntityTypeName, Select, Where, OrderBy, SystemTag, UserTag, ParamListJson, EntityType];
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