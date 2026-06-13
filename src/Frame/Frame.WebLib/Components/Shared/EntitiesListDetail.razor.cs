using System.Linq.Dynamic.Core;
using Frame.App.Cores;
using Frame.App.EntityTemplates;
using Frame.App.IEntityRepositories;
using Frame.App.Scripting;
using Frame.App.Security;
using Frame.Domain;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Params;
using Frame.Domain.Entities.Core.Reports;
using Frame.Domain.Entities.Core.Scripting;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Params;
using Frame.Domain.QuerySpec;
using Frame.Infrastructure;
using Frame.Rep.Interfaces;
using Frame.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using SmartFormat;
using Microsoft.Extensions.Logging;
using Frame.WebLib.Shared;
using Microsoft.Extensions.DependencyInjection;
using Frame.Domain.Entities.Core.Queries;

// TODO: Экспорт данных в Excel https://github.com/radzenhq/radzen-blazor/blob/master/RadzenBlazorDemos.Host/Controllers/ExportController.cs

namespace Frame.WebLib.Components.Shared
{
    public partial class
        EntitiesListDetail<TEntity, TParentComponent> //where TEntity : BaseEntity, IBaseGenericEntity<TEntity>
    {
        #region ========== Внешние сервисы ==========

        [Inject] private IServiceProvider? _services { get; set; }
        [Inject] private IGetCurrentUserNameService? _getCurrentUserService { get; set; }
        [Inject] private IDialogService? _dialogService { get; set; }
        [Inject] private IJSRuntime? _jsRuntime { get; set; }
        [Inject] private ISnackbar? _snackBar { get; set; }
        [Inject] private ILogger<TParentComponent>? _logger { get; set; }
        [Inject] private IUserCore? _userCore { get; set; }
        [Inject] private IScriptCore? _scriptCore { get; set; }
        [Inject] private AppCoreProvider? _appCoreProvider { get; set; }
        [Inject] private IFrameReporting? _frameReporting { get; set; }
        [Inject] private IEntityTemplateService? _entityTemplateService { get; set; }

        #endregion

        #region ========== Внешние (cascading) параметры компонента ==========

        /// <summary>
        /// Объектное хранилище, определенное родительской формой. Если задано - обязательно используется.
        /// </summary>
        [CascadingParameter]
        IObjectStorage? _sharedObjectStorage { get; set; }

        /// <summary>
        /// Параметр с информацией о хранилище и статусом хранилища, который может прийти из главной внешней 
        /// композитной формы, если список используется в ее составе.
        /// Этот параметр "подцепляется" всеми универсальными компонентами (RefEdit, SimpleEditForm), 
        /// которые забирают оттуда ObjectStorage.
        /// Таким образом достигается единство хранилища среди нескольких форм редактирования.
        /// Сохранение во внешниюю БД осуществляется скопом всех изменений, которые сделаны в ObjectStorage, 
        /// и делать это должен <see cref="CompositeForm{TParentComponent}"/>
        /// </summary>
        [CascadingParameter]
        CompositeFormState? _compositeFormState { get; set; }

        #endregion

        #region ========== Внешний вид ==========

        [Parameter] public required RenderFragment GridContent { get; set; }
        [Parameter] public required RenderFragment<TEntity?> FormContent { get; set; }
        [Parameter] public required RenderFragment<TEntity?> ChildRowContent { get; set; }
        
        /// <summary>
        /// Заголовок списка
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "";

        /// <summary>
        /// Функция динамического получения заголовка списка
        /// </summary>
        [Parameter]
        public Func<string>? DynamicTitle { get; set; }

        /// <summary>
        /// Стиль шрифта заголовка. По-умолчанию <see cref="Typo.h6"/>
        /// </summary>
        [Parameter]
        public Typo TitleTypo { get; set; } = Typo.h6;

        [Parameter] public bool UseSecuritySettings { get; set; } = true;
        [Parameter] public bool ShowTitle { get; set; } = true;
        [Parameter] public bool ShowAddButton { get; set; } = true;
        [Parameter] public bool ShowAddFromTemplateButton { get; set; } = false;
        [Parameter] public bool ShowEditButton { get; set; } = true;
        [Parameter] public bool ShowDeleteButton { get; set; } = true;
        [Parameter] public bool ShowDeleteSelectedButton { get; set; } = true;
        [Parameter] public bool ShowAuditButton { get; set; } = true;
        [Parameter] public bool ShowSaveAsTemplateButton { get; set; } = false;
        [Parameter] public bool ShowParamListButton { get; set; } = true;
        [Parameter] public bool ShowReportsButton { get; set; } = true;
        
        /// <summary>
        /// Если True - появится галка слева, и при ее нажатии под каждой записью будет отображаться
        /// раздел ChildRowContent (режим Master-Defail)
        /// </summary>
        [Parameter] public bool ShowHierarchyColumn { get; set; } = false;
        [Parameter] public string TooltipAddButton { get; set; } = "Добавить новую запись";
        [Parameter] public string TooltipAddFromTemplateButton { get; set; } = "Создать новую запись по шаблону";
        [Parameter] public string TooltipEditButton { get; set; } = "Редактировать текущую запись";
        [Parameter] public string TooltipDeleteButton { get; set; } = "Удалить текущую запись";
        [Parameter] public string TooltipDeleteSelectedButton { get; set; } = "Удалить выделенные";
        [Parameter] public string TooltipRefreshListButton { get; set; } = "Обновить список";
        [Parameter] public string TooltipSaveAsTemplateButton { get; set; } = "Сохранить текущую запись как шаблон для последующего использования";
        [Parameter] public string TooltipAuditButton { get; set; } = "Аудит текущей записи";

        [Parameter]
        public string TooltipParamListButton { get; set; } = "Редактирование списка параметров пользователя";

        [Parameter] public string TooltipReportsButton { get; set; } = "Подготовка отчета по выбранным записям";


        /// <summary>
        /// Список операций, которые будут показаны в меню и могут быть выполнены с выделенными записями.
        /// <list type="string">
        /// <item>- string - имя меню</item>
        /// <item>- EntitiesListOperation - список отмеченных элементов списка TEntity, который будет передан в Action</item>
        /// </list> 
        /// </summary>
        [Parameter]
        public Dictionary<string, EntitiesListOperation<TEntity>>? Operations { get; set; }

        /// <summary>
        /// Режим редактирования объекта из списка (как обрабатывается команда на редактирование): 
        /// переход по внешней ссылке, или внутренняя форма, описанная в FormContent. По-умолчанию - FormContent.
        /// </summary>
        [Parameter]
        public EntitiesListEditMode EditMode { get; set; } = EntitiesListEditMode.FormContent;

        [Parameter] public bool Filterable { get; set; } = true;

        #endregion

        #region ========== Действия, события и делегаты ==========

        /// <summary>
        /// Сигнал о том, что объект готов к редактированю в виде синхронного Action.
        /// Вызывается перед внутренним созданием/редактированием объекта. 
        /// Не вызывается перед переходом по ссылке (внешнее создание/редактирование).
        /// Не вызывается если определен <see cref="OnEditAsync"/> или <see cref="OnAddNewAsync"/>
        /// </summary>
        [Parameter]
        public Action<TEntity, IBaseRepository<TEntity>?>? OnBeforeEditFormContent { get; set; }

        /// <summary>
        /// Сигнал о том, что объект готов к редактированю в виде асинхронного Func.
        /// Вызывается перед внутренним созданием/редактированием объекта после вызова синхронной версии <see cref="OnBeforeEditFormContent"/>. 
        /// Не вызывается перед переходом по ссылке (внешнее создание/редактирование).
        /// Не вызывается если определен <see cref="OnEditAsync"/> или <see cref="OnAddNewAsync"/>
        /// </summary>
        [Parameter]
        public Func<TEntity, IBaseRepository<TEntity>?, Task<Result<TEntity>>>? OnBeforeEditFormContentAsync
        {
            get;
            set;
        }

        /// <summary>
        /// Делегат, который вызвается для получения данных от репозитория.
        /// Может быть определен пользователем если требуется доп. фильтрация, сортировка и т.п.
        /// Вызывается внутри метода <see cref="LoadDataAsync">LoadDataAsync</see>
        /// </summary>
        [Parameter]
        public Func<IBaseRepository<TEntity>, Task<Result<List<TEntity>>>>? OnLoadDataAsync { get; set; }

        /// <summary>
        /// Переопределяет внутренний метод добавления нового элемента
        /// </summary>
        [Parameter]
        public Func<IBaseRepository<TEntity>, Task<Result<TEntity>>>? OnAddNewAsync { get; set; }

        /// <summary>
        /// Переопределяет внутренний метод добавления нового элемента по шаблону
        /// </summary>
        [Parameter]
        public Func<IBaseRepository<TEntity>, Task<Result<TEntity>>>? OnAddFromTemplateAsync { get; set; }

        /// <summary>
        /// Переопределяет внутренний метод редактирования выделенного элемента
        /// </summary>
        [Parameter]
        public Func<IBaseRepository<TEntity>, TEntity, Task<Result>>? OnEditAsync { get; set; }

        /// <summary>
        /// Переопределяет внутренний метод удаления выделенного элемента
        /// </summary>
        [Parameter]
        public Func<IBaseRepository<TEntity>, TEntity, Task<Result>>? OnDeleteAsync { get; set; }

        /// <summary>
        /// Двойной клик мыши по строке. Если делегат не определен - будет считаться что это редактирование текущей строки
        /// </summary>
        [Parameter]
        public Func<TEntity, Task>? OnRowMouseDoubleClickAsync { get; set; }

        /// <summary>
        /// Uri страницы, которая будет вызываться при нажатии на кнопку AddNew (если не задан делегат OnAddNewAsync)
        /// </summary>
        [Parameter]
        public string AddNewEntityUri { get; set; } = "{TEntity.TypeName}/0";

        /// <summary>
        /// Uri страницы, которая будет вызываться при нажатии на кнопку AddNew (если не задан делегат OnAddNewAsync)
        /// </summary>
        [Parameter]
        public string AddFromTemplateUri { get; set; } = "{TEntity.TypeName}?TemplateId={Template.Id}";

        /// <summary>
        /// Uri страницы редактирования текущей записи. 
        /// <para>Uri прогоняется через Smart.Format, с возможностью получения атрибутов выбранного объекта через Entity, наименования типа через TEntity.TypeName.</para>
        /// <para>
        /// Примеры допустимого Uri:
        /// <list type="bullet">
        /// <item><![CDATA[replan_line?REPlanId=1&REPlanLineId=2]]></item>
        /// <item>replan/{Entity.Id}</item>
        /// </list>>
        /// </para>
        /// </summary>
        [Parameter]
        public string EditEntityUri { get; set; } = "{TEntity.TypeName}/{Entity.Id}";

        /// <summary>
        /// Uri страницы аудита текущей записи. По-умолчанию страница аудита - audit/тип объекта/id объекта
        /// <para>Uri прогоняется через Smart.Format, с возможностью получения атрибутов выбранного объекта через Entity, наименования типа через TEntity.TypeName.
        /// Например: replan/{Entity.Id}</para>
        /// </summary>
        [Parameter]
        public string AuditEntityUri { get; set; } = "audit/{TEntity.TypeName}/{Entity.Id}";

        /// <summary>
        /// Если задано - вызывается при изменении списка параметров (после редактирования)
        /// </summary>
        [Parameter]
        public Func<Task>? OnParamListChanged { get; set; }

        #endregion

        #region ========== Конфигурирование источника данных ==========

        /// <summary>
        /// Список для отображения в гриде. Если задан - грид переходит в режим <see cref="_operateInMemory"/> == true
        /// В этом режиме сохранение будет осуществляться только в память, SaveChangesAsync не вызывается, 
        /// обновление данных (LoadDataAsync) не осуществляется, все операции Add/Modift/Delete 
        /// осуществляются только в переданном списке
        /// </summary>
        [Parameter]
        public List<TEntity>? Entities { get; set; }

        /// <summary>
        /// Делегат-конфигуратор запроса (секции Select (Include), Where, OrderBy)
        /// </summary>
        [Parameter]
        public Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? ConfigureQuerySpecification
        {
            get;
            set;
        }

        /// <summary>
        /// Конфигурирование секции Select запроса. 
        /// <para>Не используется в режиме InMemory и если задано OnLoadDataAsync.</para>
        /// <para>Внимание! Если задается - то полностью переопределяет ConfigureSelect, 
        /// который был задан (если был задан) при регистрации репозитория</para>
        /// </summary>
        [Obsolete("Свойство устарело! Необходимо использовать QuerySpecification.")]
        [Parameter]
        public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureSelect { get; set; }

        /// <summary>
        /// Конфигурирование секции Where запроса. 
        /// <para>Не используется в режиме InMemory и если задано OnLoadDataAsync.</para>
        /// <para> При работе в режиме <see cref="_operateInMemory"/>==true не используется. </para>
        /// <para>Внимание! Если задается - то полностью переопределяет ConfigureWhere, который был задан 
        /// (если был задан) при регистрации репозитория</para>
        /// </summary>
        [Obsolete("Свойство устарело! Необходимо использовать QuerySpecification.")]
        [Parameter]
        public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureWhere { get; set; }

        /// <summary>
        /// Конфигурирование секции OrderBy запроса. 
        /// <para>Не используется в режиме InMemory и если задано OnLoadDataAsync.</para>
        /// <para>Внимание! Если задается - то полностью переопределяет ConfigureOrderBy, который был задан 
        /// (если был задан) при регистрации репозитория</para>
        /// </summary>
        [Obsolete("Свойство устарело! Необходимо использовать QuerySpecification.")]
        [Parameter]
        public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureOrderBy { get; set; }

        #endregion

        #region ========== Свойства - результаты конфигурации, предоставляемые внешнему миру ==========

        /// <summary>
        /// Объектное хранлище, которое используется для чтения и записи данных
        /// </summary>
        public IObjectStorage? ActiveObjectStorage
        {
            get
            {
                if (_objectStorage == null)
                {
                    if (_sharedObjectStorage != null)
                    {
                        _objectStorage = _sharedObjectStorage;
                    }
                    else
                    {
                        if (_services == null)
                        {
                            throw new Exception("Компонент не инициализирован! Отсутствует Services!");
                        }

                        _objectStorage = _services.GetService<IObjectStorage>();
                        if (_objectStorage == null)
                        {
                            string strError = "Ошибка получения хранилища из коллекции системных сервисов";
                            _logger?.LogError("{strError}: GetService<IObjectStorage>() вернул null", strError);
                            _snackBar?.Add(strError, Severity.Error);
                        }
                    }
                }

                return _objectStorage;
            }
        }

        /// <summary>
        /// Репозиторий, используемый для чтения и записи данных
        /// </summary>
        public IBaseRepository<TEntity>? ActiveRepository
        {
            get
            {
                if (_repository == null)
                {
                    //if (Repository != null)
                    //{
                    //    _repository = Repository;
                    //}
                    //else 
                    if (ActiveObjectStorage != null)
                    {
                        Result<IBaseRepository<TEntity>> resRepo = ActiveObjectStorage.GetBaseRepository<TEntity>();
                        if (resRepo.IsError || resRepo.Value == null)
                        {
                            string strError = "Ошибка при получении репозитория из хранилища";
                            _logger?.LogError("{strError} : {ErrorResult}", strError, resRepo.ErrorResult);
                            _snackBar?.Add(strError, Severity.Error);
                        }
                        else
                        {
                            _repository = resRepo.Value;
                        }
                    }
                }

                return _repository;
            }
            set => _repository = value;
        }

        /// <summary>
        /// Имя конкретного экземпляра списка. Используется для поиска настроек в <see cref="UserProfile"/>
        /// </summary>
        public string ComponentName
        {
            get
            {
                string name = $"{nameof(EntitiesListDetail<TEntity, TParentComponent>)}" +
                              $"[{typeof(TEntity).Name},{typeof(TParentComponent).Name}]";
                if (ConfigureComponentName != null)
                {
                    name = ConfigureComponentName(name);
                }

                return name;
            }
        }

        #endregion

        #region ========== Публичные методы и свойства ==========

        /// <summary>
        /// Конфигурирование наименования компонента. Сделано для ситуации, когда один и тот же 
        /// <see cref="EntitiesListDetail{TEntity,TParentComponent}"/>, используется в нескольких режимах работы,
        /// и для каждого режима работы нужно сохранять свои настройки интерфейса. Определив данный делегат
        /// можно, например, "дописать" в конец имени дополнительную информацию, определяющую конкретный режим
        /// использования компонента.
        /// </summary>
        [Parameter]
        public Func<string, string>? ConfigureComponentName { get; set; }

        /// <summary>
        /// Текущий объект списка.
        /// </summary>
        [Parameter]
        public TEntity? CurrentEntity { get; set; }

        /// <summary>
        /// При указании здесь Id записи открывается форма редактирования.
        /// Одновременно устанавливается CurrentEntity в значение с указанным Id 
        /// </summary>
        [Parameter]
        public int? OpenEditFormEntityId { get; set; }

        /// <summary>
        /// Количество строк в гриде
        /// </summary>
        /// <returns></returns>
        public int RecordCount => _entitiesList.Count;

        /// <summary>
        /// Обновление содержимого списка из источника данных
        /// </summary>
        /// <returns></returns>
        public async Task RefreshAsync()
        {
            if (!_operateInMemory)
            {
                await LoadDataAsync();
                StateHasChanged();
            }
        }

        #endregion

        #region ========== Переопределение методов Blzor-компонента ==========

        protected override async Task OnInitializedAsync()
        {
            _isAuthorized =
                await AuthHelpers.CheckAuthorized(_getCurrentUserService, _logger, _snackBar); /*NavigationManager*/

            if (_services == null)
            {
                const string err = "Компонент не инициализирован: ошибка при получении коллекции сервисов IServiceProvider";
                _logger?.LogError(err);
                _snackBar?.Add(err, Severity.Error);
                return;
            }

            if (_userCore == null)
            {
                const string err = "Компонент не инициализирован: ошибка при получении IUserCore";
                _logger?.LogError(err);
                _snackBar?.Add(err, Severity.Error);
                return;
            }

            if (_isAuthorized)
            {
                // Создаем объект состояния формы и подписываемся на событие о том, что дочерняя форма завершила работу и может быть закрыта
                //_formState = new();
                //_formState.OnFormCompleted += FormContentCompleted;

                _userParamList = _userCore.UserParamList;
                _canViewTEntity = _userCore.CanView<TEntity>();
                _dataGridReadOnly = EditMode != EntitiesListEditMode.InternalDialog;

                if (EditMode == EntitiesListEditMode.ReadOnly)
                {
                    // В режиме ReadOnly принудительно убираем с экрана кнопки, перезатирая настройки из свойств грида 
                    // (вдруг их забыли поправить по-ошибке)
                    ShowAddButton = false;
                    ShowDeleteButton = false;
                    ShowEditButton = false;
                    ShowAddFromTemplateButton = false;
                    ShowSaveAsTemplateButton = false;
                }
                
                if (UseSecuritySettings && _userCore != null)
                {
                    // Если на входе кнопки заданы администратором как false (не показывать) - тогда и собственно не нужно ничего проверять у UserCore
                    ShowAddButton = ShowAddButton ? _userCore.CanAdd<TEntity>() : false;
                    ShowDeleteButton = ShowDeleteButton ? _userCore.CanDelete<TEntity>() : false;
                    ShowAuditButton = ShowAuditButton ? _userCore.CanAudit<TEntity>() : false;
                    //ShowAddFromTemplateButton = ShowAddFromTemplateButton ? _userCore.CanCreateFromTemplate<TEntity>() : false;
                    ShowSaveAsTemplateButton = ShowSaveAsTemplateButton ? _userCore.CanSaveAsTemplate<TEntity>() : false;
                    
                    // Кнопку редактирование не корректируем, оставляем это право за SimpleEditForm,
                    // поскольку могут быть сложные формы, с вкладками, и для каждой вкладки - свои типы и права
                    // ShowEditButton = ShowEditButton ? _userCore.CanModify<TEntity>() : false;
                }

                // pageHistoryState.AddPageToHistory(NavigationManager.ToBaseRelativePath(NavigationManager.Uri));
                if (_getCurrentUserService != null)
                {
                    _currentUserName = await _getCurrentUserService.GetLoginAsync();
                }

                if (_currentUserName.Length == 0)
                {
                    _snackBar?.Add("Вы не авторизованы на данной странице", Severity.Warning);
                    _isAuthorized = false;
                    //NavigationManager?.NavigateTo("Login");
                }
                else
                {
                    await LoadSavedFiltersAsync();
                    await LoadScriptCommandsAsync();
                    await LoadReportsAsync();
                    InitOperationsList();
                    await LoadUserProfileAsync();
                    await LoadDataAsync();
                }
                _firstInitCompleted = true;
            }

        }

        protected override async Task OnParametersSetAsync()
        {
            if (OpenEditFormEntityId != null && OpenEditFormEntityId > 0)
            {
                TEntity? entity = _entitiesList.FirstOrDefault(x => x.Id == OpenEditFormEntityId);
                if (entity == null)
                {
                    _snackBar?.Add($"Объект с Id = {OpenEditFormEntityId} не найден в списке", Severity.Error);
                    return;
                }

                await EditAsync(entity);
            }
            else
            {
                // Попытка установить текущую запись на первую строку (в случае, если она не установлена)
                if (CurrentEntity == null && _entitiesList.Count > 0)
                {
                    CurrentEntity = _entitiesList[0];
                    MoveCurrentRecord(0, 0);
                }
            }
        }

        // protected override Task OnAfterRenderAsync(bool firstRender)
        // {
        //     if (firstRender)
        //     {
        //         _rowsPerPage = 50;
        //         StateHasChanged();
        //     }
        //     
        //     return Task.CompletedTask;
        // }
        //
        #endregion

        #region ========== Внутренние (private) служебные переменные и свойства ==========

        private IObjectStorage? _objectStorage;
        private IBaseRepository<TEntity>? _repository;

        private MudDataGrid<TEntity>? _dataGrid;
        private List<TEntity> _entities = [];
        private HashSet<TEntity> _selectedEntities = [];
        private HashSet<TEntity> _entitiesToReload = [];
        private ParamList _userParamList = new();
        private int _currentRowNumber = -1;
        
        private const int DEFAULT_ROWS_PER_PAGE = 10;
        private int _rowsPerPage { get; set; } = DEFAULT_ROWS_PER_PAGE;
        private bool _firstInitCompleted = false;

        /// <summary>
        /// Этот ObjectStorage предназначен для загрузки вспомогательных данных (скрипты, отчеты)
        /// Нужен чтобы избегать блокировок/ожиданий при работе в многокомпонентной среде, когда например
        /// несколько гридов лежат на одной форме и используют один и тот же ObjectStorage с данными
        /// </summary>
        private IObjectStorage? __serviceObjectStorage;
        private IObjectStorage? _serviceObjectStorage
        {
            get
            {
                if (__serviceObjectStorage != null) return __serviceObjectStorage;
                
                if (_services == null)
                {
                    throw new Exception("Компонент не инициализирован! Отсутствует Services!");
                }

                __serviceObjectStorage = _services.GetService<IObjectStorage>();
                if (__serviceObjectStorage == null)
                {
                    string strError = "Ошибка получения хранилища из коллекции системных сервисов";
                    _logger?.LogError("{strError}: GetService<IObjectStorage>() вернул null", strError);
                    _snackBar?.Add(strError, Severity.Error);
                }

                return __serviceObjectStorage;
            }
        }
        
        /// <summary>
        /// Предыдущее значение CurrentEntity - для корректного отслеживания Mouse Dblclick
        /// </summary>
        private TEntity? _prevCurrentEntity;

        private bool _reloadAll;
        private string _currentUserName { get; set; } = "";
        private bool _dataGridReadOnly = true;
        private bool _loading;
        private bool _needToRestorePage;
        private int _pageToRestore;
        private bool _isAuthorized;
        private bool _canViewTEntity; // Разрешен просмотр объектов данного типа или нет.
        private bool _isEditing;

        /// <summary>
        /// Признак того, что все операции выполняются в памяти, без сохранения в БД.
        /// </summary>
        private bool _operateInMemory => Entities != null;

        /// <summary>
        /// Коллекция с объектами, используемая для грида. Используется Entities, если задано, иначе - внутренний список _entities
        /// </summary>
        private List<TEntity> _entitiesList => Entities ?? _entities;

        /// <summary>
        /// Список доступных сохраненных фильтров, зарегистрированных для TEntity.
        /// Если таковые существуют - они отображаются в заголовке в виде ComboBox, после выбора пользователем
        /// выбранный фильтр накладывается на основной запрос.
        /// <para>Фильтры не загружаются и не отображаются в режиме работы <see cref="_operateInMemory"/> == true</para> 
        /// </summary>
        private List<SavedFilter> _savedFilters = [];

        /// <summary>
        /// Список доступных операций, зарегистрированных для TEntity.
        /// Если таковые существуют - они добавляются в список выполняемых операций.
        /// После выбора пользователем все выделенные и активные записи передаются в автоооперацию в качестве аргумента.
        /// </summary>
        private List<ScriptCommand> _scriptCommands = [];

        /// <summary>
        /// Список отчетов, зарегистрированных для TEntity.
        /// Если таковые существуют - они добавляются в список.
        /// После выбора пользователем все выделенные и активные записи передаются в процедуру подготовки
        /// выбранного отчета в качестве аргумента.
        /// </summary>
        private List<FrameReport> _reports = [];

        /// <summary>
        /// Список операций со списком. Состоит из объединения преднастроенных заранее операций, переданных
        /// в параметре <see cref="Operations"/> и списка <see cref="ScriptCommand"/>, у которых установлен
        /// тип равный типу списка <see cref="TEntity"/>
        /// </summary>
        private Dictionary<string, EntitiesListOperation<TEntity>> _operations = [];

        /// <summary>
        /// Текущий активный фильтр
        /// </summary>
        private SavedFilter? _savedFilter { get; set; }

        private string _savedFilterLabel => _savedFilter != null ? "Установлен фильтр" : "Выберите фильтр";

        // /// <summary>
        // /// Первоначальный <see cref="ActiveRepository.ConfigureWhere"/>, до его изменения фильтрами.
        // /// </summary>
        // private Func<IQueryable<TEntity>, IQueryable<TEntity>>? _configureWhereOriginal { get; set; }

        private string _returnToListButtonText { get; set; } = "Вернуться в список";

        #endregion

        #region ========== Внутренние (private) методы компонента ==========

        /// <summary>
        /// Этот метод вызывается после того, как объект добавлен в ObjectReposotory - это значит что при сохранении ObjectStorage он будет добавлен в БД
        /// </summary>
        private void OnNewEntityAdded(TEntity entity)
        {
            // Перед добавлением проверяем, а вдруг объект уже есть в списке
            if (_entitiesList.All(x => x.GetHashCode() != entity.GetHashCode()))
            {
                _entitiesList.Add(entity);
            }

            StateHasChanged();
        }

        /// <summary>
        /// Метод для перевода компонента в режим списка
        /// </summary>
        /// <returns></returns>
        private async Task OnFormContentClose()
        {
            _isEditing = false;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Метод для перевода компонента в режим редактирования
        /// </summary>
        /// <returns></returns>
        private async Task OnFormContentOpen()
        {
            _isEditing = true;
            await InvokeAsync(StateHasChanged);
        }

        private async Task AddNewAsync()
        {
            if (EditMode == EntitiesListEditMode.ReadOnly)
            {
                return;
            }

            // Если задан делегат функции добавления - вызываем его. Внутреннее добавление не используем.
            if (OnAddNewAsync != null && ActiveRepository != null)
            {
                Result<TEntity> res = await OnAddNewAsync(ActiveRepository);
                if (res.IsError)
                {
                    _snackBar?.Add(res.ErrorResult, Severity.Error);
                }
                else
                {
                    // Если объект успешно создан - добавляем его в список перезачитки
                    await RefreshAsync();
                }
                return;
            }

            if (EditMode == EntitiesListEditMode.FormContent)
            {
                // Если определена внутренняя форма редактирования - создам новый объект и делаем форму активной
                if (FormContent != null)
                {
                    if (CurrentEntity != null)
                    {
                        _prevCurrentEntity = CurrentEntity;
                    }

                    CurrentEntity = new();
                    OnBeforeEditFormContent?.Invoke(CurrentEntity, ActiveRepository);
                    if (OnBeforeEditFormContentAsync != null)
                    {
                        Result<TEntity> res = await OnBeforeEditFormContentAsync(CurrentEntity, ActiveRepository);
                        if (res.IsErrorOrNull)
                        {
                            _snackBar?.Add(res.ErrorResult, Severity.Error);
                            return;
                        }

                        CurrentEntity = res.Value;
                    }

                    await OnFormContentOpen();
                }
                else
                {
                    string strError =
                        "Ошибка конфигурирования: установлен режим редактирования FormContent при этом FormContent не определен";
                    _logger?.LogError("{strError}", strError);
                    _snackBar?.Add(strError);
                }
            }
            else if (EditMode == EntitiesListEditMode.ExternalUri &&
                     _jsRuntime != null) //  && CurrentEntity != null
            {
                if (AddNewEntityUri.Length == 0)
                {
                    _snackBar?.Add(
                        "Команда добавления не сконфигурирована корректно: не установлен параметр AddNewEntityUri",
                        Severity.Warning);
                }
                else
                {
                    try
                    {
                        string strUri = Smart.Format(AddNewEntityUri, BuildArgsForSmartFormat(CurrentEntity));
                        await NavigateToNewTab(strUri);
                        _entitiesToReload.Clear();
                        _reloadAll = true;
                    }
                    catch (Exception ex)
                    {
                        _snackBar?.Add(ex.Message, Severity.Error);
                        _logger?.LogError(ex, "Ошибка обработки команды создания нового объекта");
                    }
                }
            }
        }

        private async Task AddFromTemplateAsync()
        {
            if (EditMode == EntitiesListEditMode.ReadOnly || _entityTemplateService == null || _objectStorage == null)
            {
                return;
            }

            // Если задан делегат функции добавления по шаблону - вызываем его. Внутреннее добавление не используем.
            if (OnAddFromTemplateAsync != null && ActiveRepository != null)
            {
                Result<TEntity> res = await OnAddFromTemplateAsync(ActiveRepository);
                if (res.IsError)
                {
                    _snackBar?.Add(res.ErrorResult, Severity.Error);
                }
                else
                {
                    // Если объект успешно создан - добавляем его в список перезачитки
                    await RefreshAsync();
                }

                return;
            }

            // Получаем список шаблонов для данного типа и текущего пользователя
            Result<List<TemplateEntity>> resList = await _entityTemplateService.GetTemplateListAsync<TEntity>();
            if (resList.IsErrorOrNull)
            {
                _snackBar?.Add(resList.ErrorResult, Severity.Error);
                return;
            }

            Result<TemplateEntity> resSelected = await Helpers.ShowSelectDialogAsync(resList.Value!,
                _dialogService, _snackBar, "Выберите шаблон");
            if (resSelected.IsErrorOrNull) return;
            TemplateEntity template = resSelected.Value!;

            if (EditMode == EntitiesListEditMode.FormContent)
            {
                // Если определена внутренняя форма редактирования - создам новый объект и делаем форму активной
                if (FormContent != null)
                {
                    if (CurrentEntity != null)
                    {
                        _prevCurrentEntity = CurrentEntity;
                    }

                    TemplateSerializationContext ctx = new()
                    {
                        ObjectStorage = _objectStorage
                    };
                    Result<TEntity> resObject =
                        await _entityTemplateService.DeserializeAsync<TEntity>(template, ctx);
                    if (resObject.IsErrorOrNull)
                    {
                        _snackBar?.Add(resObject.ErrorResult, Severity.Error);
                        return;
                    }

                    CurrentEntity = resObject.Value!;
                    OnBeforeEditFormContent?.Invoke(CurrentEntity, ActiveRepository);
                    if (OnBeforeEditFormContentAsync != null)
                    {
                        Result<TEntity> res = await OnBeforeEditFormContentAsync(CurrentEntity, ActiveRepository);
                        if (res.IsErrorOrNull)
                        {
                            _snackBar?.Add(res.ErrorResult, Severity.Error);
                            return;
                        }

                        CurrentEntity = res.Value;
                    }

                    await OnFormContentOpen();
                }
            }
            else if (EditMode == EntitiesListEditMode.ExternalUri && _jsRuntime != null) 
            {
                if (AddFromTemplateUri.Length == 0)
                {
                    _snackBar?.Add(
                        "Команда добавления не сконфигурирована корректно: не установлен параметр AddFromTemplateUri",
                        Severity.Warning);
                }
                else
                {
                    try
                    {
                        var args = new { TemplateId = template.Id };
                        string strUri = Smart.Format(AddFromTemplateUri, args);
                        await NavigateToNewTab(strUri);
                        _entitiesToReload.Clear();
                        _reloadAll = true;
                    }
                    catch (Exception ex)
                    {
                        _snackBar?.Add(ex.Message, Severity.Error);
                        _logger?.LogError(ex, "Ошибка обработки команды создания нового объекта по шаблону");
                    }
                }
            }
        }


        private static object BuildArgsForSmartFormat(TEntity? entity)
        {
            return new { Entity = entity, TEntity = new { TypeName = typeof(TEntity).Name } };
        }

        private async Task EditAsync(TEntity entity)
        {
            if (entity == null)
            {
                string strError = "На редактирование передан нулевой объект";
                _logger?.LogError("{strError}", strError);
                _snackBar?.Add(strError);
                return;
            }

            if (EditMode == EntitiesListEditMode.ReadOnly)
            {
                return;
            }

            // Устанавливаем CurrentEntity равным entity 
            await SelectedItemChangedAsync(entity);

            // Если задан делегат функции редактирования - вызываем его. Внутреннее редактирование не используем.
            if (OnEditAsync != null && ActiveRepository != null)
            {
                Result res = await OnEditAsync(ActiveRepository, entity);
                if (res.IsError)
                {
                    _snackBar?.Add(res.ErrorResult, Severity.Error);
                }
                else
                {
                    // Если объект успешно отредактирован - добавляем его в список перезачитки
                    _entitiesToReload.Add(entity);
                }
            }
            else
            {
                if (EditMode == EntitiesListEditMode.FormContent)
                {
                    // Если определена внутренняя форма редактирования - просто делаем ее активной
                    if (FormContent != null)
                    {
                        OnBeforeEditFormContent?.Invoke(entity, ActiveRepository);
                        if (OnBeforeEditFormContentAsync != null)
                        {
                            Result<TEntity> res = await OnBeforeEditFormContentAsync(entity, ActiveRepository);
                            if (res.IsErrorOrNull)
                            {
                                _snackBar?.Add(res.ErrorResult, Severity.Error);
                                return;
                            }

                            entity = res.Value!;
                            // Чисто теоретически entity мог быть изменен, даже заново считан в делегате.
                            // Поэтому заново устанавливаем CurrentEntity равным entity 
                            await SelectedItemChangedAsync(entity);
                        }

                        await OnFormContentOpen();
                    }
                    else
                    {
                        string strError =
                            "Ошибка конфигурирования: установлен режим редактирования FormContent при этом FormContent не определен";
                        _logger?.LogError("{strError}", strError);
                        _snackBar?.Add(strError);
                    }
                }
                else if (EditMode == EntitiesListEditMode.ExternalUri && _jsRuntime != null)
                {
                    if (EditEntityUri.Length == 0)
                    {
                        _snackBar?.Add(
                            "Команда редактирования не сконфигурирована корректно: не установлен параметр EditEntityUri",
                            Severity.Warning);
                    }
                    else
                    {
                        try
                        {
                            string strUri = Smart.Format(EditEntityUri, BuildArgsForSmartFormat(entity));
                            await NavigateToNewTab(strUri);
                            _entitiesToReload.Add(entity);
                        }
                        catch (Exception ex)
                        {
                            _snackBar?.Add(ex.Message);
                            _logger?.LogError(ex, "Ошибка обработки команды редактирования объекта");
                        }
                    }
                }
                else
                {
                    // Редактирование силами самого MudDataGrid не используем!
                }
            }
        }

        private async Task ItemChangedAfterInternalEditing(TEntity entity)
        {
            if (ActiveRepository != null) // EditMode == EntitiesListEditMode.InternalDialog &&
            {
                Result result = await ActiveRepository.UpdateAsync(entity);
                if (result.IsError)
                {
                    _snackBar?.Add($"Ошибка обновления: {result.ErrorResult}", Severity.Error);
                }
                else
                {
                    _snackBar?.Add($"Изменения сохранены", Severity.Success);
                }
            }
        }

        private async Task SelectedItemChangedAsync(TEntity entity)
        {
            if (CurrentEntity != null && entity != null &&
                CurrentEntity.GetHashCode() == entity.GetHashCode())
            {
                return;
            }

            _prevCurrentEntity = CurrentEntity;
            CurrentEntity = entity;
            await CheckAndReload();
        }

        private async Task SelectedItemsChangedAsync(HashSet<TEntity> items)
        {
            _selectedEntities = items;
            await CheckAndReload();
        }

        private async Task ParamListChangedAsync()
        {
            _reloadAll = true;
            if (OnParamListChanged != null)
            {
                await OnParamListChanged();
            }

            await CheckAndReload();
        }

        private async Task CheckAndReload()
        {
            if (!_operateInMemory)
            {
                if (_reloadAll)
                {
                    await LoadDataAsync();
                }
                else if (_entitiesToReload.Count > 0)
                {
                    IBaseRepository<TEntity>? repo = ActiveRepository;
                    if (repo != null)
                    {
                        foreach (TEntity ee in _entitiesToReload)
                        {
                            await repo.RefreshAsync(ee);
                        }

                        _entitiesToReload.Clear();
                    }
                }
            }
            else
            {
                _entitiesToReload.Clear();
                StateHasChanged();
            }
        }

        private async Task DeleteAllAsync()
        {
            if (EditMode == EntitiesListEditMode.ReadOnly)
            {
                return;
            }

            if (ActiveRepository != null && _dialogService != null && _snackBar != null)
            {
                if (_selectedEntities.Count > 0)
                {
                    MarkupString markupString = new(
                        $"Выбрано для удаления записей: <b>{_selectedEntities.Count}.</b> " +
                        $"Вы уверены что хотите <b>удалить все выделенные записи</b>?");
                    bool? result = await _dialogService.ShowMessageBox(
                        "Подтверждение",
                        markupString,
                        yesText: "Да, удалить!", noText: "Отмена");

                    if (result == true)
                    {
                        foreach (TEntity entity in _selectedEntities)
                        {
                            Result r = await DeleteAsync(entity, false, false);
                            if (r.IsError)
                            {
                                return;
                            }

                            // Если есть композитная форма - оповещаем ее, что есть изменения в списке
                            _compositeFormState?.SetDirty(true);
                        }

                        Result rSave = await SaveChangesToStorageAsync("Удаление завершено успешно", "Ошибка удаления");
                        if (!rSave.IsError)
                        {
                            // Убираем удаленные записи из текущего списка и очищаем список выбранных
                            foreach (TEntity entity in _selectedEntities)
                            {
                                _entitiesList.Remove(entity);
                            }

                            _selectedEntities.Clear();
                            StateHasChanged();
                        }
                    }
                }
                else
                {
                    await _dialogService.ShowMessageBox("Удаление", "Записи для удаления не выдеделны!");
                }
            }
            else
            {
                _logger?.LogError(
                    "OnDeleteAsync: отсутствуют один или более из обязательных параметров [entity, Repository, DialogService, SnackBar]");
            }
        }

        private async Task<Result> DeleteAsync(TEntity entity, bool askConfirmation = true,
            bool pushChangesToObjectStorage = true)
        {
            if (EditMode == EntitiesListEditMode.ReadOnly)
            {
                return Result.Success;
            }

            // Если задан делегат функции удаления - вызываем его. Внутреннее удаление не используем.
            if (OnDeleteAsync != null && ActiveRepository != null)
            {
                Result res = await OnDeleteAsync(ActiveRepository, entity);
                if (res.IsError)
                {
                    _snackBar?.Add(res.ErrorResult, Severity.Error);
                    return res;
                }
                else
                {
                    // Если объект успешно удален - удаляем его из списка
                    if (_entitiesList.Exists(e => e.Id == entity.Id))
                    {
                        _entitiesList.Remove(entity);
                        StateHasChanged();
                        // await RefreshAsync();
                    }

                    return Result.Success;
                }
            }
            else
            {
                if (entity != null && ActiveRepository != null && _dialogService != null && _snackBar != null)
                {
                    bool doDelete = true;
                    if (askConfirmation)
                    {
                        MarkupString markupString = new($"Вы уверены что хотите <b>удалить</b> {entity.Description}?");
                        bool? result = await _dialogService.ShowMessageBox(
                            "Подтверждение",
                            markupString,
                            yesText: "Да, удалить!", noText: "Отмена");

                        doDelete = result ?? false;
                    }

                    if (doDelete)
                    {
                        Result res = await ActiveRepository.DeleteAsync(entity);
                        if (res.IsError)
                        {
                            _snackBar.Add(res.ErrorResult, Severity.Error);
                            return res;
                        }
                        else
                        {
                            // Если есть композитная форма - оповещаем ее, что есть изменения в списке
                            _compositeFormState?.SetDirty(true);

                            if (pushChangesToObjectStorage)
                            {
                                Result rSave = await SaveChangesToStorageAsync("Удаление завершено успешно",
                                    "Ошибка удаления");
                                if (!rSave.IsError)
                                {
                                    _entitiesList.Remove(entity);
                                    StateHasChanged();
                                }
                            }

                            return res;
                        }
                    }
                    else
                    {
                        return Result.Success;
                    }
                }
                else
                {
                    string strError =
                        "OnDeleteAsync: отсутствуют один или более из обязательных параметров [entity, Repository, DialogService, SnackBar]";
                    _logger?.LogError("{strError}", strError);
                    return Result.Error(strError);
                }
            }
        }

        private async Task<Result> SaveChangesToStorageAsync(string successMessage, string errorMessage)
        {
            if (ActiveRepository != null && _compositeFormState == null && _operateInMemory == false)
            {
                // Физическое удаление в хранилище осуществляем только если этим не управляет CompositeForm и режим работы с БД, а не с памятью
                Result rSave = await ActiveRepository.ObjectStorage.SaveChangesAsync();
                if (rSave.IsError)
                {
                    _snackBar?.Add($"{errorMessage}: {rSave.ErrorResult}", Severity.Error);
                }
                else
                {
                    _snackBar?.Add(successMessage, Severity.Success);
                    // Если есть композитная форма - оповещаем ее, что все изменения сохранены
                    _compositeFormState?.SetDirty(true);
                }

                return rSave;
            }
            else
            {
                return Result.Success;
            }
        }

        private async Task SaveAsTemplateAsync(TEntity entity)
        {
            if (entity == null || _dialogService == null || _services == null) return;

            string defautName = $"{entity}";
            Result<string?> resAskDialog = await Helpers.ShowAskDialogAsync("Запрос наименования шаблона", 
                "Имя шаблона", defautName, _dialogService);

            if (resAskDialog.IsError)
            {
                _snackBar?.Add(resAskDialog.ErrorResult, Severity.Error);
                return;
            }

            if (string.IsNullOrEmpty(resAskDialog.Value))
            {
                // Пользователь отказался от сохранения
                return;
            }

            IEntityTemplateService? entityTemplateService = _services.GetService<IEntityTemplateService>();
            if (entityTemplateService == null)
            {
                string err = $"Не удалось получить сервис {nameof(IEntityTemplateService)}";
                _logger?.LogError("{Err}", err);
                _snackBar?.Add(err, Severity.Error);
                return;
            }
            
            Result resSave = await entityTemplateService.SaveAsTemplateAsync(entity, resAskDialog.Value);
            if (resSave.IsError)
            {
                _snackBar?.Add(resSave.ErrorResult, Severity.Error);
                return;
            }
            
            _snackBar?.Add("Шаблон успешно сохранен", Severity.Success);
        }
            
        private async Task AuditAsync(TEntity entity)
        {
            if (entity != null && _jsRuntime != null)
            {
                if (AuditEntityUri.Length == 0)
                {
                    _snackBar?.Add(
                        "Команда аудита текущей записи не сконфигурирована корректно: не установлен параметр AuditEntityUri",
                        Severity.Warning);
                }
                else
                {
                    try
                    {
                        string strUri = Smart.Format(AuditEntityUri, BuildArgsForSmartFormat(entity));
                        await NavigateToNewTab(strUri);
                    }
                    catch (Exception ex)
                    {
                        _snackBar?.Add(ex.Message);
                        _logger?.LogError(ex, "Ошибка обработки команды аудита объекта");
                    }
                }
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Как правило, в режиме OperateInMemory ObjectStorage передается через CompositeFormState
                //if(OperateInMemory && ActiveRepository == null)
                //{
                //    string strErr = "Некорректная конфигурация: в случае, когда инициализирован параметр Entities для работы с памятью, " +
                //        "обязательно должен быть передан и Repository, из которого данные были загружены";
                //    _snackBar?.Add(strErr, Severity.Error);
                //}

                if (_dataGrid != null)
                {
                    _pageToRestore = _dataGrid.CurrentPage;
                    _needToRestorePage = _pageToRestore > 0;
                }

                _loading = true;

                if (!_operateInMemory)
                {
                    // Если не получить новый репозиторий - данные не обновятся из БД
                    // Поэтому принудительно гасим старый IObjectStorage. При обращении к ActiveObjectStorage он будет вновь запрошен у Services
                    // ----------------------------------
                    // !!!!!! ВНИМАНИЕ! ЭТО ВОЗМОЖНО ПРИВЕДЕТ К СОЗДАНИЮ НОВОГО ObjectStorage И ПЕРЕДАЧЕ ЕГО В ДОЧЕРНИЕ КОМПОНЕНТЫ.
                    // !!!!!! ПОСЛЕ ЭТОГО ВСЕ ОБЪЕКТЫ, РАНЕЕ СЧИТАННЫС С БД, ПОТЕРЯЮТ ПРИВЯЗКУ К EFDbContext.
                    // !!!!!! ЭТО ПОРОДИТ КУЧУ ОШИБОК ПРИ СОХРАНЕНИИ.
                    // !!!!!! НУЖНО ПОДУМАТЬ, НУЖНО ЛИ ВООБЩЕ ТАКОЕ ОБНОВЛЕНИЕ.
                    _objectStorage = null;
                    // ----------------------------------

                    if (ActiveObjectStorage != null)
                    {
                        Result<IBaseRepository<TEntity>> resRepo = ActiveObjectStorage.GetBaseRepository<TEntity>();
                        if (resRepo.IsError || resRepo.Value == null)
                        {
                            _snackBar?.Add(resRepo.ErrorResult, Severity.Error);
                            return;
                        }

                        ActiveRepository = resRepo.Value;

                        Result<List<TEntity>> result;
                        if (OnLoadDataAsync != null)
                        {
                            result = await OnLoadDataAsync(ActiveRepository);
                        }
                        else
                        {
                            if (ConfigureQuerySpecification != null)
                            {
                                ActiveRepository.ConfigureQuerySpecification = ConfigureQuerySpecification;
                            }
                            else
                            {
                                ActiveRepository.ConfigureSelect = ConfigureSelect ?? ActiveRepository.ConfigureSelect;
                                ActiveRepository.ConfigureWhere = ConfigureWhere ?? ActiveRepository.ConfigureWhere;
                                ActiveRepository.ConfigureOrderBy =
                                    ConfigureOrderBy ?? ActiveRepository.ConfigureOrderBy;
                            }

                            try
                            {
                                if (_savedFilter != null && _savedFilter.Where.Length > 0)
                                {
                                    // Готовим переменные для работы Smart.Format
                                    Result<Object> resContainer =
                                        await SmartFormatHelpers.CreateContainerForSmartFormat(
                                            _savedFilter.ParamListJson, _appCoreProvider);
                                    if (resContainer.IsError || resContainer.Value == null)
                                    {
                                        string err = $"Ошибка чтения данных: {resContainer.ErrorResult}";
                                        _logger?.LogError(err);
                                        _snackBar?.Add(err, Severity.Error);
                                        return;
                                    }

                                    string where = Smart.Format(_savedFilter.Where, resContainer.Value);

                                    if (ActiveRepository.ConfigureSelect != null
                                        || ActiveRepository.ConfigureWhere != null
                                        || ActiveRepository.ConfigureOrderBy != null)
                                    {
                                        // Старый вариант конфигурирования. Нужно будет избавиться от него!
                                        IQueryable<TEntity> Filter(IQueryable<TEntity> qry) => qry.Where(where);
                                        var f1 = ActiveRepository.ConfigureWhere;
                                        if (f1 != null)
                                        {
                                            ActiveRepository.ConfigureWhere = qry => Filter(f1(qry));
                                        }
                                    }
                                    else
                                    {
                                        if (ActiveRepository.ConfigureQuerySpecification != null)
                                        {
                                            // Если текущая конфигурация существует - фильтр накладываем поверх нее
                                            IQuerySpecification<TEntity>
                                                ApplyFilter(IQuerySpecification<TEntity> spec) => spec.Where(where);

                                            var configureCurrent
                                                = ActiveRepository.ConfigureQuerySpecification;
                                            ActiveRepository.ConfigureQuerySpecification
                                                = spec => ApplyFilter(configureCurrent(spec));
                                        }
                                        else
                                        {
                                            // Если текущая конфигурация не существует - создаем новую
                                            ActiveRepository.ConfigureQuerySpecification
                                                = spec => spec.Where(where);
                                        }
                                    }
                                }

                                result = await ActiveRepository.GetAllAsync();
                            }
                            catch (Exception ex)
                            {
                                string err = $"Ошибка чтения данных: {ex.Message}";
                                _logger?.LogError(ex, err);
                                _snackBar?.Add(err, Severity.Error);
                                return;
                            }
                        }

                        if (result.IsError == false)
                        {
                            _entities = result.Value ?? ( []);
                            _reloadAll = false;
                            _entitiesToReload.Clear();
                            _selectedEntities.Clear();
                        }
                        else
                        {
#if DEBUG
                            _snackBar?.Add($"Ошибка чтения данных: {result.ErrorResult}", Severity.Error);
#else
                            _snackBar?.Add("Ошибка чтения данных. Обратитесь к администраторам.", Severity.Error);
#endif
                        }
                    }
                }
            }
            finally
            {
                _loading = false;
            }
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (_dataGrid != null)
            {
                _needToRestorePage = _pageToRestore > 0;
                if (_needToRestorePage)
                {
                    SetCurrentPage(_pageToRestore);
                    _needToRestorePage = false;
                    _pageToRestore = 0;
                }
            }
        }

        protected void SetCurrentPage(int page)
        {
            if (_dataGrid != null)
            {
#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
                // Этот код "слизан" из исходников DataGrid - там это можно. Значит считаем что и здесь можно.
                _dataGrid.CurrentPage = page;
#pragma warning restore BL0005 // Component parameter should not be set outside of its component.
                _dataGrid.GroupItems();
            }
        }

        private string SelectedRowClassFunc(TEntity element, int rowNumber)
        {
            if (CurrentEntity != null)
            {
                if (CurrentEntity.Equals(element))
                {
                    _currentRowNumber = rowNumber;
                    return "selected";
                }
            }

            return string.Empty;
        }

        private void OnRowClick(DataGridRowClickEventArgs<TEntity> eventArgs)
        {
            _prevCurrentEntity = CurrentEntity;
            if (eventArgs.RowIndex == _currentRowNumber)
            {
                CurrentEntity = null;
                _currentRowNumber = -1;
            }
            else
            {
                CurrentEntity = eventArgs.Item;
                _currentRowNumber = eventArgs.RowIndex;
            }
        }

        private async Task OnDblClick(MouseEventArgs e)
        {
            if (_dataGrid != null)
            {
                CurrentEntity = CurrentEntity ?? _prevCurrentEntity;
                //TEntity? entityToEdit = CurrentEntity ?? _prevCurrentEntity;
                if (CurrentEntity != null)
                {
                    if (OnRowMouseDoubleClickAsync != null)
                    {
                        await OnRowMouseDoubleClickAsync(CurrentEntity);
                    }
                    else
                    {
                        if (ShowEditButton)
                        {
                            await EditAsync(CurrentEntity);
                        }
                    }
                }
            }
        }

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (_dataGrid != null)
            {
                switch (e.Key)
                {
                    case "ArrowUp":
                        MoveCurrentRecord(-1);
                        break;
                    case "ArrowDown":
                        MoveCurrentRecord(1);
                        break;
                    case "PageUp":
                        MoveCurrentRecord(-1 * _dataGrid.RowsPerPage);
                        break;
                    case "PageDown":
                        MoveCurrentRecord(_dataGrid.RowsPerPage);
                        break;
                    case "Home":
                        MoveCurrentRecord(0, 0);
                        break;
                    case "End":
                        MoveCurrentRecord(0, _dataGrid.FilteredItems.Count() - 1);
                        break;
                    case "Enter":
                        if (ShowEditButton && CurrentEntity != null)
                        {
                            await EditAsync(CurrentEntity);
                        }

                        break;
                    case "Del":
                        if (ShowDeleteButton && CurrentEntity != null)
                        {
                            await DeleteAsync(CurrentEntity);
                        }

                        break;
                    case " ":
                        if (CurrentEntity != null)
                        {
                            if (_selectedEntities.Contains(CurrentEntity))
                                _selectedEntities.Remove(CurrentEntity);
                            else
                                _selectedEntities.Add(CurrentEntity);
                            MoveCurrentRecord(1);
                        }

                        break;
                    default:
                        return;
                }

                StateHasChanged();
            }
        } 
        
        private void MoveCurrentRecord(int relativeIndex, int absoluteIndex = -1)
        {
            if (CurrentEntity != null && _dataGrid != null)
            {
                if (absoluteIndex == -1)
                {
                    int currentIndex = _dataGrid.FilteredItems.ToList().IndexOf(CurrentEntity);
                    absoluteIndex = currentIndex + relativeIndex;
                }

                int maxIndex = _dataGrid.FilteredItems.Count() - 1;
                if (absoluteIndex > maxIndex)
                {
                    absoluteIndex = maxIndex;
                }
                else if (absoluteIndex < 0)
                {
                    absoluteIndex = 0;
                }

                TEntity? nextItem = _dataGrid.FilteredItems.ElementAtOrDefault(absoluteIndex);
                if (nextItem != null)
                {
                    CurrentEntity = nextItem;
                    int newPage = absoluteIndex / _dataGrid.RowsPerPage;
                    SetCurrentPage(newPage);
                }
            }
        }

        protected virtual async Task ProcessMenuOperationAsync(string menuName,
            EntitiesListOperation<TEntity> menuValue)
        {
            try
            {
                if (ActiveRepository != null)
                {
                    if (menuValue.ActiveOnCurrent && CurrentEntity != null)
                    {
                        menuValue.Action?.Invoke(menuName, [CurrentEntity], ActiveRepository);
                        if (menuValue.ActionAsync != null)
                        {
                            await menuValue.ActionAsync(menuName, [CurrentEntity], ActiveRepository);
                        }
                        _entitiesToReload.Add(CurrentEntity);
                    }
                    else if (menuValue.ActiveOnSelected && _selectedEntities.Count > 0)
                    {
                        menuValue.Action?.Invoke(menuName, _selectedEntities, ActiveRepository);
                        if (menuValue.ActionAsync != null)
                            await menuValue.ActionAsync(menuName, _selectedEntities, ActiveRepository);
                        foreach (var entity in _selectedEntities) _entitiesToReload.Add(entity);
                    }
                    else if (menuValue.ActiveOnNoCurrentAndSelected && _selectedEntities.Count == 0 &&
                             CurrentEntity == null)
                    {
                        menuValue.Action?.Invoke(menuName, _selectedEntities, ActiveRepository);
                        if (menuValue.ActionAsync != null)
                            await menuValue.ActionAsync(menuName, _selectedEntities, ActiveRepository);
                        foreach (var entity in _selectedEntities) _entitiesToReload.Add(entity);
                    }
                    else if (menuValue.ActiveAlways)
                    {
                        if (_selectedEntities.Count > 0)
                        {
                            menuValue.Action?.Invoke(menuName, _selectedEntities, ActiveRepository);
                            if (menuValue.ActionAsync != null)
                                await menuValue.ActionAsync(menuName, _selectedEntities, ActiveRepository);
                            foreach (var entity in _selectedEntities) _entitiesToReload.Add(entity);
                        }
                        else if (CurrentEntity != null)
                        {
                            menuValue.Action?.Invoke(menuName, [CurrentEntity], ActiveRepository);
                            if (menuValue.ActionAsync != null)
                                await menuValue.ActionAsync(menuName, [CurrentEntity], ActiveRepository);
                            _entitiesToReload.Add(CurrentEntity);
                        }
                        else
                        {
                            menuValue.Action?.Invoke(menuName, [], ActiveRepository);
                            if (menuValue.ActionAsync != null)
                                await menuValue.ActionAsync(menuName, [], ActiveRepository);
                            _reloadAll = true;
                        }
                    }
                    await CheckAndReload();
                }
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при выполнении операции: {ex.Message}";
                _logger?.LogError(ex, "{message}", message);
                _snackBar?.Add(message, Severity.Error);
            }
        }

        private MarkupString BuildTitle()
        {
            string title = (DynamicTitle != null) ? DynamicTitle() : Title;
            return new MarkupString(title);
        }

        private async Task NavigateToNewTab(string strUri)
        {
            if (_jsRuntime != null)
            {
                await _jsRuntime.InvokeVoidAsync("NavigateToNewTab", CancellationToken.None, strUri);
            }
        }

        private async Task LoadSavedFiltersAsync()
        {
            // Фильтры грузим только в том случае, если не переопределен сам механизм (метод) чтения данных.
            // Если чтение переопределено - фильтрация полностью отдается на откуп стороннему разработчику.
            // Также фильтры не читаем и не отображаем для режима работы OperateInMemory
            if (OnLoadDataAsync == null && _serviceObjectStorage != null && _operateInMemory == false)
            {
                Result<List<SavedFilter>> resList = await _serviceObjectStorage.GetListAsync<SavedFilter>(q =>
                    q.Where(x => x.EntityTypeName.Contains(typeof(TEntity).Name)));
                if (resList.IsError || resList.Value == null)
                {
                    _snackBar?.Add($"Ошибка при получении списка сохраненных фильтров: {resList.ErrorResult}",
                        Severity.Error);
                    return;
                }

                _savedFilters = resList.Value;
                if (_savedFilters.Count > 0)
                {
                    _savedFilters.Add(new SavedFilter(){Id = 0, Name = "Удалить фильтр"});
                }
            }
        }

        private async Task LoadScriptCommandsAsync()
        {
            if (_serviceObjectStorage != null)
            {
                Result<List<ScriptCommand>> resList = await _serviceObjectStorage.GetListAsync<ScriptCommand>(q =>
                    q.Where(x =>
                        x.EntityTypeName.Contains(typeof(TEntity).Name)));
                if (resList.IsError || resList.Value == null)
                {
                    _snackBar?.Add($"Ошибка при получении списка автоопераций: {resList.ErrorResult}", Severity.Error);
                    return;
                }

                _scriptCommands = resList.Value;
            }
        }

        private async Task LoadReportsAsync()
        {
            if (_serviceObjectStorage != null)
            {
                Result<List<FrameReport>> resList = await _serviceObjectStorage.GetListAsync<FrameReport>(q => q
                    .Include(x => x.FrameReportType)
                    .Include(x => x.ReportTemplate)
                    .Include(x => x.SavedQuery)
                    .Include(x => x.ScriptCommand)
                    .Where(x => x.EntityTypeName.Contains(typeof(TEntity).Name)));
                if (resList.IsError || resList.Value == null)
                {
                    _snackBar?.Add($"Ошибка при получении списка отчетов: {resList.ErrorResult}", Severity.Error);
                    return;
                }

                _reports = resList.Value;
            }
        }

        private void InitOperationsList()
        {
            // 1. Добавляем список операций, определенных в коде
            if (Operations != null)
            {
                foreach (var kvp in Operations)
                {
                    _operations.Add(kvp.Key, kvp.Value);
                }
            }

            // 2. Добавляем список автоопераций.
            if (_scriptCommands.Count > 0)
            {
                if (_operations.Count > 0)
                {
                    _operations.Add("Div1", new EntitiesListOperation<TEntity>() { Divider = true });
                }

                foreach (var cmd in _scriptCommands)
                {
                    EntitiesListOperation<TEntity> operation = new EntitiesListOperation<TEntity>()
                    {
                        ActiveAlways = true,
                        ActionAsync = (async (operationName, setEntities, repository) =>
                            await ExecuteOperationScriptCommand(operationName, setEntities, repository))
                    };

                    _operations.Add(cmd.ScriptName, operation);
                }
            }
        }

        private async Task ExecuteOperationScriptCommand(string operationName, HashSet<TEntity> setEntities,
            IBaseRepository<TEntity> repository)
        {
            // Имя операции здесь совпадает с именем ScriptCommand
            ScriptCommand? cmd = _scriptCommands.FirstOrDefault(sc => sc.ScriptName == operationName);
            if (cmd == null)
            {
                string err =
                    $"Для выбраной операции {operationName} в списке не найдено соответствующей скрипт-операции";
                _logger?.LogError(err);
                _snackBar?.Add(err, Severity.Error);
                return;
            }

            if (_dialogService == null)
            {
                string err = $"Отсутствует DialogService";
                _logger?.LogError(err);
                _snackBar?.Add(err, Severity.Error);
                return;
            }

            if (_scriptCore == null)
            {
                string err = $"Отсутствует ScriptCore";
                _logger?.LogError(err);
                _snackBar?.Add(err, Severity.Error);
                return;
            }

            if (ActiveObjectStorage == null)
            {
                string err = $"Не удалось получить ActiveObjectStorage";
                _logger?.LogError(err);
                _snackBar?.Add(err, Severity.Error);
                return;
            }

            await Helpers.ExecuteScriptCommandAsync(cmd, setEntities, ActiveObjectStorage,
                _dialogService, _scriptCore, _logger, _snackBar);
        }

        private async Task OnSavedFilterChangedAsync(SavedFilter? newFilter)
        {
            try
            {
                if (newFilter != null && newFilter.ParamListJson.Length > 0)
                {
                    if (ActiveObjectStorage != null && ActiveObjectStorage.IsDirty)
                    {
                        if (_dialogService == null)
                        {
                            string err = "OnSavedFilterChangedAsync: нужно подтверждение, а не установлен DialogService";
                            _logger?.LogError(err);
                            _snackBar?.Add(err, Severity.Error);
                            return;
                        }

                        bool? result = await _dialogService.ShowMessageBox(
                            "Подтверждение",
                            "При установке или отмене фильтра все сделанные изменения будут потеряны. Продолжать?",
                            yesText: "Да", noText: "Отмена");

                        if (result != true)
                        {
                            return;
                        }
                    }

                    if (newFilter == _savedFilter)
                    {
                        // Если фильтр тот же самый - сохраняем текущие значения списка параметров,
                        // чтобы пользователь увидел то, что ввел ранее.
                        newFilter.ParamListJson = _savedFilter.ParamListJson;
                    }

                    ParamList paramList = new ParamList();
                    paramList.FromJson(newFilter.ParamListJson);
                    bool bContinue = await Helpers.FillParamListByUser(paramList,
                        _snackBar,
                        _dialogService,
                        _logger,
                        "Параметры фильтра");
                    if (!bContinue)
                    {
                        return;
                    }

                    newFilter.ParamListJson = paramList.ToJson();
                }

                _savedFilter = (newFilter == null || newFilter.Id == 0) ? null : newFilter;
                await RefreshAsync();
                await SaveUserProfileAsync();
            }
            catch (Exception ex)
            {
                string err = $"Ошибка при установке фильтра: {ex.Message}";
                _logger?.LogError(ex, "{Err}", err);
                _snackBar?.Add(err, Severity.Error);
            }
        }

        private async Task PrepareReportAsync()
        {
            if (_reports.Count == 0)
                return;

            FrameReport? report;

            if (_reports.Count > 1)
            {
                // Диалог показываем только если отчетов больше 1, если только 1 - сразу запускаем.
                Result<FrameReport> resReport = await Helpers.ShowSelectDialogAsync<FrameReport>(_reports,
                    _dialogService, _snackBar,
                    "Выберите отчет", "Отчеты не найдены");

                // Если ошибка или ничего не выбрано - выходим
                if (resReport.IsError || resReport.Value == null)
                {
                    return;
                }

                report = resReport.Value;
            }
            else
            {
                report = _reports[0];
            }

            // Список записей, которые будут переданы для подготовки отчета:
            // выделенные, если таковых нет - то текущая, если такой тоже нет - то null 
            List<TEntity>? entities = null;

            if (_selectedEntities.Count > 0)
            {
                entities = _selectedEntities.ToList();
            }
            else if (CurrentEntity != null)
            {
                entities = [CurrentEntity];
            }

            // Запускаем процесс построения отчета
            await Helpers.ExecuteReportAsync(report, entities, _frameReporting, _snackBar, _jsRuntime,
                _logger, _dialogService);
        }

        private async Task LoadUserProfileAsync()
        {
            if (_userCore == null || _userCore.CurrentUser == null || ActiveObjectStorage == null)
            {
                return;
            }

            User user = _userCore.CurrentUser;
            string uiParamsJson = user.UserProfile.UIParamsJson;
            if (uiParamsJson.Length == 0)
            {
                return;
            }

            try
            {
                UserUiParams uiParams = UserUiParams.FromJson(uiParamsJson);
                if (uiParams.EntitiesListDetailSettings.TryGetValue(ComponentName, out var settings))
                {
                    if(settings.RowsPerPage > 0) _rowsPerPage = settings.RowsPerPage;
                    if (settings.FilterId > 0)
                    {
                        _rowsPerPage = (settings.RowsPerPage > 0) ? settings.RowsPerPage : DEFAULT_ROWS_PER_PAGE;
                        Result<SavedFilter> resFilter =
                            await ActiveObjectStorage.GetObjAsync<SavedFilter>(spec =>
                                spec.Where(f => f.Id == settings.FilterId));
                        if (resFilter.IsError || resFilter.Value == null)
                        {
                            string err =
                                $"Ошибка чтения фильтра, заданного в параметрах интерфейса пользователя: {resFilter.ErrorResult}";
                            _logger?.LogError(err);
                            _snackBar?.Add(err, Severity.Error);
                            return;
                        }

                        _savedFilter = resFilter.Value;
                        if (settings.FilterParamListJson.Length > 0)
                        {
                            _savedFilter.ParamListJson = settings.FilterParamListJson;
                        }
                    }
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                string err = $"Ошибка чтения параметров интерфейса пользователя: {ex.Message}";
                _logger?.LogError(ex, err);
                _snackBar?.Add(err, Severity.Error);
            }
        }

        private async Task SaveUserProfileAsync()
        {
            if (_userCore == null || _userCore.CurrentUser == null || ActiveObjectStorage == null)
            {
                return;
            }

            try
            {
                EntitiesListDetailSettings listDetailSettings = new()
                {
                    FilterId = _savedFilter?.Id ?? null,
                    FilterParamListJson = _savedFilter?.ParamListJson ?? "",
                    RowsPerPage = _rowsPerPage
                };
                UserUiParams uiParams = UserUiParams.FromJson(_userCore.CurrentUser.UserProfile.UIParamsJson);
                uiParams.EntitiesListDetailSettings[ComponentName] = listDetailSettings;
                _userCore.CurrentUser.UserProfile.UIParamsJson = UserUiParams.ToJson(uiParams);
                Result resSave = await _userCore.SaveUserProfileAsync();
                if (resSave.IsError)
                {
                    string err = $"Ошибка при сохранении профиля пользователя: {resSave.ErrorResult}";
                    _snackBar?.Add(err, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                string err = $"Ошибка чтения параметров интерфейса пользователя: {ex.Message}";
                _logger?.LogError(ex, err);
                _snackBar?.Add(err, Severity.Error);
            }
        }
        
        private async Task _onRowsPerPageChangedAsync(int rowsCount)
        {
            _rowsPerPage = rowsCount;
            await SaveUserProfileAsync();
        }


        #endregion
    }
}

///// <summary>
///// Сигнал о том, что объект готов к удалению. Вызывается перед удалением объекта. 
///// Не вызывается если определен <see cref="OnDeleteAsync"/>
///// </summary>
//[Parameter] public Action<TEntity, IBaseRepository<TEntity>?>? OnBeforeDelete { get; set; }
//// <summary>
///// Используется для формирования списка колонок грида
///// </summary>
//[Parameter] public RenderFragment? ChildContent { get; set; }