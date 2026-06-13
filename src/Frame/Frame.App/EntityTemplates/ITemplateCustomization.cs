using Frame.Domain.Entities.Core;

namespace Frame.App.EntityTemplates;

public interface ITemplateCustomization<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>
    /// Настройка контекста перед началом сериализации. 
    /// </summary>
    /// <param name="context"></param>
    void SetUpContext(TemplateSerializationContext context);
    
    /// <summary>
    /// Метод вызывается после заполнения <see cref="snapshot"/> данными из объекта <see cref="entity"/>
    /// </summary>
    /// <param name="entity">Сериализуемый объект</param>
    /// <param name="snapshot">Снимок объекта</param>
    /// <param name="context">Контекст сериализации</param>
    void CustomizeSerialization(
        TEntity entity,
        Dictionary<string, object?> snapshot,
        TemplateSerializationContext? context = null);

    /// <summary>
    /// Метод вызывается после воссоздания объекта <see cref="entity"/> из данных, сохраненных в <see cref="rawSnapshot"/>
    /// </summary>
    /// <param name="entity">Сериализуемый объект</param>
    /// <param name="snapshot">Снимок объекта</param>
    /// <param name="context">Контекст сериализации</param>
    Task CustomizeDeserializationAsync(
        TEntity entity,
        TemplateSerializationContext context,
        Dictionary<string, object?> rawSnapshot);
}