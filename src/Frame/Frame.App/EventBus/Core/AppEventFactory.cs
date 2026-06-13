using Frame.App.EventBus.Events.Notify;
using Frame.App.EventBus.Events.Request;
using Frame.Domain.Entities.Core;
using Frame.App.IEntityRepositories;
using Frame.App.Scripting;
using Frame.Domain.Entities.Metadata;
using Frame.Shared;

namespace Frame.App.EventBus.Core
{
    /// <summary>
    /// Динамически формируемая фабрика, создающая по запросу строго типизированные Notification / Requests.
    /// Заполняется на старте приложения после сканирования всех сборок в поисках требуемых типов.
    /// По сути - это карта анонимных методов по созданию объектов Notification / Requests.
    /// Обработчики этих событий - это совсем другая история, пишется в другом месте.
    /// При добавлении или удалении в runtime в <see cref="IScriptCore"/> скриптовых методов <see cref="IEntityScriptMethod"/>, 
    /// динамически добавляется или удаляется соответствующая запись в карте <see cref="_beforeSaveMethods"/>
    /// </summary>
    public class AppEventFactory : IAppEventFactory
    {
        private readonly Dictionary<Type, Func<BaseEntity, 
                                    IObjectStorage, EntityStorageState, 
                                    IEntityBeforeSaveRequest>> _beforeSaveMethods = [];
        private readonly Dictionary<Type, Func<BaseEntity, 
                                    IObjectStorage, EntityStorageState, 
                                    IEntityAfterSaveNotification>> _afterSaveMethods = [];

        /// <summary>
        /// Регистрация события <see cref="EntityBeforeSaveRequest{TEntity}"/> 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <exception cref="Exception"></exception>
        public void RegisterEntityBeforeSaveRequest<TEntity>() where TEntity : BaseEntity
        {
            var entityType = EntityMetadata.GetEntityType<TEntity>();
            if (!_beforeSaveMethods.ContainsKey(entityType))
            {
                _beforeSaveMethods[entityType] = (entity, storage, entityState) => 
                    new EntityBeforeSaveRequest<TEntity>((TEntity)entity, storage, entityState);
            }
            else
            {
                throw new FrameException($"Для типа {entityType.Name} уже зарегистрирован EntityBeforeSaveRequest.");
            }
        }

        /// <summary>
        /// Регистрация события <see cref="EntityBeforeSaveScriptRequest{TEntity}"/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <exception cref="Exception"></exception>
        public void RegisterEntityBeforeSaveScriptRequest<TEntity>() where TEntity : BaseEntity
        {
            var entityType = EntityMetadata.GetEntityType<TEntity>();
            if (!_beforeSaveMethods.ContainsKey(entityType))
            {
                _beforeSaveMethods[entityType] = (entity, storage, entityState) => 
                    new EntityBeforeSaveScriptRequest<TEntity>((TEntity)entity, storage, entityState);
            }
            else
            {
                throw new FrameException($"Для типа {entityType.Name} уже зарегистрирован EntityBeforeSaveRequest.");
            }
        }

        /// <summary>
        /// Регистрация события <see cref="EntityAfterSaveNotification{TEntity}"/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <exception cref="Exception"></exception>
        public void RegisterEntityAfterSaveNotification<TEntity>() where TEntity : BaseEntity
        {
            var entityType = EntityMetadata.GetEntityType<TEntity>();
            if (!_afterSaveMethods.ContainsKey(entityType))
            {
                _afterSaveMethods[entityType] = (entity, storage, entityState) => 
                    new EntityAfterSaveNotification<TEntity>((TEntity)entity, storage, entityState);
            }
        }

        /// <summary>
        /// Создания события-запроса OnBeforeSave для заданного типа. 
        /// Может быть зарегистрировано как <see cref="EntityBeforeSaveRequest{TEntity}"/> или <see cref="EntityBeforeSaveScriptRequest{TEntity}"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="storage"></param>
        /// <param name="entityStorageState"></param>
        /// <returns></returns>
        public IEntityBeforeSaveRequest? CreateBeforeSaveRequest(BaseEntity entity, 
                                                                 IObjectStorage storage, 
                                                                 EntityStorageState entityStorageState)
        {
            var entityType = EntityMetadata.GetEntityType(entity);
            if (_beforeSaveMethods.TryGetValue(entityType, out var factory))
            {
                return factory(entity, storage, entityStorageState);
            }
            return null;
        }

        public IEntityAfterSaveNotification? CreateAfterSaveNotification(BaseEntity entity, 
                                                                         IObjectStorage storage, 
                                                                         EntityStorageState entityStorageState)
        {
            var entityType = EntityMetadata.GetEntityType(entity);
            if (_afterSaveMethods.TryGetValue(entityType, out var factory))
            {
                return factory(entity, storage, entityStorageState);
            }
            else
            {
                return null;
            }
        }
    }
}
