using Frame.App.Security;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Params;
using Frame.Shared;

namespace Frame.App.Cores
{
    /// <summary>
    /// Точка доступа для основных параметров текущего (вызывающего) пользователя (безопасность, список параметров и др.)
    /// </summary>
    public interface IUserCore
    {
        /// <summary>
        /// Привязка данного экземпляра UserCore к пользователю (первичная инициализация).
        /// Должна проходить до первого использования UserCore.
        /// В текущей реализации UserCore является Scoped-объектом, поэтому после инициализации
        /// в течение всего срока жизни объекта (а это по сути текущая сессия) он обладает информацией
        /// о текущем пользователе и может отдавать ее и синхронным и асинхронным запросам. 
        /// </summary>
        public Task InitializeAsync();
        
        /// <summary>
        /// Статус экземпляра UserCore: инициализирован или нет
        /// </summary>
        /// <returns>true - инициализирован, имя пользователя установлено</returns>
        public bool IsInitialized { get; }

        /// <summary>
        /// Login пользователя, который был передан при инициализации UserCore методом <see cref="InitializeAsync"/>
        /// </summary>
        public string CurrentUserLogin { get; }

        /// <summary>
        /// Текущий пользователь, забирается из UserSession.
        /// </summary>
        /// <exception cref="Exception">Если UserCore не инициализирован</exception>
        public User? CurrentUser { get; }

        /// <summary>
        /// Список параметров текущего пользователя.
        /// </summary>
        /// <exception cref="Exception">Если UserCore не инициализирован</exception>
        public ParamList UserParamList { get; }
        
        #region ==================== Свойства и методы по работе с пользовательским списком параметров ====================

        /// <summary>
        /// Сохранение в БД пользовательского профиля, в т.ч. списка параметров
        /// </summary>
        /// <returns></returns>
        public Task<Result> SaveUserProfileAsync();

        #endregion
        
        #region ==================== Свойства и методы, связанные с правами доступа ====================
        
        /// <summary>
        /// Возможно ли просматривать содержимое объектов данного типа?
        /// </summary>
        /// <returns></returns>
        public Result<bool> CanView<TEntity>() where TEntity : BaseEntity;

        /// <summary>
        /// Возможно ли просматривать содержимое данного конкретного объекта?
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Result<bool> CanView(BaseEntity obj);
        
        /// <summary>
        /// Возможно ли создавать объекты данного типа?
        /// </summary>
        /// <returns></returns>
        public Result<bool> CanAdd<TEntity>() where TEntity : BaseEntity;

        /// <summary>
        /// Возможно ли создать данный конкретный объект?
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Result<bool> CanAdd(BaseEntity obj);

        /// <summary>
        /// Возможно ли изменять объекты данного типа?
        /// </summary>
        /// <returns></returns>
        public Result<bool> CanModify<TEntity>() where TEntity : BaseEntity;

        /// <summary>
        /// Возможно ли изменить конкретный объект? Внутри идет проверка сначала для типа.
        /// Если нельзя - то нельзя вне зависимости от конкретного объекта.
        /// </summary>
        /// <param name="obj">Проверяемый объект</param>
        /// <param name="changedPropValues">Список свойств, которые изменяются в объекте.
        /// Если заданы - будут проверяться ограничения по <see cref="TEntityRights.ReadAttrs"/>
        /// и <see cref="TEntityRights.ModifyAttrs"/>: если они там присутствуют - будет выдана ошибка.</param>
        /// <returns></returns>
        public Result<bool> CanModify(BaseEntity obj, List<ChangedPropValue>? changedPropValues = null);

        /// <summary>
        /// Возможно ли удалять объекты данного типа?
        /// </summary>
        /// <returns></returns>
        public Result<bool> CanDelete<TEntity>() where TEntity : BaseEntity;

        /// <summary>
        /// Возможно ли удалить конкретный объект? Внутри идет проверка сначала для типа. Если нельзя - то нельзя вне зависимости от конкретного объекта.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Result<bool> CanDelete(BaseEntity obj);

        /// <summary>
        /// Имеет ли пользователь право на аудит записи
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanAudit<TEntity>() where TEntity : BaseEntity;

        /// <summary>
        /// Имеет ли пользователь право на экспорт (сохранение в виде шаблона)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanSaveAsTemplate<TEntity>() where TEntity : BaseEntity;

        /// <summary>
        /// Имеет ли пользователь право на экспорт (сохранение в виде шаблона)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanSaveAsTemplate(BaseEntity obj);

        /// <summary>
        /// Имеет ли пользователь право на импорт (создание из шаблона)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CreateFromTemplate<TEntity>() where TEntity : BaseEntity;
        
        /// <summary>
        /// Имеет ли пользователь право на импорт (создание из шаблона)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CreateFromTemplate(BaseEntity obj);
        /// <summary>
        /// Добавляет фильтр системы безопасности к переданному списку
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public Result<IQueryable<T>> ApplyReadQueryFilter<T>(IQueryable<T> query) where T : BaseEntity;

        /// <summary>
        /// Возвращает список имен свойств, которые доступны для просмотра.
        /// Если список пуст - значит ограничений нет, все доступно. 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Result<List<string>> GetReadableAttrNames<TEntity>() where TEntity : BaseEntity;
        
        /// <summary>
        /// Возвращает список имен свойств, которые доступны для редактирования.
        /// Если список пуст - значит ограничений нет, все доступно. 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Result<List<string>> GetEditableAttrNames<TEntity>() where TEntity : BaseEntity;
        
        #endregion
    }
}
