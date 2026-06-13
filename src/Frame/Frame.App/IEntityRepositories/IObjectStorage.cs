using System.Linq.Expressions;
using Frame.Domain.Entities.Core;
using Frame.Shared;
using Frame.App.EventBus.Core;
using Frame.App.EventBus.Events.Notify;
using Frame.App.EventBus.Events.Request;
using Frame.Domain.Params;
using Frame.Domain.QuerySpec;
using Frame.Domain.Entities.Core.Queries;

namespace Frame.App.IEntityRepositories
{
    /// <summary>
    /// Интерфейс объектного хранилище. 
    /// <para>
    /// В случае работы с EF.Core - это обертка вокруг DbContext. 
    /// Основное назначение - реализация корректного вызова методов моделей OnBefore/After/Save/Delete/Async
    /// </para>
    /// </summary>
    public interface IObjectStorage
    {
        /// <summary>
        /// Получение нового репозитория заданного типа.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<IBaseRepository<TEntity>> GetBaseRepository<TEntity>() where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Упрощенный метод, который внутри вызывает <see cref="GetBaseRepository{TEntity}()"/>, в случае ошибки 
        /// пишет в лог сообщение <paramref name="strErrorMessage"/> и возвращает null.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="strErrorMessage"></param>
        /// <returns></returns>
        public IBaseRepository<TEntity>? GetBaseRepository<TEntity>(string strErrorMessage) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Получение нового запроса к источнику данных. 
        /// В случае работы с EF.Core - это результат вызова <![CDATA[DbContext.Set<TEntity>() или AppDbContext.GetSet<TEntity>()]]>
        /// <para>
        /// Также при работе с EF.Core по-умолчанию перед возвратом будет вызван .AsSplitQuery(), для более эффективного построения отчетов.
        /// Это определяется текущей реализацией EFObjectStorage и может быть изменено в других реализациях.
        /// </para>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="filter">Фильтр, который накладывается на источник данных (recordset)</param>
        /// <param name="strErrPrefix">Текст, который будет префиксом при формировании строки ошибки</param>
        /// <returns></returns>
        [Obsolete("Метод устарел! Необходимо использовать GetListAsync с настройкой через QuerySpecification.")]
        public Result<IQueryable<TEntity>> GetQuery<TEntity>(
            string strErrPrefix = "", 
            Expression<Func<TEntity, bool>>? filter = null) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Упрощенный метод, возвращающий готовый IQueryable без Result. 
        /// Внутри вызывает <see cref="GetQuery{TEntity}"/>. 
        /// В случае ошибки пишет в лог сообщение с префиксом <paramref name="strErrPrefix"/> и возвращает null.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="filter">Фильтр, который накладывается на источник данных (recordset)</param>
        /// <param name="strErrPrefix">Текст, который будет префиксом при формировании строки ошибки</param>
        /// <returns></returns>
        [Obsolete("Метод устарел! Необходимо использовать GetListAsync с настройкой через QuerySpecification.")]
        public IQueryable<TEntity>? GetQueryNR<TEntity>(
            string strErrPrefix = "", 
            Expression<Func<TEntity, bool>>? filter = null) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Считывание из БД списка объектов данного типа и возврат их в виде List{BaseEntity} 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Task<Result<List<BaseEntity>>> GetBaseEntityListAsync<TEntity>(
            Func<IQuerySpecification<TEntity>,IQuerySpecification<TEntity>>? querySpec = null, 
            string strErrPrefix = "") where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Считывание из БД списка объектов данного типа и возврат их в виде List{dynamic} 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="querySpec">Функция - конфигуратор спецификации запроса</param>
        /// <param name="strErrPrefix">Текст, который будет префиксом при формировании строки ошибки</param>
        /// <returns></returns>
        public Task<Result<List<dynamic>>> GetDynamicListAsync<TEntity>(
            Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? querySpec = null, 
            string strErrPrefix = "") where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Считывание из БД списка объектов на основе <see cref="SavedQuery"/> и возврат их в виде List{BaseEntity}
        /// </summary>
        /// <param name="savedQuery">Сохраненный запрос</param>
        /// <param name="paramList">Предзаполненный список параметров. Если не задан - используется тот, который внутри SavedQuery</param>
        /// <param name="strErrPrefix">Текст, который будет префиксом при формировании строки ошибки</param>
        /// <returns></returns>
        public Task<Result<List<dynamic>>> GetDynamicListAsync(
            SavedQuery savedQuery, 
            ParamList? paramList = null, 
            string strErrPrefix = "");

