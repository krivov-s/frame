using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Entities.Metadata;

namespace Frame.Domain.Entities.Core;

/// <summary>
/// Шаблон объекта системы 
/// </summary>
public class TemplateEntity  : BaseEntity, IBaseGenericEntity<TemplateEntity>
{
    public override string Description => Name;

    public string Name { get; set; } = "";

    /// <summary>
    /// Полное наименование типа, которому приналлежит шаблон. ВНИМАНИЕ! При его установке автоматически установится (перезапишется) <see cref="EntityType"/>
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

    public string JsonData { get; set; } = "";
    
    public User? UserOwner { get; set; }
    public int UserOwnerId { get; set; }
    
    /// <summary>
    /// Тип, по для которого строится шаблон. ВНИМАНИЕ! Если тип не равен null - при его установке автоматически установится (перезапишется) <see cref="EntityTypeName"/>
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

    
    public List<Field<TemplateEntity>> GetFields() { return Meta.Fields; }

    public static class Meta
    {
        public static readonly string HumanName = "Шаблон";
        public static readonly Field<TemplateEntity> Name = new()
        {
            Name = nameof(Name), 
            StringGet = x => x.Name, 
            StringSet = (x, val) => x.Name = val ?? "", 
            Required = true, 
            MaxLength = 255, 
            HumanName = "Наименование"
        };
        public static readonly Field<TemplateEntity> EntityTypeName = new()
        {
            Name = nameof(EntityTypeName), 
            StringGet = x => x.EntityTypeName, 
            StringSet = (x, val) => x.EntityTypeName = val ?? "", 
            Required = true, 
            MaxLength = 255, 
            HumanName = "Тип объекта"
        };
        public static readonly Field<TemplateEntity> JsonData = new()
        {
            Name = nameof(JsonData), 
            StringGet = x => x.JsonData, 
            StringSet = (x, val) => x.JsonData = val ?? "", 
            Required = false, 
            HumanName = "Json data"
        };
        public static readonly RefField<TemplateEntity, User> UserOwner = new()
        {
            Name = nameof(UserOwner),
            RefGetter = x => x.UserOwner,
            RefIdGetter = x => x.UserOwnerId,
            RefSetter = (x, val) => x.UserOwner = val,
            RefIdSetter = (x, val) => x.UserOwnerId = val ?? 0,
            Required = true,
            HumanName = "Пользователь-владелец"
        };
        
        public static readonly Field<TemplateEntity> EntityTypeHumanName = new()
        {
            Name = nameof(EntityTypeHumanName), 
            StringGet = x => x.EntityTypeHumanName, 
            Required = false, 
            IsPersistent = false,
            HumanName = "'Человеческое' имя типа объекта"
        };
        
        
        public static readonly List<Field<TemplateEntity>> Fields = [Name, EntityTypeName, JsonData, UserOwner, EntityTypeHumanName];
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
