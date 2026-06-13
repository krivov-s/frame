using Frame.Domain.Entities.Core;
using Frame.Shared;

namespace Frame.App.EntityTemplates;

public interface IEntityTemplateService
{
    public Task<Result> SaveAsTemplateAsync<TEntity>(TEntity entity, string templateName, 
        TemplateSerializationContext? context = null) where TEntity : BaseEntity;
    
    public Task<Result<string>> SerializeAsync<TEntity>(TEntity entity, TemplateSerializationContext? context = null) 
        where TEntity : BaseEntity;

    public Task<Result<TEntity>> DeserializeAsync<TEntity>(TemplateEntity template, TemplateSerializationContext context) 
        where TEntity : BaseEntity, new();

    public Task<Result<TEntity>> DeserializeAsync<TEntity>(string json, TemplateSerializationContext context) 
        where TEntity : BaseEntity, new();

    /// <summary>
    /// Получение списка шаблонов для заданного типа для текущего пользователя
    /// </summary>
    /// <typeparam name="TEntity">Тип, для которого читаются шаблоны</typeparam>
    /// <returns></returns>
    public Task<Result<List<TemplateEntity>>> GetTemplateListAsync<TEntity>();

}