using System.Collections;
using System.Reflection;
using System.Text.Json;
using Frame.App.IEntityRepositories;
using Frame.App.Security;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Entities.Metadata;
using Frame.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Frame.App.EntityTemplates;

public class EntityTemplateService(IServiceProvider serviceProvider, IObjectStorageProvider storageProvider, 
    ILogger<EntityTemplateService> logger, IGetCurrentUserService getCurrentUserService) : IEntityTemplateService
{
    private IObjectStorage _objectStorage = null!;
    private User _currentUser = null!;
    
    public async Task<Result> SaveAsTemplateAsync<TEntity>(TEntity entity, string templateName, 
        TemplateSerializationContext? context = null) where TEntity : BaseEntity
    {
        Result resInit = await _initService();
        if(resInit.IsError) return resInit;

        context ??= new()
        {
            ObjectStorage = _objectStorage
        };

        if (context.ObjectStorage == null)
        {
            string err = $"{nameof(SaveAsTemplateAsync)}: отсутствует ObjectStorage";
            logger.LogError("{Err}", err);
            return Result.Error(err);
        }
        
        Result<string> resJson = await SerializeAsync(entity, context);
        if(resJson.IsErrorOrNull) return Result.Error(resJson.ErrorResult);

        string entityType = EntityMetadata.BiuldExtTypeName(entity.GetType());
        
        TemplateEntity templateEntity = new()
        {
            Name = templateName,
            EntityTypeName = entityType,
            UserOwnerId = _currentUser.Id,
            JsonData = resJson.Value!
        };
        
        Result resAdd = context.ObjectStorage.Add(templateEntity);
        if(resAdd.IsError) return resAdd;

        Result resSave = await context.ObjectStorage.SaveChangesAsync();
        return resSave;
    }

    public async Task<Result<List<TemplateEntity>>> GetTemplateListAsync<TEntity>()
    {
        Result resInit = await _initService();
        if(resInit.IsError) return Result<List<TemplateEntity>>.Error(resInit.ErrorResult);
        
        string entityType = EntityMetadata.BiuldExtTypeName(typeof(TEntity));
        
        Result<List<TemplateEntity>> resList = await _objectStorage.GetListAsync<TemplateEntity>(spec => 
            spec.Where(x => x.EntityTypeName == entityType && x.UserOwnerId == _currentUser.Id));

        return resList;
    }

    public async Task<Result<string>> SerializeAsync<TEntity>(TEntity entity, TemplateSerializationContext? context = null)
        where TEntity : BaseEntity
    {
        try
        {
            // 3. Строим snapshot (проекцию)
            Result<Dictionary<string, object?>> resSnapshot = await _buildSnapshot(entity, context);
            if(resSnapshot.IsErrorOrNull) return Result<string>.Error(resSnapshot.ErrorResult);
            var snapshot = resSnapshot.Value!;

            // 5. JSON
            string json = JsonSerializer.Serialize(
                snapshot,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            
            return Result<string>.Success(json);
        }
        catch (Exception ex)
        {
            string err = $"Ошибка при сериализации объекта {entity}: {ex.Message}";
            logger.LogError(ex, "{Err}", err);
            return Result<string>.Error(err);
        }
    }

    public async Task<Result<TEntity>> DeserializeAsync<TEntity>(TemplateEntity template,
        TemplateSerializationContext context) where TEntity : BaseEntity, new()
    {
        string typeName = EntityMetadata.BiuldExtTypeName(typeof(TEntity));
        if (typeName != template.EntityTypeName)
        {
            string err = $"Имя типа, сохраненное в шаблоне ({template.EntityTypeName}), " +
                         $"не совпадает с типом шаблонной операции {typeof(TEntity).Name})";
            logger.LogError("{Err}", err);
            return Result<TEntity>.Error(err);
        }
        Result<TEntity> result = await DeserializeAsync<TEntity>(template.JsonData, context);
        return result;
    }


    public async Task<Result<TEntity>> DeserializeAsync<TEntity>(string json, TemplateSerializationContext context)
        where TEntity : BaseEntity, new()
    {
        if (string.IsNullOrEmpty(json)) return Result<TEntity>.Error("Передан пустой json, создание объекта невозможно");
        
        try
        {
            // 1. Snapshot из JSON
            var snapshot = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                           ?? throw new InvalidOperationException("Для восстановления из шаблона передан некорректный json");

            // 2. Новый объект
            var entity = new TEntity();

            // 3. Заполнение свойств
            await _fillEntityFromJson(context, snapshot, entity);

            // 4. Кастомизация после восстановления
            var customization = _getCustomization<TEntity>();
            if (customization != null)
            {
                var rawSnapshot = snapshot.ToDictionary(
                    x => x.Key,
                    x => (object?)x.Value);

                await customization.CustomizeDeserializationAsync(
                    entity,
                    context,
                    rawSnapshot);
            }

            return Result<TEntity>.Success(entity);

        }
        catch (Exception ex)
        {
            string err = $"Ошибка при десериализации объекта {typeof(TEntity).Name}: {ex.Message}";
            logger.LogError(ex, "{Err}", err);
            return Result<TEntity>.Error(err);
        }
    }

    private async Task _fillEntityFromJson<TEntity>(TemplateSerializationContext context, Dictionary<string, JsonElement> snapshot,
        TEntity entity) where TEntity : BaseEntity, new()
    {
        Type realEntityType = entity.GetType();
        PropertyInfo[] properties = realEntityType.GetProperties();
        
        foreach (var prop in properties)
        {
            if (!prop.CanWrite)
                continue;

            if (!snapshot.TryGetValue(prop.Name, out var element))
                continue;

            // Навигация → загрузка по Id
            if (typeof(BaseEntity).IsAssignableFrom(prop.PropertyType) && element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("$entity", out var embedded))
                {
                    await _deserializeEmbeddedEntity(
                        context, prop, embedded, entity);
                }
                else
                {
                    await _readReferenceEntityFromStorage(
                        context, prop, element, entity);
                }
            }
            else
            {
                var value = element.Deserialize(prop.PropertyType);
                prop.SetValue(entity, value);            
            }
        }
    }

    // =========================
    // Helpers
    // =========================
    private async Task<Result<Dictionary<string, object?>>> _buildSnapshot<TEntity>(
        TEntity entity,
        TemplateSerializationContext? context)
        where TEntity : BaseEntity
    {
        context ??= new();

        // ----------------------------------------------------
        // 1. Запрашиваем кастомизацию
        // ----------------------------------------------------
        ITemplateCustomization<TEntity>? customization = _getCustomization<TEntity>();
            
        // ----------------------------------------------------
        // 2. Донастройка контекста (при наличии кастомизации)
        // ----------------------------------------------------
        customization?.SetUpContext(context);
        
        var snapshot = new Dictionary<string, object?>();
        Type realEntityType = entity.GetType();
        PropertyInfo[] properties = realEntityType.GetProperties();

        // ----------------------------------------------------
        // 3. Формирование коллекции со значениями свойств
        // ----------------------------------------------------
        try
        {
            // Навигации, которые сериализуются как embedded
            var embeddedNavigations = new HashSet<string>();

            // 3.1. Первый проход — сбор данных
            foreach (var prop in properties)
            {
                if (!prop.CanRead || !prop.CanWrite)
                    continue;

                if (context?.ShouldSkip(typeof(TEntity), prop.Name) == true)
                    continue;
                
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType)
                    && prop.PropertyType != typeof(string))
                    continue;

                var value = prop.GetValue(entity);
                var related = value as BaseEntity;

                // Значение может быть null потому что оно не было считано, но если это свойство в списке 
                // embedded, то его обязательно необходимо дочитать, если оно null
                if (context?.ShouldEmbed(typeof(TEntity), prop.Name) == true)
                {
                    if (related is null)
                    {
                        var fkProp = _findForeignKeyProperty(typeof(TEntity), prop);
                        if (fkProp != null && fkProp.CanWrite)
                        {
                            object? objectId = fkProp.GetValue(entity);
                            if (objectId is not null)
                            {
                                int id = Convert.ToInt32(objectId);

                                Result<BaseEntity> resRelated =
                                    await _readObjectFromStorage(context, prop.PropertyType, id);
                                if (resRelated.IsErrorOrNull)
                                {
                                    return Result<Dictionary<string, object?>>.Error(resRelated.ErrorResult);
                                }
                                related = resRelated.Value!;
                            }
                        }
                    }

                    if (related is not null)
                    {
                        Result<Dictionary<string, object?>> resEntity = 
                            await _executeBuildSnapshotViaReflection(related, context);
                        if (resEntity.IsErrorOrNull)return resEntity;
                        
                        embeddedNavigations.Add(prop.Name);
                        snapshot[prop.Name] = new Dictionary<string, object?>
                        {
                            ["$entity"] = new EntitySnapshot
                            {
                                EntityType = EntityMetadata.BiuldExtTypeName(related.GetType()),
                                Data = resEntity.Value!
                            }
                        };
                    }
                }
                else
                {
                    if (related is not null)
                    {
                        snapshot[prop.Name] = new EntityReference { Id = related.Id };
                    }
                    else
                    {
                        snapshot[prop.Name] = value;
                    }
                }
            }

            // 3.2. Второй проход — зануление FK
            foreach (var navName in embeddedNavigations)
            {
                var navProp = realEntityType.GetProperty(navName);
                if (navProp == null)
                    continue;

                var fkProp = _findForeignKeyProperty(typeof(TEntity), navProp);
                if (fkProp != null && fkProp.CanWrite)
                {
                    snapshot[fkProp.Name] = 0;
                }
            }

            // 3.3. Id текущей сущности всегда 0. Даты создания/изменения тоже обнуляем.
            snapshot[nameof(BaseEntity.Id)] = 0;
            snapshot.Remove(nameof(BaseEntity.DateCreate));
            snapshot.Remove(nameof(BaseEntity.DateModify));
            
            // ----------------------------------------------------
            // 4. Корректировка snapshot при наличии кастомизации
            // ----------------------------------------------------
            customization?.CustomizeSerialization(entity, snapshot, context);
            
            return Result<Dictionary<string, object?>>.Success(snapshot);
        }
        catch (Exception ex)
        {
            string err = $"Ошибка построения 'снимка' объекта для сериализации: {ex.Message}";
            logger.LogError(ex, "{Err}", err);
            return Result<Dictionary<string, object?>>.Error(err);
        }
    }

    private async Task<Result> _readReferenceEntityFromStorage<TEntity>(TemplateSerializationContext context,
        PropertyInfo prop, JsonElement element, TEntity entity) where TEntity : BaseEntity, new()
    {
        if(element.ValueKind == JsonValueKind.Null) return Result.Success;
                    
        int refId = element.GetProperty(nameof(EntityReference.Id)).GetInt32();
        if(refId == 0) return Result.Success;
        
        Result<BaseEntity> resEntity = await _readObjectFromStorage(context, prop.PropertyType, refId);
        if (resEntity.IsErrorOrNull) return Result.Error(resEntity.ErrorResult);

        prop.SetValue(entity, resEntity.Value!);

        return Result.Success;
    }

    private async Task<Result<BaseEntity>> _readObjectFromStorage(
        TemplateSerializationContext context, Type entityType, int entityId)
    {
        var method = typeof(IObjectStorage)
            .GetMethods()
            .Single(m =>
                m is { Name: nameof(IObjectStorage.GetObjAsync), IsGenericMethodDefinition: true } 
                && m.GetParameters().Length >= 1 
                && m.GetParameters()[0].ParameterType == typeof(int));

        if (method == null)
        {
            string err = $"Не удалось получить метод {nameof(IObjectStorage.GetObjAsync)} через Reflection";
            logger.LogError("{Err}", err);
            return Result<BaseEntity>.Error(err);
        }

        var genericMethod = method.MakeGenericMethod(entityType);                    

        var task = (Task)genericMethod.Invoke(
            context.ObjectStorage,
            new object[] { entityId, null, "" })!;

        await task.ConfigureAwait(false);

        var result = task
            .GetType()
            .GetProperty("Result")!
            .GetValue(task);

        if (result == null)
        {
            string err = "При чтении связанного объекта при десериализации вернулся null";
            logger.LogError("{Err}", err);
            return Result<BaseEntity>.Error(err);
        }

        // Получаем тип Result<TEntity>
        var resultType = result.GetType();

        // Читаем его свойства
        var isErrorProp = resultType.GetProperty("IsError");
        var valueProp = resultType.GetProperty("Value");

        var isError = (bool)(isErrorProp?.GetValue(result) ?? true);
        var value = valueProp?.GetValue(result);

        if (isError || value == null)
        {
            // Получаем текст ошибки
            var errorProp = resultType.GetProperty("ErrorResult");
            var errorMessage = errorProp?.GetValue(result) as string ?? "";
            logger.LogError("{Err}", errorMessage);
            return Result<BaseEntity>.Error(errorMessage);
        }
        
        // Получаем объект из Value
        BaseEntity? entity = value as BaseEntity;
        if (entity == null)
        {
            string err = "Прочитанный из БД объект не является BaseEntity";
            logger.LogError("{Err}", err);
            return Result<BaseEntity>.Error(err);
        }

        return Result<BaseEntity>.Success(entity);
    }
    
    private ITemplateCustomization<TEntity>? _getCustomization<TEntity>()
        where TEntity : BaseEntity
    {
        return serviceProvider.GetService<ITemplateCustomization<TEntity>>();
    }
    
    private async Task<Result> _initService()
    {
        Result<IObjectStorage> resStorage = storageProvider.GetObjectStorage();
        if (resStorage.IsErrorOrNull) return Result.Error(resStorage.ErrorResult);
        _objectStorage = resStorage.Value!;

        Result<User> resUser = await getCurrentUserService.GetCurrentUserAsync();
        if (resUser.IsErrorOrNull) return Result.Error(resStorage.ErrorResult);
        _currentUser = resUser.Value!;
        return Result.Success;
    }
    
    private async Task _deserializeEmbeddedEntity<TEntity>(
        TemplateSerializationContext context,
        PropertyInfo prop,
        JsonElement embedded,
        TEntity owner)
        where TEntity : BaseEntity
    {
        var snapshot = embedded.Deserialize<EntitySnapshot>()
                       ?? throw new InvalidOperationException("Invalid embedded entity");

        var entityType = Type.GetType(snapshot.EntityType);
        if (entityType == null)
        {
            throw new FrameException($"Не удалось восстановить тип с именем {snapshot.EntityType}");
        }
        
        var related = (BaseEntity)Activator.CreateInstance(entityType)!;
        if (related == null)
        {
            throw new FrameException($"Тип с именем {entityType} не является {nameof(BaseEntity)}");
        }

        Type ownerType = owner.GetType();
        var json = JsonSerializer.Serialize(snapshot.Data);
        
        MethodInfo? method = typeof(EntityTemplateService)
            .GetMethods()
            .FirstOrDefault(m => m.Name == nameof(DeserializeAsync) 
                                 && m.IsGenericMethod
                                 && m.GetParameters().Length == 2
                                 && m.GetParameters()[0].ParameterType == typeof(string));
		        
        if (method != null)
        {
            var genericMethod = method.MakeGenericMethod(entityType);
            var task = (Task)genericMethod.Invoke(this, new object[] { json, context })!;
            await task;

            var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
            var value = result.GetType().GetProperty("Value")!.GetValue(result);

            prop.SetValue(owner, value);
            
            var fkProp = _findForeignKeyProperty(ownerType, prop);
            if (fkProp != null && fkProp.CanWrite)
            {
                fkProp.SetValue(owner, 0);
            }            
        }
    }
    
    private async Task<Result<Dictionary<string, object?>>> _executeBuildSnapshotViaReflection(BaseEntity entity, TemplateSerializationContext context)
    {
        var method = GetType().GetMethod(nameof(_buildSnapshot), BindingFlags.NonPublic | BindingFlags.Instance);
        var entityType = entity.GetType();

        if (method == null)
        {
            string err = $"Не удалось получить метод {nameof(_buildSnapshot)} через Reflection";
            logger.LogError("{Err}", err);
            return Result<Dictionary<string, object?>>.Error(err);
        }

        var genericMethod = method.MakeGenericMethod(entityType);                    

        var task = (Task)genericMethod.Invoke(
            this,
            new object[] { entity, context })!;

        await task.ConfigureAwait(false);

        var result = task
            .GetType()
            .GetProperty("Result")!
            .GetValue(task);

        if (result == null)
        {
            string err = $"При выполнении {nameof(_executeBuildSnapshotViaReflection)} произошла ошибка: вернулся нулевой Result";
            logger.LogError("{Err}", err);
            return Result<Dictionary<string, object?>>.Error(err);
        }

        Result<Dictionary<string, object>>? resultValue = result as Result<Dictionary<string, object>>;
        if (resultValue == null)
        {
            string err = $"Из метода вернулся результат, отличный от ожидаемого типа {nameof(Result<Dictionary<string, object>>)}";
            logger.LogError("{Err}", err);
            return Result<Dictionary<string, object?>>.Error(err);
        }

        return resultValue;
    }
    
    
    
    private static PropertyInfo? _findForeignKeyProperty(Type ownerType, PropertyInfo navigationProperty)
    {
        // Convention: <NavPropName>Id
        return ownerType.GetProperty(navigationProperty.Name + "Id");
    }
}
