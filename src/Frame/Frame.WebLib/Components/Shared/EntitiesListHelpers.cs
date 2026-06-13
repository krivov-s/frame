using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core;
using Microsoft.AspNetCore.Components;

namespace Frame.WebLib.Components.Shared
{
    /// <summary>
    /// Класс, содержащий метаданные об операции, которая будет показана в меню грида и выполнена как команда.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class EntitiesListOperation<TEntity> where TEntity : BaseEntity
    {
        /// <summary>
        /// Данный пункт - линия-разделитель
        /// </summary>
        public bool Divider = false;
        
        /// <summary>
        /// Пункт меню будет активен только на отмеченных записях. Если отмеченных нет - пункт будет не активен.
        /// </summary>
        public bool ActiveOnSelected = false;
        
        /// <summary>
        /// Пункт меню будет активен только на текущей записи. Если текущей нет - пункт будет не активен.
        /// </summary>
        public bool ActiveOnCurrent = false;
        
        /// <summary>
        /// Пункт меню будет активен только если нет ни выбранных записей ни текущей.
        /// </summary>
        public bool ActiveOnNoCurrentAndSelected = false;
        
        /// <summary>
        /// Пункт меню будет активен всегда. В аргументах будет передан список всего, что выделено или активная запись, если выделение отсутствует. Если ничего не выделено - будет передан пустой список.
        /// </summary>
        public bool ActiveAlways = false;
        
        /// <summary>
        /// Ссылка на страницу, которая будет открыта при выборе пункта. Прогоняется через Smart.Format, т.е. есть возможность использовать {Entity}, {TEntity}
        /// </summary>
        public string Href = "";
        
        /// <summary>
        /// Если true - страница по <see cref="Href"/> будет открыта в новом окне.
        /// </summary>
        public bool NewWindow = false;
        
        /// <summary>
        /// Действие (Action), которое будет вызвано при выборе пункта. Href в этом случае не вызывается. 
        /// <para>Содержимое массива зависит от <see cref="ActiveOnCurrent"/> и от <see cref="ActiveOnSelected"></see></para>
        /// <para>Если <see cref="ActiveOnCurrent"/> = true в массиве будет только текущая запись (при наличии).</para>
        /// <para>Если <see cref="ActiveOnSelected"/> = true в массиве будет только выбранные (отмеченные) записи (при наличии).</para>
        /// </summary>
        public Action<string, HashSet<TEntity>, IBaseRepository<TEntity>>? Action;
        public Func<string, HashSet<TEntity>, IBaseRepository<TEntity>, Task>? ActionAsync;
    }

    /// <summary>
    /// Режим работы компонента <see cref="EntitiesListDetail"/> 
    /// </summary>
    public enum EntitiesListEditMode
    {
        /// <summary>
        /// Внутренний диалог MudDataGrid (сейчас не используется)
        /// </summary>
        InternalDialog,
        /// <summary>
        /// При двойном клике или нажатии на кнопку создать/редактировать - открывает новую вкладку по заданному URI
        /// </summary>
        ExternalUri,
        /// <summary>
        /// При двойном клике или нажатии на кнопку создать/редактировать - в текущей вкладке открывает форму,
        /// определенную в FormContent 
        /// </summary>
        FormContent,
        /// <summary>
        /// Режим "Только чтение"
        /// </summary>
        ReadOnly
    };

    public class EntitiesListDetailFormState<TEntity>() where TEntity : BaseEntity
    {
        //protected IBaseRepository<TEntity> _repository = repository;
        //public IBaseRepository<TEntity>? ObjectRepository { get => _repository; }

        /// <summary>
        /// Событие, говорящее о том, что в ObjectRepository был добавлен новый объект.
        /// </summary>
        public EventCallback<TEntity> OnNewEntityAdded { get; set; }

        /// <summary>
        /// Установить у формы флаг того, что в <see cref="ObjectRepository"/> был добавлен новый объект.
        /// </summary>
        /// <param name="entity"></param>
        public void NotifyNewEntityAdded(TEntity entity)
        {
            //_newEntityAdded = true;
            OnNewEntityAdded.InvokeAsync(entity);
        }
    }


    //public class EntitiesListFormContentContext<TEntity>
    //{
    //    public TEntity? Entity { get; set; }
    //    public EventCallback OnNewAdded { get; set; }
    //}
}