        /// <summary>
        /// Считывание из БД списка объектов данного типа и возврат их в виде List{dynamic} 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="filter">Фильтр, который накладывается на источник данных (recordset)</param>
        /// <param name="strErrPrefix">Текст, который будет префиксом при формировании строки ошибки</param>
        /// <returns></returns>
        public Task<Result<List<dynamic>>> GetDynamicListAsync<TEntity>(
            Expression<Func<TEntity, bool>>? filter = null, 
            string strErrPrefix = "") where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Считывание из БД списка объектов данного типа и возврат их в виде List{TEntity} 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="querySpec">Секции Include, Where, OrderBy для применения к источнику данных</param>
        /// <param name="strErrPrefix">Текст, который будет префиксом при формировании строки ошибки</param>
        /// <returns></returns>
        public Task<Result<List<TEntity>>> GetListAsync<TEntity>(
            Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? querySpec = null, 
            string strErrPrefix = "") where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();
        
        /// <summary>
        /// Считывание из БД списка объектов данного типа и возврат их в виде List{TEntity} 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="strErrPrefix">Текст, который будет префиксом при формировании строки ошибки</param>
        /// <returns></returns>
        public Task<Result<List<TEntity>>> GetListAsync<TEntity>( 
            string strErrPrefix = "") where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Добавление нового экземпляра в хранилище без сохранения изменений в постоянной БД
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Result Add<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Обновление существующего экземпляра в хранилище без сохранения изменений в постоянной БД
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Result Update<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Удаление экземпляра из хранилища без сохранения изменений в постоянной БД
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Result Delete<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Удаление экземпляра из хранилища по Id без сохранения изменений в постоянной БД
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public Result Delete<TEntity>(int id) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();


        /// <summary>
        /// Групповое удаление списка экземпляра из хранилища без сохранения изменений в постоянной БД
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="list">Список объектов для удаления</param>
        /// <returns></returns>
        public Result Delete<TEntity>(List<TEntity> list) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Добавление нового экземпляра в хранилище с одновременным сохранением 
        /// <b>всех, в т.ч. сделанных ранее другими вызовами из всех репозиториев Add/Update/Delete</b> изменений в постоянную БД
        /// (будет вызван <see cref="SaveChangesAsync"/>)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<Result> AddAndSaveAsync<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Обновление существующего экземпляра в хранилище с одновременным сохранением 
        /// <strong>всех, в т.ч. сделанных ранее другими вызовами из всех репозиториев Add/Update/Delete</strong> изменений в постоянную БД
        /// (будет вызван <see cref="SaveChangesAsync"/>)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<Result> UpdateAndSaveAsync<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Удаление экземпляра в хранилище с одновременным сохранением 
        /// <strong>всех, в т.ч. сделанных ранее другими вызовами из всех репозиториев Add/Update/Delete</strong> изменений в постоянную БД
        /// (будет вызван <see cref="SaveChangesAsync"/>)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<Result> DeleteAndSaveAsync<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Обновление из источника данных конкретного экземпляра TEntity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<Result> RefreshAsync<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Сохранение всех изменений в постоянное хранилище.
        /// В случае работы с EF.Core - в итоге будет вызов у контекста метода SaveChangesAsync().
        /// <para>
        /// При сохранении изменений должны быть сформированы и отправлены в <see cref="AppEventDispatcher"/> все события, связанных с сохранением и удалением объектов 
        /// ( <see cref="IEntityBeforeSaveRequest"/>, <see cref="IEntityAfterSaveNotification"/> )
        /// </para>
        /// <para>
        /// На уровне реализации необходимо запрещать рекурсивный вызов данного метода. Попытка вызвать метод рекурсивно может прилететь из пользовательского кода
        /// в реализации события <see cref="IEntityBeforeSaveRequest"/>. В этом случае реализация должна возвращать ошибку. Это важно, поскольку <see cref="IEntityBeforeSaveRequest"/>
        /// выполняются в одном потоке-последовательнсти вызовов, через <see cref="AppEventDispatcher"/> и <see cref="MediatR.IMediator" />, 
        /// в отличие от <see cref="IEntityAfterSaveNotification"/>, которые выполняются через очередь сообщений (возможно даже внешнюю).
        /// </para>
        /// <para>
        /// В рамках текущего проекта все эти требования реализованы в EFObjectStorage.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public Task<Result> SaveChangesAsync();

