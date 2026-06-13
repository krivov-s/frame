// using Frame.App.Cores;
// using Frame.App.IEntityRepositories;
// using Frame.App.Security;
// using Frame.Shared;
// using Microsoft.AspNetCore.Components;
// using Microsoft.AspNetCore.Components.Web;
// using Microsoft.JSInterop;
// using MudBlazor;
// using SmartFormat;
// using Microsoft.Extensions.Logging;
// using Frame.WebLib.Shared;
// using Microsoft.Extensions.DependencyInjection;
// using Frame.Domain.Entities.Core;
//
// // TODO: Экспорт данных в Excel https://github.com/radzenhq/radzen-blazor/blob/master/RadzenBlazorDemos.Host/Controllers/ExportController.cs
//
// namespace Frame.WebLib.Components.Shared
// {
//     public partial class EntitiesList<TEntity, TParentComponent> //where TEntity : BaseEntity, IBaseGenericEntity<TEntity>
//     {
//         [Inject] protected IServiceProvider? Services { get; set; }
//         [Inject] IGetCurrentUserNameService? GetCurrentUserService { get; set; }
//         [Inject] IDialogService? DialogService { get; set; }
//         [Inject] IJSRuntime? JsRuntime { get; set; }
//         [Inject] ISnackbar? SnackBar { get; set; }
//         [Inject] ILogger<TParentComponent>? Logger { get; set; }
//         [Inject] IUserCore? UserCore { get; set; }
//
//         /// <summary>
//         /// Заголовок списка
//         /// </summary>
//         [Parameter] public string Title { get; set; } = "";
//         /// <summary>
//         /// Функция динамического получения заголовка списка
//         /// </summary>
//         [Parameter] public Func<string>? DynamicTitle { get; set; }
//         /// <summary>
//         /// Стиль шрифта заголовка. По-умолчанию <see cref="Typo.h6"/>
//         /// </summary>
//         [Parameter] public Typo TitleTypo { get; set; } = Typo.h6;
//         
//         // <summary>
//         /// Используется для формирования списка колонок грида
//         /// </summary>
//         [Parameter] public RenderFragment? ChildContent { get; set; }
//         [Parameter] public bool UseSecuritySettings { get; set; } = true;
//         [Parameter] public bool ShowTitle { get; set; } = true;
//         [Parameter] public bool ShowAddButton { get; set; } = true;
//         [Parameter] public bool ShowEditButton { get; set; } = true;
//         [Parameter] public bool ShowDeleteButton { get; set; } = true;
//         [Parameter] public bool ShowDeleteSelectedButton { get; set; } = true;
//         [Parameter] public bool ShowAuditButton { get; set; } = true;
//         [Parameter] public string TooltipAddButton { get; set; } = "Добавить новую запись";
//         [Parameter] public string TooltipEditButton { get; set; } = "Редактировать текущую запись";
//         [Parameter] public string TooltipDeleteButton { get; set; } = "Удалить текущую запись";
//         [Parameter] public string TooltipDeleteSelectedButton { get; set; } = "Удалить выделенные";
//         [Parameter] public string TooltipRefreshListButton { get; set; } = "Обновить список";
//         [Parameter] public string TooltipAuditButton { get; set; } = "Аудит текущей записи";
//         /// <summary>
//         /// Список операций, которые будут показаны в меню и могут быть выполнены с выделенными записями.
//         /// <para><typeparamref name="string"/>  - имя меню</para>
//         /// <para><typeparamref name="List"/> - список отмеченных элементов списка TEntity, который будет передан в Action</para>
//         /// </summary>
//         [Parameter] public Dictionary<string, EntitiesListOperation<TEntity>>? Operations{ get; set; }
//         // public delegate Result<List<TComponent>> LoadDataDelegate(IBaseRepository<TComponent> repository);
//         /// <summary>
//         /// Делегат, который вызвается для получения данных от репозитория.
//         /// Может быть определен пользователем если требуется доп. фильтрация, сортировка и т.п.
//         /// Вызывается внутри метода <see cref="LoadDataAsync">LoadDataAsync</see>
//         /// </summary>
//         [Parameter] public Func<IBaseRepository<TEntity>, Task<Result<List<TEntity>>>>? OnLoadDataAsync { get; set; }
//         //[Parameter] public LoadDataDelegateAsync? OnLoadDataAsync { get; set; }
//         /// <summary>
//         /// Конфигурирование секции Select запроса. Используется при чтении данных, если не задано OnLoadDataAsync.
//         /// <para>Внимание! Если задается - то полностью переопределяет ConfigureSelect, который был задан (если был задан) при регистрации репозитория</para>
//         /// </summary>
//         [Parameter] public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureSelect { get; set; }
//         /// <summary>
//         /// Конфигурирование секции Where запроса. Используется при чтении данных, если не задано OnLoadDataAsync
//         /// <para>Внимание! Если задается - то полностью переопределяет ConfigureWhere, который был задан (если был задан) при регистрации репозитория</para>
//         /// </summary>
//         [Parameter] public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureWhere { get; set; } = null;
//         /// <summary>
//         /// Конфигурирование секции OrderBy запроса. Используется при чтении данных, если не задано OnLoadDataAsync
//         /// <para>Внимание! Если задается - то полностью переопределяет ConfigureOrderBy, который был задан (если был задан) при регистрации репозитория</para>
//         /// </summary>
//         [Parameter] public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureOrderBy { get; set; }
//
//         /// <summary>
//         /// Переопределяет внутренний метод добавления нового элемента
//         /// </summary>
//         [Parameter] public Func<IBaseRepository<TEntity>, Task<Result<TEntity>>>? OnAddNewAsync { get; set; }
//         //[Parameter] public AddNewDelegateAsync? OnAddNewAsync { get; set; }
//         /// <summary>
//         /// Переопределяет внутренний метод редактирования выделенного элемента
//         /// </summary>
//         [Parameter] public Func<IBaseRepository<TEntity>, TEntity, Task<Result>>? OnEditAsync { get; set; }
//         //[Parameter] public EditDelegateAsync? OnEditAsync { get; set; }
//         /// <summary>
//         /// Переопределяет внутренний метод удаления выделенного элемента
//         /// </summary>
//         [Parameter] public Func<IBaseRepository<TEntity>, TEntity, Task<Result>>? OnDeleteAsync { get; set; }
//         //[Parameter] public DeleteDelegateAsync? OnDeleteAsync { get; set; }
//         /// <summary>
//         /// Двойной клик мыши по строке. Если делегат не определен - будет считаться что это редактирование текущей строки
//         /// </summary>
//         [Parameter] public Func<TEntity, Task>? OnRowMouseDoubleClickAsync { get; set; }
//         //[Parameter] public MouseRowDoubleClickAsync? OnRowMouseDoubleClickAsync { get; set; }
//         /// <summary>
//         /// Uri страницы, которая будет вызываться при нажатии на кнопку AddNew (если не задан делегат OnAddNewAsync)
//         /// </summary>
//         [Parameter] public string AddNewEntityUri { get; set; } = "{TEntity.TypeName}/0";
//         /// <summary>
//         /// Uri страницы редактирования текущей записи. 
//         /// <para>Uri прогоняется через Smart.Format, с возможностью получения атрибутов выбранного объекта через Entity, наименования типа через TEntity.TypeName. 
//         /// Пример допустимого Uri: replan_line?REPlanId=1&REPlanLineId=2
//
//         /// Например: replan/{Entity.Id}</para>
//         /// </summary>
//         [Parameter] public string EditEntityUri { get; set; } = "{TEntity.TypeName}/{Entity.Id}";
//         /// <summary>
//         /// Uri страницы аудита текущей записи. По-умолчанию страница аудита - audit/тип объекта/id объекта
//         /// <para>Uri прогоняется через Smart.Format, с возможностью получения атрибутов выбранного объекта через Entity, наименования типа через TEntity.TypeName.
//         /// Например: replan/{Entity.Id}</para>
//         /// </summary>
//         [Parameter] public string AuditEntityUri { get; set; } = "audit/{TEntity.TypeName}/{Entity.Id}";
//         [Parameter] public EntitiesListEditMode EditMode { get; set; } = EntitiesListEditMode.ExternalUri;
//         [Parameter] public TEntity? CurrentEntity { get; set; }
//         /// <summary>
//         /// Объектное хранилище, которое будет использоваться для чтения данных в гриде. Если не задано - получит новое от DI.
//         /// </summary>
//         [Parameter] public IObjectStorage? ObjectStorage { get; set; }
//
//         private int _currentRowNumber = -1;
//         private TEntity? _prevCurrentEntity = null; // Предыдущее значение CurrentEntity - для корректного отслеживания Mouse Dblclick
//
//         private MudDataGrid<TEntity>? _dataGrid;
//         protected string CurrentUserName { get; set; } = "";
//         protected IBaseRepository<TEntity>? Repository { get; set; }
//         //private List<TEntity> EntitiesList = [];
//         private HashSet<TEntity> _entities = [];
//         private HashSet<TEntity> _selectedEntities = [];
//         private bool _reloadAll = false;
//         private List<TEntity> _entitiesToReload = [];
//         private bool _dataGridReadOnly = true;
//         private bool _loading = false;
//         private bool _needToRestorePage = false;
//         private int _pageToRestore = 0;
//         private bool _isAuthorized = false;
//         private bool _canViewTEntity = false; // Разрешен просмотр объектов данного типа или нет.
//
//         protected IObjectStorage? _objectStorage = null;
//         protected IObjectStorage ActiveObjectStorage
//         {
//             get
//             {
//                 if (_objectStorage == null)
//                 {
//                     if(ObjectStorage != null)
//                     {
//                         _objectStorage = ObjectStorage;
//                     }
//                     else
//                     {
//                         if (Services == null)
//                         {
//                             throw new Exception("Компонент не инициализирован! Отсутствует Services!");
//                         }
//
//                         _objectStorage = Services.GetService<IObjectStorage>();
//                         if (_objectStorage == null)
//                         {
//                             throw new Exception("Ошибка получения IObjectStorage от Services!");
//                         }
//                     }
//                 }
//                 return _objectStorage;
//             }
//         }
//
//
//         //protected bool IsDataGridVisible = true;
//         //protected void TogglleDataGrid() => IsDataGridVisible = !IsDataGridVisible;
//
//         /// <summary>
//         /// Обновление содержимого списка из источника данных
//         /// </summary>
//         /// <returns></returns>
//         public async Task RefreshAsync()
//         {
//             await LoadDataAsync();
//             StateHasChanged();
//         }
//
//         /// <summary>
//         /// Количество строк в гриде
//         /// </summary>
//         /// <returns></returns>
//         public int RecordCount { get => _entities.Count; }
//
//         protected MarkupString BuildTitle()
//         {
//             string title = (DynamicTitle != null) ? DynamicTitle() : Title;
//             return new MarkupString(title);
//         }
//
//         protected override async Task OnInitializedAsync()
//         {
//             _isAuthorized = await AuthHelpers.CheckAuthorized(GetCurrentUserService, Logger, SnackBar); /*NavigationManager*/
//
//             if (_isAuthorized)
//             {
//                 _canViewTEntity = UserCore?.CanView<TEntity>() ?? false;
//                 _dataGridReadOnly = EditMode != EntitiesListEditMode.InternalDialog;
//
//                 if (EditMode == EntitiesListEditMode.ReadOnly)
//                 {
//                     ShowEditButton = false;
//                     ShowDeleteButton = false;
//                     ShowAddButton = false;
//                 }
//                 else
//                 {
//                     if (UseSecuritySettings == true && UserCore != null)
//                     {
//                         // Если на входе кнопки заданы администратором как false (не показывать) - тогда и собственно не нужно ничего проверять у UserCore
//                         ShowAddButton = ShowAddButton ? UserCore.CanAdd<TEntity>() : false;
//                         ShowEditButton = ShowEditButton ? UserCore.CanModify<TEntity>() : false;
//                         ShowDeleteButton = ShowDeleteButton ? UserCore.CanDelete<TEntity>() : false;
//                         ShowAuditButton = ShowAuditButton ? UserCore.CanAudit<TEntity>() : false;
//                     }
//                 }
//
//                 // pageHistoryState.AddPageToHistory(NavigationManager.ToBaseRelativePath(NavigationManager.Uri));
//                 if (GetCurrentUserService != null)
//                 {
//                     CurrentUserName = await GetCurrentUserService.GetLoginAsync();
//                 }
//
//                 if (CurrentUserName.Length == 0)
//                 {
//                     SnackBar?.Add("Вы не авторизованы на данной странице", Severity.Warning);
//                     _isAuthorized = false;
//                     //NavigationManager?.NavigateTo("Login");
//                 }
//                 else
//                 {
//                     await LoadDataAsync();
//                 }
//             }
//         }
//
//         // protected override void OnInitialized()
//         // {
//         //     if (OnLoadData != null && Repository != null)
//         //     {
//         //         Result<List<TComponent>> result = OnLoadData(Repository);
//         //         if (!result.IsError)
//         //         {
//         //             EntitiesList = (result.Value != null) ? result.Value : [];
//         //             _reloadAll = false;
//         //             SnackBar?.Add("Данные загружены", Severity.Info,
//         //                 opt =>
//         //                 {
//         //                     opt.ShowTransitionDuration = 500;
//         //                     opt.VisibleStateDuration = 2000;
//         //                     opt.SnackbarVariant = Variant.Outlined;
//         //                 }
//         //                 );
//         //         }
//         //     }
//         // }
//
//         private async Task AddNewAsync()
//         {
//             if (EditMode != EntitiesListEditMode.ReadOnly)
//             {
//                 // Если задан делегат функции добавления - вызываем его. Внутреннее добавление не используем.
//                 if (OnAddNewAsync != null && Repository != null)
//                 {
//                     Result<TEntity> res = await OnAddNewAsync(Repository);
//                     if (res.IsError)
//                     {
//                         SnackBar?.Add(res.ErrorResult, Severity.Error);
//                     }
//                     else
//                     {
//                         // Если объект успешно создан - добавляем его в список перезачитки
//                         await RefreshAsync();
//                         // TComponent? entity = res.Value;
//                         // if (entity != null)
//                         // {
//                         //     _entitiesToReload.Add(entity);
//                         // }
//                     }
//                 }
//                 else
//                 {
//                     if (JsRuntime != null && EditMode == EntitiesListEditMode.ExternalUri)
//                     {
//                         if (AddNewEntityUri.Length == 0)
//                         {
//                             SnackBar?.Add("Команда добавления не сконфигурирована корректно: не установлен параметр AddNewEntityUri", Severity.Warning);
//                             return;
//                         }
//                         else
//                         {
//                             //await JsRuntime.InvokeVoidAsync("NavigateToNewTab", CancellationToken.None, AddNewEntityUri);
//                             try
//                             {
//                                 string strUri = Smart.Format(AddNewEntityUri, BuildArgsForSmartFormat(CurrentEntity));
//                                 await NavigateToNewTab(strUri);
//                                 _entitiesToReload.Clear();
//                                 _reloadAll = true;
//                             }
//                             catch(Exception ex)
//                             {
//                                 SnackBar?.Add(ex.Message, Severity.Error);
//                                 Logger?.LogError(ex, "Ошибка обработки команды создания нового объекта");
//                             }
//                         }
//                     }
//                 }
//             }
//         }
//
//         private static object BuildArgsForSmartFormat(TEntity? entity)
//         {
//             return new { Entity = entity, TEntity = new { TypeName = typeof(TEntity).Name } };
//         }
//
//         private async Task EditAsync(TEntity entity)
//         {
//             if (EditMode != EntitiesListEditMode.ReadOnly)
//             {
//                 // Если задан делегат функции редактирования - вызываем его. Внутреннее редактирование не используем.
//                 if (OnEditAsync != null && Repository != null)
//                 {
//                     Result res = await OnEditAsync(Repository, entity);
//                     if (res.IsError)
//                     {
//                         SnackBar?.Add(res.ErrorResult, Severity.Error);
//                     }
//                     else
//                     {
//                         // Если объект успешно отредактирован - добавляем его в список перезачитки
//                         _entitiesToReload.Add(entity);
//                     }
//                 }
//                 else
//                 {
//                     if (EditMode == EntitiesListEditMode.ExternalUri)
//                     {
//                         if (entity != null && JsRuntime != null)
//                         {
//                             if (EditEntityUri.Length == 0)
//                             {
//                                 SnackBar?.Add("Команда редактирования не сконфигурирована корректно: не установлен параметр EditEntityUri", Severity.Warning);
//                                 return;
//                             }
//                             else
//                             {
//                                 try
//                                 {
//                                     string strUri = Smart.Format(EditEntityUri, BuildArgsForSmartFormat(entity));
//                                     //string strUri = $"{EditEntityUri}/{entity.Id}";
//                                     await NavigateToNewTab(strUri); 
//                                     //await JsRuntime.InvokeVoidAsync("NavigateToNewTab", CancellationToken.None, strUri);
//                                     _entitiesToReload.Add(entity);
//                                 }
//                                 catch (Exception ex)
//                                 {
//                                     SnackBar?.Add(ex.Message);
//                                     Logger?.LogError(ex, "Ошибка обработки команды редактирования объекта");
//                                 }
//                             }
//                         }
//                     }
//                     else if (EditMode == EntitiesListEditMode.InternalDialog)
//                     {
//                     }
//                 }
//             }
//         }
//
//         private async Task ItemChangedAfterInternalEditing(TEntity entity)
//         {
//             if (Repository != null)        // EditMode == EntitiesListEditMode.InternalDialog &&
//             {
//                 Result result = await Repository.UpdateAsync(entity);
//                 if (result.IsError)
//                 {
//                     SnackBar?.Add($"Ошибка обновления: {result.ErrorResult}", Severity.Error);
//                     return;
//                 }
//                 else
//                 {
//                     SnackBar?.Add($"Изменения сохранены", Severity.Success);
//
//                     return;
//                 }
//             }
//         }
//
//         private async Task SelectedItemChangedAsync(TEntity entity)
//         {
//             _prevCurrentEntity = CurrentEntity;
//             CurrentEntity = entity;
//             await CheckAndReload();
//         }
//
//         private async Task SelectedItemsChangedAsync(HashSet<TEntity> items)
//         {
//             _selectedEntities = items;
//             await CheckAndReload();
//         }
//
//         private async Task CheckAndReload()
//         {
//             if (_reloadAll == true)
//             {
//                 await LoadDataAsync();
//             }
//             else if (_entitiesToReload.Count > 0)
//             {
//                 IBaseRepository<TEntity>? repo = Repository;
//                 if (repo != null)
//                 {
//                     foreach (TEntity ee in _entitiesToReload)
//                     {
//                         await repo.RefreshAsync(ee);
//                     }
//                     _entitiesToReload.Clear();
//                 }
//             }
//         }
//
//         private async Task DeleteAllAsync()
//         {
//             if (Repository != null && DialogService != null && SnackBar != null)
//             {
//                 if (_selectedEntities.Count > 0)
//                 {
//                     MarkupString markupString = new($"Вы уверены что хотите <b>удалить</b> все выделенные записи?");
//                     bool? result = await DialogService.ShowMessageBox(
//                         "Подтверждение",
//                         markupString,
//                         yesText: "Да, удалить!", noText: "Отмена");
//
//                     if (result == true)
//                     {
//                         foreach (TEntity entity in _selectedEntities)
//                         {
//                             Result r = await DeleteAsync(entity, false, false);
//                             if(r.IsError)
//                             {
//                                 return;
//                             }
//                         }
//                         Result rSave = await Repository.ObjectStorage.SaveChangesAsync();
//                         if (rSave.IsError)
//                         {
//                             SnackBar.Add($"Ошибка удаления: {rSave.ErrorResult}", Severity.Error);
//                         }
//                         else
//                         {
//                             SnackBar.Add($"Удаление завершено успешно");
//
//                             // Убираем удаленные записи из текущего списка и очищаем список выбранных
//                             foreach(TEntity entity in _selectedEntities)
//                             {
//                                 _entities.Remove(entity);
//                             }
//                             _selectedEntities.Clear();
//
//                             StateHasChanged();
//                         }
//                     }
//                 }
//                 else
//                 {
//                     await DialogService.ShowMessageBox("Удаление", "Записи для удаления не выдеделны!");
//
//                 }
//             }
//             else
//             {
//                 Logger?.LogError("OnDeleteAsync: отсутствуют один или более из обязательных параметров [entity, Repository, DialogService, SnackBar]");
//             }
//         }
//
//
//         private async Task<Result> DeleteAsync(TEntity entity, bool askConfirmation = true, bool pushChangesToObjectStorage = true)
//         {
//             if (EditMode != EntitiesListEditMode.ReadOnly)
//             {
//                 // Если задан делегат функции удаления - вызываем его. Внутреннее удаление не используем.
//                 if (OnDeleteAsync != null && Repository != null)
//                 {
//                     Result res = await OnDeleteAsync(Repository, entity);
//                     if (res.IsError)
//                     {
//                         SnackBar?.Add(res.ErrorResult, Severity.Error);
//                         return res;
//                     }
//                     else
//                     {
//                         // Если объект успешно удален - удаляем его из списка
//                         //if (EntitiesList.Exists(e => e.Id == entity.Id))
//                         if (_entities.Contains(entity))
//                         {
//                             _entities.Remove(entity);
//                             StateHasChanged();
//                             // await RefreshAsync();
//                         }
//                         return Result.Success;
//                     }
//                 }
//                 else
//                 {
//                     if (entity != null && Repository != null && DialogService != null && SnackBar != null)
//                     {
//                         bool doDelete = true;
//                         if (askConfirmation == true)
//                         {
//                             MarkupString markupString = new($"Вы уверены что хотите <b>удалить</b> {entity.Description}?");
//                             bool? result = await DialogService.ShowMessageBox(
//                                 "Подтверждение",
//                                 markupString,
//                                 yesText: "Да, удалить!", noText: "Отмена");
//
//                             doDelete = result ?? false;
//                         }
//
//                         if (doDelete == true)
//                         {
//                             Result res = await Repository.DeleteAsync(entity);
//                             if (res.IsError)
//                             {
//                                 SnackBar.Add(res.ErrorResult, Severity.Error);
//                                 return res;
//                             }
//                             else
//                             {
//                                 if(pushChangesToObjectStorage)
//                                 {
//                                     Result rSave = await Repository.ObjectStorage.SaveChangesAsync();
//                                     if(rSave.IsError)
//                                     {
//                                         SnackBar.Add($"Ошибка удаления: {rSave.ErrorResult}");
//                                     }
//                                     else
//                                     {
//                                         SnackBar.Add($"Запись ({entity.Description}) удалена успешно", Severity.Info);
//                                         _entities.Remove(entity);
//                                         StateHasChanged();
//                                     }
//                                 }
//                                 return res;
//                             }
//                         }
//                         else
//                         {
//                             return Result.Success;
//                         }
//                     }
//                     else
//                     {
//                         string strError = "OnDeleteAsync: отсутствуют один или более из обязательных параметров [entity, Repository, DialogService, SnackBar]";
//                         Logger?.LogError("{strError}", strError);
//                         return Result.Error(strError);
//                     }
//                 }
//             }
//             return Result.Success;
//         }
//
//         private async Task AuditAsync(TEntity entity)
//         {
//             if (entity != null && JsRuntime != null)
//             {
//                 if (AuditEntityUri.Length == 0)
//                 {
//                     SnackBar?.Add("Команда аудита текущей записи не сконфигурирована корректно: не установлен параметр AuditEntityUri", Severity.Warning);
//                     return;
//                 }
//                 else
//                 {
//                     try 
//                     { 
//                         string strUri = Smart.Format(AuditEntityUri, BuildArgsForSmartFormat(entity));
//                         await NavigateToNewTab(strUri);
//                     }
//                     catch (Exception ex)
//                     {
//                         SnackBar?.Add(ex.Message);
//                         Logger?.LogError(ex, "Ошибка обработки команды аудита объекта");
//                     }
//                 }
//             }
//         }
//
//         private async Task NavigateToNewTab(string strUri)
//         {
//             if(JsRuntime != null)
//             {
//                 await JsRuntime.InvokeVoidAsync("NavigateToNewTab", CancellationToken.None, strUri);
//             }
//         }
//
//         //private Task SaveChangesAsync()
//         //{
//         //    // Repository.
//         //    return Task.CompletedTask;
//         //}
//
//         private async Task LoadDataAsync()
//         {
//             if (ActiveObjectStorage != null && SnackBar != null)
//             {
//                 try
//                 {
//                     if (_dataGrid != null)
//                     {
//                         _pageToRestore = _dataGrid.CurrentPage;
//                         _needToRestorePage = _pageToRestore > 0 ? true : false;
//                     }
//                     _loading = true;
//
//                     // TODO: проверить, как grid отрабатывает обновление данных в случае, если работа идет из одного контекста. (см. ниже _objectStorage = null;)
//                     // Если не получить новый репозиторий - данные не обновятся из БД
//                     // Поэтому принудительно гасим старый IObjectStorage. При обращении к ActiveObjectStorage он будет вновь запрошен у Services
//                     // ----------------------------------
//                     _objectStorage = null;
//                     // ----------------------------------
//                     Result<IBaseRepository<TEntity>> resRepo = ActiveObjectStorage.GetBaseRepository<TEntity>();
//                     if (resRepo.IsError || resRepo.Value == null)
//                     {
//                         SnackBar.Add(resRepo.ErrorResult, Severity.Error);
//                         return;
//                     }
//                     Repository = resRepo.Value;
//
//                     Result<List<TEntity>> result;
//                     if (OnLoadDataAsync != null)
//                     {
//                         result = await OnLoadDataAsync(Repository);
//                     }
//                     else
//                     {
//                         Repository.ConfigureSelect = ConfigureSelect ?? Repository.ConfigureSelect;
//                         Repository.ConfigureWhere = ConfigureWhere ?? Repository.ConfigureWhere;
//                         Repository.ConfigureOrderBy = ConfigureOrderBy ?? Repository.ConfigureOrderBy;
//                         // Repository.UseLazyLoading = false;
//                         result = await Repository.GetAllAsync();
//                     }
//
//                     if (result.IsError == false)
//                     {
//                         _entities = (result.Value != null) ? new HashSet<TEntity>(result.Value) : []; //result.Value ?? ([]);
//                         _reloadAll = false;
//                         _entitiesToReload.Clear();
//                     }
//                     else
//                     {
//                         SnackBar.Add("Ошибка чтения данных", Severity.Error);
//                     }
//                 }
//                 finally
//                 {
//                     _loading = false;
//                 }
//             }
//         }
//
//         //protected override async Task OnAfterRenderAsync(bool firstRender)
//         //{
//         //    await CheckAndReload();
//         //}
//
//         protected override void OnAfterRender(bool firstRender)
//         {
//             if (_dataGrid != null)
//             {
//                 _needToRestorePage = _pageToRestore > 0 ? true : false;
//                 if (_needToRestorePage)
//                 {
//                     SetCurrentPage(_pageToRestore);
//                     _needToRestorePage = false;
//                     _pageToRestore = 0;
//                 }
//             }
//         }
//
//         protected void SetCurrentPage(int page)
//         {
//             if (_dataGrid != null)
//             {
// #pragma warning disable BL0005 // Component parameter should not be set outside of its component.
//                 // Этот код "слизан" из исходников DataGrid - там это можно. Значит считаем что и здесь можно.
//                 _dataGrid.CurrentPage = page;
// #pragma warning restore BL0005 // Component parameter should not be set outside of its component.
//                 _dataGrid.GroupItems();
//             }
//         }
//         private string SelectedRowClassFunc(TEntity element, int rowNumber)
//         {
//             if (CurrentEntity != null)
//             {
//                 if (CurrentEntity.Equals(element))
//                 {
//                     _currentRowNumber = rowNumber;
//                     return "selected";
//                 }
//             }
//             // if (_currentRowNumber == rowNumber)
//             // {
//             //     _currentRowNumber = -1;
//             //     return string.Empty;
//             // }
//             // else if (CurrentEntity != null)
//             // {
//             //     if (CurrentEntity.Equals(element))
//             //     {
//             //         _currentRowNumber = rowNumber;
//             //         return "selected";
//             //     }
//             // }
//             return string.Empty;
//         }
//
//         private void OnRowClick(DataGridRowClickEventArgs<TEntity> eventArgs)
//         {
//             _prevCurrentEntity = CurrentEntity;
//             if (eventArgs.RowIndex == _currentRowNumber)
//             {
//                 CurrentEntity = null;
//                 _currentRowNumber = -1;
//             }
//             else
//             {
//                 CurrentEntity = eventArgs.Item;
//                 _currentRowNumber = eventArgs.RowIndex;
//             }
//         }
//
//         private async Task OnDblClick(MouseEventArgs e)
//         {
//             if (_dataGrid != null)
//             {
//                 if (ShowEditButton)
//                 {
//                     TEntity? entityToEdit = CurrentEntity ?? _prevCurrentEntity;
//                     if(entityToEdit != null)
//                     {
//                         if (OnRowMouseDoubleClickAsync != null)
//                         {
//                             await OnRowMouseDoubleClickAsync(entityToEdit);
//                         }
//                         else
//                         {
//                             await EditAsync(entityToEdit);
//                         }
//                     }
//                 }
//             }
//         }
//
//         private async Task HandleKeyDown(KeyboardEventArgs e)
//         {
//             if (_dataGrid != null)
//             {
//                 switch (e.Key)
//                 {
//                     case "ArrowUp":
//                         MoveCurrentRecord(-1);
//                         break;
//                     case "ArrowDown":
//                         MoveCurrentRecord(1);
//                         break;
//                     case "PageUp":
//                         MoveCurrentRecord(-1 * _dataGrid.RowsPerPage);
//                         break;
//                     case "PageDown":
//                         MoveCurrentRecord(_dataGrid.RowsPerPage);
//                         break;
//                     case "Home":
//                         MoveCurrentRecord(0, 0);
//                         break;
//                     case "End":
//                         MoveCurrentRecord(0, _dataGrid.FilteredItems.Count() - 1);
//                         break;
//                     case "Enter":
//                         if (ShowEditButton && CurrentEntity != null)
//                         {
//                             await EditAsync(CurrentEntity);
//                         }
//                         break;
//                     case "Del":
//                         if (ShowDeleteButton && CurrentEntity != null)
//                         {
//                             await DeleteAsync(CurrentEntity);
//                         }
//                         break;
//                     case " ":
//                         if (CurrentEntity != null)
//                         {
//                             if (_selectedEntities.Contains(CurrentEntity))
//                                 _selectedEntities.Remove(CurrentEntity);
//                             else
//                                 _selectedEntities.Add(CurrentEntity);
//                             MoveCurrentRecord(1);
//                         }
//                         break;
//                     default:
//                         return;
//                 }
//                 StateHasChanged();
//             }
//         }
//
//         private void MoveCurrentRecord(int relativeIndex, int absoluteIndex = -1)
//         {
//             if (CurrentEntity != null && _dataGrid != null)
//             {
//                 if (absoluteIndex == -1)
//                 {
//                     int currentIndex = _dataGrid.FilteredItems.ToList().IndexOf(CurrentEntity);
//                     absoluteIndex = currentIndex + relativeIndex;
//                 }
//
//                 int maxIndex = _dataGrid.FilteredItems.Count() - 1;
//                 if (absoluteIndex > maxIndex)
//                 {
//                     absoluteIndex = maxIndex;
//                 }
//                 else if (absoluteIndex < 0)
//                 {
//                     absoluteIndex = 0;
//                 }
//                 //int firstVisibleIndex = _dataGrid.CurrentPage * _dataGrid.RowsPerPage;
//                 //int lastVisibleIndex = (_dataGrid.CurrentPage + 1) * _dataGrid.RowsPerPage - 1;
//                 //if (absoluteIndex > lastVisibleIndex) 
//                 //{
//                 //    absoluteIndex = lastVisibleIndex;
//                 //}
//                 //else if (absoluteIndex < firstVisibleIndex)
//                 //{
//                 //    absoluteIndex = firstVisibleIndex;
//                 //}
//
//                 TEntity? nextItem = _dataGrid.FilteredItems.ElementAtOrDefault(absoluteIndex);
//                 if (nextItem != null)
//                 {
//                     CurrentEntity = nextItem;
//                     int newPage = absoluteIndex / _dataGrid.RowsPerPage;
//                     SetCurrentPage(newPage);
//                 }
//             }
//         }
//
//         protected virtual async Task ProcessMenuOperationAsync(string menuName, EntitiesListOperation<TEntity> menuValue)
//         {
//             try
//             {
//                 if (Repository != null)
//                 {
//                     if (menuValue.ActiveOnCurrent && CurrentEntity != null)
//                     {
//                         menuValue.Action?.Invoke(menuName, [CurrentEntity], Repository);
//                         if (menuValue.ActionAsync != null) await menuValue.ActionAsync(menuName, [CurrentEntity], Repository);
//                     }
//                     else if (menuValue.ActiveOnSelected && _selectedEntities.Count > 0)
//                     {
//                         menuValue.Action?.Invoke(menuName, _selectedEntities, Repository);
//                         if (menuValue.ActionAsync != null) await menuValue.ActionAsync(menuName, _selectedEntities, Repository);
//                     }
//                     else if (menuValue.ActiveOnNoCurrentAndSelected && _selectedEntities.Count == 0 && CurrentEntity == null)
//                     {
//                         menuValue.Action?.Invoke(menuName, _selectedEntities, Repository);
//                         if (menuValue.ActionAsync != null) await menuValue.ActionAsync(menuName, _selectedEntities, Repository);
//                     }
//                     else if (menuValue.ActiveAlways)
//                     {
//                         if (_selectedEntities.Count > 0)
//                         {
//                             menuValue.Action?.Invoke(menuName, _selectedEntities, Repository);
//                             if (menuValue.ActionAsync != null) await menuValue.ActionAsync(menuName, _selectedEntities, Repository);
//                         }
//                         else if (CurrentEntity != null)
//                         {
//                             menuValue.Action?.Invoke(menuName, [CurrentEntity], Repository);
//                             if (menuValue.ActionAsync != null) await menuValue.ActionAsync(menuName, [CurrentEntity], Repository);
//                         }
//                         else
//                         {
//                             menuValue.Action?.Invoke(menuName, [], Repository);
//                             if (menuValue.ActionAsync != null) await menuValue.ActionAsync(menuName, [], Repository);
//                         }
//                     }
//                 }
//             }
//             catch (Exception ex)
//             {
//                 string message = $"Ошибка при выполнении операции: {ex.Message}";
//                 Logger?.LogError(ex, "{message}", message);
//                 SnackBar?.Add(message, Severity.Error);
//             }
//         }
//     }
// }