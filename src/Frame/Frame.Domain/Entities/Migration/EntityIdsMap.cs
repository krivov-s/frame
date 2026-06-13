using Frame.Domain.Entities.Core;

namespace Frame.Domain.Entities.Migration;

public class EntityIdsMap: BaseEntity, IBaseGenericEntity<EntityIdsMap>
{
    public override string Description => $"";

    /// <summary>
    /// Полное наименование типа (с именем сборки)
    /// </summary>
    public string FullTypeName { get; set; } = "";
    
    /// <summary>
    /// Идентификатор объекта, поступившего извне для сохранения в БД
    /// </summary>
    public int IdSrc { get; set; }
    
    /// <summary>
    /// Ключ объекта (тип + Id),  поступившего извне для сохранения в БД
    /// </summary>
    public string KeySrc { get; set; } = "";
    
    /// <summary>
    /// Идентификатор объекта, реально сохраненного в БД
    /// </summary>
    public int IdDst { get; set; }

    public List<Field<EntityIdsMap>> GetFields() { return Meta.Fields; }

    public static class Meta
    {
        public static readonly string HumanName = "Связь старый-новый Id";

        public static readonly Field<EntityIdsMap> IdSrc = new()
        {
            Name = nameof(IdSrc), 
            IntGet = x => x.IdSrc, 
            IntSet = (x, val) => x.IdSrc = val ?? 0,
            Required = true, 
            HumanName = "Id объекта - источника"
        };
        public static readonly Field<EntityIdsMap> KeySrc = new() { 
            Name = nameof(KeySrc), 
            StringGet = x => x.KeySrc, 
            StringSet = (x, val) => x.KeySrc = val ?? "", 
            Required = true, 
            MaxLength = 255, 
            HumanName = "Ключ объекта - источника" };
        public static readonly Field<EntityIdsMap> FullTypeName = new() { 
            Name = nameof(FullTypeName), 
            StringGet = x => x.FullTypeName, 
            StringSet = (x, val) => x.FullTypeName = val ?? "", 
            Required = true, 
            MaxLength = 255, 
            HumanName = "Тип объекта - приемника" };
        public static readonly Field<EntityIdsMap> IdDst = new()
        {
            Name = nameof(IdDst), 
            IntGet = x => x.IdDst, 
            IntSet = (x, val) => x.IdDst = val ?? 0,
            Required = true, 
            HumanName = "Id объекта - приемника"
        };
        
        public static readonly List<Field<EntityIdsMap>> Fields = [IdSrc, FullTypeName, IdDst];
        
    }
    
    public static string SubstituteRefObjects<TEntity>(TEntity entity, Dictionary<string, EntityIdsMap> dictEntityIds) 
        where TEntity: BaseEntity, IBaseGenericEntity<TEntity>, new()
    {
        IBaseGenericEntity<TEntity> genericEntity = entity;

        foreach (var field in genericEntity.GetFields())
        {
            if (field.GetType().Name.StartsWith("RefField"))
            {
                if (field.RefIdGet == null)
                {
                    return $"У типа {typeof(TEntity).Name} у поля {field.HumanName} не определен RefIdGetter!";
                }
                if (field.RefIdSet == null)
                {
                    return $"У типа {typeof(TEntity).Name} у поля {field.HumanName} не определен RefIdSetter!";
                }

                if (field.RefEntityType == null)
                {
                    return $"У типа {typeof(TEntity).Name} у поля {field.HumanName} не определен RefEntityType!";
                }
                var funcGet = field.RefIdGet.Compile();
                int? id = funcGet(entity);
                if (id != null && id != 0)
                {
                    // Ищем данный Id в карте недавно импортированных объектов. Если находим - заменяем.
                    var refObject = Activator.CreateInstance(field.RefEntityType);
                    if (refObject == null)
                    {
                        return $"Не удалось создать Reference объект {field.RefEntityType.Name} для поля {field.HumanName} " +
                               $"у типа {typeof(TEntity).Name}!";
                    }
                    BaseEntity? newEntity = refObject as BaseEntity;
                    if (newEntity == null)
                    {
                        return $"Reference объект {field.RefEntityType.Name} для поля {field.HumanName} у типа {typeof(TEntity).Name} " +
                               $"не является BaseEntity!!! Такого вообще не должно было быть!!!";
                    }

                    newEntity.Id = id.Value;
                    string keyToFind = newEntity.Key;
                    bool bFind = dictEntityIds.TryGetValue(keyToFind, out var idsMap);
                    if (bFind && idsMap != null)
                    {
                        // Устанавливаем новое значение
                        field.RefIdSet(entity, idsMap.IdDst);
                    }
                }
            }
        }
        return "";
    }
}