        /// <summary>
        /// Получение объекта из хранилища по его типу и Id.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="id">Id требуемого объекта</param>
        /// <param name="querySpec">Дополнительные настройки запроса (в данном случае значение имеют связи - Include)</param>
        /// <param name="strErrPrefix">Префикс к тексту ошибки для записи в лог при возникновении ошибки</param>
        /// <returns>
        /// Если объект с заданным Id не найден - вернется Result.Success, но с Value = null,
        /// при этом в лог сообщение не выводится. Если найдется несколько объектов с такими Id - вернется Result.Error.
        /// </returns>
        public Task<Result<TEntity>> GetObjAsync<TEntity>(int id, 
            Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? querySpec = null,
            string strErrPrefix = "") 
            where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Получение из хранилища объекта из списка по Id и в соответствии с заданной спецификацией.
        /// </summary>
        /// <typeparam name="TEntity">Тип объекта</typeparam>
        /// <param name="querySpec">Спецификация для загрузки (в т.ч. условие (фильтр) для поиска объекта в БД)</param>
        /// <returns>
        /// Если объект с заданным Id не найден - вернется Result.Success, но с Value = null,
        /// при этом в лог сообщение не выводится. Если найдется несколько объектов с такими Id - вернется Result.Error.
        /// </returns>
        public Task<Result<TEntity>> GetObjAsync<TEntity>(
            Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>> querySpec) 
            where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Получение из хранилища объекта из списка, соответствующего условию в <see cref="filter"/>
        /// Если в БД будет обнаружено более одного объекта - вернется ошибка.
        /// Если объект не будет обнаружен - вернется Result.Success(null)
        /// </summary>
        /// <typeparam name="TEntity">Тип объекта</typeparam>
        /// <param name="filter">Условие (фильтр) для поиска объекта в БД</param>
        /// <returns></returns>
        public Task<Result<TEntity>> GetObjByFilterAsync<TEntity>(Expression<Func<TEntity, bool>> filter) 
            where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new();

        /// <summary>
        /// Метод возвращает true, если в хранилище есть хотя бы одно изменение (Add/Modify/Delete)
        /// </summary>
        /// <returns></returns>
        public bool IsDirty { get; }
        
        /// <summary>
        /// Ручной запуск транзакции.
        /// </summary>
        /// <returns></returns>
        public Task<Result> BeginTransactionAsync();

        /// <summary>
        /// Ручное завершение транзакции.
        /// </summary>
        /// <returns></returns>
        public Task<Result> CommitTransactionAsync();

        /// <summary>
        /// Ручной откат транзакции.
        /// </summary>
        /// <returns></returns>
        public Task<Result> RollbackTransactionAsync();
        
        /// <summary>
        /// Возвращает true, если транзакция была запущена вручную методом <see cref="BeginTransactionAsync"/> 
        /// </summary>
        /// <returns></returns>
        public bool IsTransactionStarted();
        
    }
    
    /// <summary>
    /// Состояние объекта в хранилище данных (результат сохранения или удаления).
    /// Используется при обработке оповещений.
    /// </summary>
    public enum EntityStorageState
    {
        Undefined = 0,
        /// <summary>
        /// Объект был добавлен в хранилище
        /// </summary>
        Added,
        /// <summary>
        /// Объект был изменен в хранилище
        /// </summary>
        Modified,
        /// <summary>
        /// Объект был удален из хранилища
        /// </summary>
        Deleted
    };
}
