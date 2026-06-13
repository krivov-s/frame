using System.Linq.Expressions;
using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Metadata;
using Frame.Shared;

namespace Frame.App.EntityTemplates;

public sealed class TemplateSerializationContext
{
    public IObjectStorage? ObjectStorage { get; set; }

    /// <summary>
    /// Свойства, которые должны сериализовываться как вложенные сущности
    /// Key: тип сущности-владельца
    /// Value: имена свойств
    /// </summary>
    private readonly Dictionary<Type, HashSet<string>> _embeddedProperties = new();

    /// <summary>
    /// Свойства, которые не должны сериализовываться
    /// Key: тип сущности-владельца
    /// Value: имена свойств
    /// </summary>
    private readonly Dictionary<Type, HashSet<string>> _skippedProperties = new();
    
    public void Embed<TEntity>(Expression<Func<TEntity, object?>> property)
        where TEntity : BaseEntity
    {
        var propName = GetPropertyName(property);
        Embed(typeof(TEntity), propName);
    }

    public void Embed(Type ownerType, string propertyName)
    {
        if (!_embeddedProperties.TryGetValue(ownerType, out var props))
        {
            props = new HashSet<string>();
            _embeddedProperties[ownerType] = props;
        }

        props.Add(propertyName);
    }

    public void Skip<TEntity>(Expression<Func<TEntity, object?>> property)
        where TEntity : BaseEntity
    {
        var propName = GetPropertyName(property);
        Skip(typeof(TEntity), propName);
    }

    public void Skip(Type ownerType, string propertyName)
    {
        if (!_skippedProperties.TryGetValue(ownerType, out var props))
        {
            props = new HashSet<string>();
            _skippedProperties[ownerType] = props;
        }

        propertyName = propertyName.Trim();
        props.Add(propertyName);

        // Проверяем, если свойство указывает на BaseEntity - то добавляем в skip-список и его идентификтор 
        Result<bool> resIsReference = EntityMetadata.IsEntityReference(ownerType, propertyName);
        if (resIsReference is { IsErrorOrNull: false, Value: true })
        {
            string propertyNameId = propertyName + "Id";
            props.Add(propertyNameId);
        }
    }

    
    public bool ShouldEmbed(Type ownerType, string propertyName)
        => _embeddedProperties.TryGetValue(ownerType, out var props)
           && props.Contains(propertyName);

    public bool ShouldSkip(Type ownerType, string propertyName)
        => _skippedProperties.TryGetValue(ownerType, out var props)
           && props.Contains(propertyName);

    private static string GetPropertyName<TEntity>(
        Expression<Func<TEntity, object?>> expr)
    {
        return expr.Body switch
        {
            MemberExpression m => m.Member.Name,
            UnaryExpression u when u.Operand is MemberExpression m => m.Member.Name,
            _ => throw new InvalidOperationException("Некорректное выражение свойства")
        };
    }
}
