using Frame.Domain.Entities.Core;
using Frame.Shared;
using System.Linq.Expressions;
using Frame.Domain.QuerySpec;

namespace Frame.App.IEntityRepositories
{
    /// <summary>
    /// Репозиторий (таблица) объектов заданного типа. Выполняет валидацию объекта и CRUD-операции <strong>в режиме по умолчанию - без сохранения во внешниюю БД</strong>.
    /// <para>
    /// В случае работы с EF.Core - это CRUD операции с контекстом но без вызова метода контекста SaveChanges.
    /// </para>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IBaseRepository<TEntity> where TEntity : BaseEntity //BaseGenericEntity<TEntity>
    {
        /// <summary>
        /// Возвращает то объектное хранилище, из которого берутся данные для данного репозитория.
        /// </summary>
        /// <returns></returns>
        public IObjectStorage ObjectStorage { get; }

        public Task<Result<List<TEntity>>> GetAllAsync();
        public Task<Result<List<TEntity>>> GetAllAsync(Expression<Func<TEntity, bool>> filter);
        
        /// <summary>
        /// Получение объекта из репозитория по его Id.
        /// </summary>
        /// <param name="id">Id требуемого объекта</param>
        /// <param name="querySpec">Дополнительные настройки запроса (в данном случае значение имеют связи - Include)</param>
        /// <returns>
        /// Если объект с заданным Id не найден - вернется Result.Success, но с Value = null,
        /// при этом в лог сообщение не выводится. Если найдется несколько объектов с такими Id - вернется Result.Error.
        /// </returns>
        public Task<Result<TEntity>> GetByIdAsync(int id, Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? querySpec = null);
        
        /// <summary>
        /// Считывание из БД списка объектов данного типа и возврат их в виде List{TEntity} 
        /// </summary>
        /// <param name="querySpec">Секции Include, Where, OrderBy для применения к источнику данных</param>
        /// <returns></returns>
        public Task<Result<List<TEntity>>> GetListAsync(Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? querySpec = null);

        #region ========== Синхронные методы Add/Update/Delete ==========
        /// <summary>
        /// Объект добавляется только в памяти. Для сохранения в БД потребуется вызов <see cref="IObjectStorage.SaveChangesAsync"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Result Add(TEntity entity);
        /// <summary>
        /// Объект изменяется только в памяти. Для сохранения в БД потребуется вызов <see cref="IObjectStorage.SaveChangesAsync"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Result Update(TEntity entity);

        /// <summary>
        /// Объект удаляется только в памяти. Для сохранения в БД потребуется вызов <see cref="IObjectStorage.SaveChangesAsync"/>
        /// </summary>
        /// <param name="id">Идентификатор объекта</param>
        /// <returns></returns>
        public Result Delete(int id);
        /// <summary>
        /// Объект удаляется только в памяти. Для сохранения в БД потребуется вызов <see cref="IObjectStorage.SaveChangesAsync"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Result Delete(TEntity entity);
        #endregion

        #region ========== Асинхронные методы Add/Update/Delete ==========
        /// <summary>
        /// Асинхронная версия метода <see cref="Add(TEntity)"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<Result> AddAsync(TEntity entity);
        /// <summary>
        /// Асинхронная версия метода <see cref="Update(TEntity)"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<Result> UpdateAsync(TEntity entity);

        /// <summary>
        /// Асинхронная версия метода <see cref="Delete(TEntity)"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<Result> DeleteAsync(TEntity entity);
        #endregion

        [Obsolete("Метод устарел! Необходимо использовать GetListAsync с настройкой через QuerySpecification.")]
        public Result<IQueryable<TEntity>> GetQuerable();
        
        /// <summary>
        /// Обновление (перезачитка) объекта из источника данных
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<Result> RefreshAsync(TEntity entity);
        
        /// <summary>
        /// Опциональный метод конфигурирования репозитория.
        /// Необходимость возникла для создания репозиториев, заточенных на конкретный тип, 
        /// с вариативностью: создать новый DBContext или исопльзовать существующий.
        /// Состав параметров индивидуален и может отличаться для разных типов репозиториев.
        /// </summary>
        /// <param name="args">Словарь с именованными параметрами. Ключ - идентификатор, как правило - enum конкретного репозитория.</param>
        public Result Configure(Dictionary<int, object> args);
        
        /// <summary>
        /// Объект-конфигуратор запроса (секции Select (Include), Where, OrderBy)
        /// </summary>
        public Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? ConfigureQuerySpecification { get; set; }
        
        /// <summary>
        /// Делегат который, при наличии, настраивает секцию Select запроса
        /// </summary>
        [Obsolete("Свойство устарело! Необходимо использовать GetListAsync с настройкой через QuerySpecification.")]
        public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureSelect { get; set; }

        /// <summary>
        /// Делегат который, при наличии, настраивает секцию Where запроса
        /// </summary>
        [Obsolete("Свойство устарело! Необходимо использовать GetListAsync с настройкой через QuerySpecification.")]
        public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureWhere { get; set; }
        
        /// <summary>
        /// Делегат который, при наличии, настраивает секцию OrderBy запроса
        /// </summary>
        [Obsolete("Свойство устарело! Необходимо использовать GetListAsync с настройкой через QuerySpecification.")]
        public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureOrderBy { get; set; }
    }
}
