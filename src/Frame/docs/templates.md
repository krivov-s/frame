# Шаблоны
В системе реализована возможность сохранения любого объекта в виде шаблона и создание нового объекта по этому шаблону

## Основные тезисы.
- Для работы с шаблонами в ядре системы реализован сервис IEntityTemplateService.
- В интерфейсе пользователя в EntitiesListDetail добавлены кнопки сохранения объекта в виде шаблона и создания объекта по шаблону
- Права на создание объекта по шаблону и сохранение в виде шаблона также настраиваются через систему безопасности (права на тип для роли)
- По-умолчанию в шаблон сохраняются только линейные атрибуты. Для сложных объектов необходимо дополнительно настраивать процедуру сохранения. 
  Это делается путем создания объекта, реализующего интерфейс ITemplateCustomization<TEntity>. 
  В реализации методов осуществляется настройка контекста сериализации (объекта TemplateSerializationContext), которому
  указываются свойства-ассоциации, которые должны считаться частью объекта и сериализоваться целиком, а также указываются
  свойства, которые не нужно сериализовать.
  Пример:
```csharp
public sealed class TestDocTemplateCustomization : ITemplateCustomization<TestDoc>
{
    public void SetUpContext(TemplateSerializationContext context)
    {
        context.Embed<TestDoc>(x => x.RefObject1);
        context.Embed<TestDoc>(x => x.RefObject2);
    }
    
    public void CustomizeSerialization(
        TestDoc entity,
        Dictionary<string, object?> snapshot,
        TemplateSerializationContext context)
    {
    }

    public Task CustomizeDeserializationAsync(
        TestDoc entity,
        TemplateSerializationContext context,
        Dictionary<string, object?> rawSnapshot)
    {
        return Task.CompletedTask;
    }
}
```