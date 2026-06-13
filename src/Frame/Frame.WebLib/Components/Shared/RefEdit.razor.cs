using Frame.App.IEntityRepositories;
using Frame.Domain;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Metadata;
using Frame.Domain.QuerySpec;
using Frame.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace Frame.WebLib.Components.Shared
{
    public partial class RefEdit<TEntity, TRefEntity>
    {
        #region ==================== Внешние сервисы ====================
        [Inject] IDialogService? _dialogService { get; set; }
        [Inject] ISnackbar? _snackBar { get; set; }
        [Inject] protected IObjectStorageProvider? _objectStorageProvider { get; set; }
        [Inject] private IJSRuntime? _jsRuntime { get; set; }
        //[Inject] protected IObjectStorage? NewObjectStorage { get; set; }
        #endregion

        private FieldIdentifier _fieldIdentifier;

        #region Параметры RefEdit
        /// <summary>
        /// Объектное хранилище, определенное родительской формой. Без него работать не будем.
        /// </summary>
        [CascadingParameter] IObjectStorage? _sharedObjectStorage { get; set; }

        [CascadingParameter] private EditContext? _editContext { get; set; }
        /// <summary>
        /// Основной объект, в котором изменяется атрибут
        /// </summary>
        [Parameter] public TEntity? Entity { get; set; }
        /// <summary>
        /// Изменяемый атрибут объекта (значеие свойства)
        /// </summary>
        [Parameter] public TRefEntity? Value { get; set; }
        
        // Параметры MudTextField, которые мы хотим сделать настраиваемыми
        [Parameter] public string Label { get; set; } = "Выберите значение";
        [Parameter] public Variant Variant { get; set; } = Variant.Outlined;
        [Parameter] public Margin Margin { get; set; } = Margin.None;
        [Parameter] public bool Required { get; set; }
        [Parameter] public string RequiredError { get; set; } = "";
        [Parameter] public string HelperText { get; set; } = "";
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool ReadOnly { get; set; } = true;
        [Parameter] public InputType InputType { get; set; } = InputType.Text;
        [Parameter] public string Placeholder { get; set; } = "";
        [Parameter] public int? MaxLength { get; set; }
        [Parameter] public Func<TRefEntity?, string>? ValidationFunc { get; set; }
        /// <summary>
        /// Функция, которая возвращает значение Tooltip-а к выбранному значению
        /// </summary>
        [Parameter] public Func<TEntity?, TRefEntity?, string>? GetTooltipFunc { get; set; }
        // [Parameter] public IObjectStorage? ObjectStorage { get; set; }

        /// <summary>
        /// Func, который нужно вызывать для получения значения отображаемого текста из объекта TRefEntity.
        /// Если не указано - будет возвращено свойство <see cref="BaseEntity.Description"/> от объекта RefEntity.
        /// </summary>
        [Parameter] public Func<TRefEntity?, string>? DisplayTextGetter { get; set; }
        
        /// <summary>
        /// Func, который нужно вызывать для получения значения TRefEntity из объекта TEntity
        /// </summary>
        [Parameter] public Func<TEntity, TRefEntity?>? ValGetter { get; set; }
        
        /// <summary>
        /// Action, которую нужно вызывать для установки значения TRefEntity в объект TEntity
        /// </summary>
        [Parameter] public Action<TEntity, TRefEntity?>? ValSetter { get; set; }
        [Parameter] public Action<TEntity, int?>? ValIdSetter { get; set; }

        [Parameter] public Func<IQuerySpecification<TRefEntity>, IQuerySpecification<TRefEntity>>? ConfigureQuerySpecification { get; set; }
        
        /// <summary>
        /// Заранее подготовленный список объектов, из которых осуществлять выбор. 
        /// </summary>
        [Parameter] public List<TRefEntity>? RefEntities { get; set; }
        // [Obsolete("Данный параметр устарел! Нужно использовать ConfigureQuerySpecification.")]
        // [Parameter] public Func<IQueryable<TRefEntity>, IQueryable<TRefEntity>>? ConfigureSelect { get; set; }
        // [Obsolete("Данный параметр устарел! Нужно использовать QuerySpecification.")]
        // [Parameter] public Func<IQueryable<TRefEntity>, IQueryable<TRefEntity>>? ConfigureWhere { get; set; }
        // [Obsolete("Данный параметр устарел! Нужно использовать QuerySpecification.")]
        // [Parameter] public Func<IQueryable<TRefEntity>, IQueryable<TRefEntity>>? ConfigureOrderBy { get; set; }
        // // [Parameter] public Action? ValueChanged { get; set; }
        
        [Parameter] public EventCallback ValueChanged { get; set; }
        
        /// <summary>
        /// Функция, возращающая значение Url для открытия формы с выбранным объектом.
        /// Если задана и возвращает непустую строку - справа будет отрисована кнопка с установленным url.
        /// </summary>
        [Parameter] public Func<string>? HrefToFormUrl { get; set; }

        /// <summary>
        /// Функция, возращающая значение текст (подсказку) для кнопки со ссылкой на открытие формы.
        /// </summary>
        [Parameter] public Func<string>? HrefToFormText { get; set; }

        private bool _hasErrors => !string.IsNullOrEmpty(GetErrorText());
        private string _tooltip = "";
        private string _helperText => (_tooltip.Length > 0) ? _tooltip : HelperText;

        protected IObjectStorage? _objectStorage { get; set; }
        // [Parameter] public int Id { get; set; } = 0;
        protected IObjectStorage? ActiveObjectStorage
        {
            get
            {
                if (_objectStorage == null)
                {
                    if (_sharedObjectStorage != null)
                    {
                        _objectStorage = _sharedObjectStorage;
                    }
                    //else if (ObjectStorage != null)
                    //{
                    //    _objectStorage = ObjectStorage;
                    //}
                     
                    // Если к этому моменту не удалось инициализировать ObjectStorage - получаем его от DI
                    if (_objectStorage == null && _objectStorageProvider != null)
                    {
                        Result<IObjectStorage> res = _objectStorageProvider.GetObjectStorage();
                        if (!res.IsError && res.Value != null)
                        {
                            _objectStorage = res.Value;
                        }
                    }
                }
                return _objectStorage;
            }
            set => _objectStorage = value;
        }


        /// <summary>
        /// Значание, котрое отображается пользователю
        /// </summary>
        public string Text
        {
            get
            {
                if (Entity == null || ValGetter == null) return _text;
                
                TRefEntity? refEntity = ValGetter.Invoke(Entity);
                if (DisplayTextGetter != null)
                {
                    _text = DisplayTextGetter.Invoke(refEntity); 
                }
                else
                {
                    _text = refEntity?.Description ?? "";
                }
                return _text;
            }
            set => _text = value;
        }
        private string _text = "";


        protected override void OnInitialized()
        {
            if(_sharedObjectStorage == null)
            {
                _snackBar?.Add("Ошибка конфигурирования: не задан каскадный параметр SharedObjectStorage", Severity.Error);
                return;
            }

            if (_editContext != null)
            {
                // Создаем FieldIdentifier для текущего компонента
                _fieldIdentifier = FieldIdentifier.Create(() => Value);
            }

            base.OnInitialized();
        }

        private string GetErrorText()
        {
            if (ValidationFunc != null)
            {
                return ValidationFunc(Value);
            }
            return "";
        }

        // https://stackoverflow.com/questions/77044361/mudblazor-and-c-sharp-how-do-i-use-multiple-validation-methods-in-a-mudtextfield
        private Func<TRefEntity, IEnumerable<string>> _validation => _ =>
            {
                var error = GetErrorText();
                return string.IsNullOrEmpty(error) ? [] : [error];
            };

        #endregion

        protected override void OnParametersSet()
        {
            // Если установлена функция получения значения Tooltip-а - вызываем ее и заполняем значение Tooltip
            if (GetTooltipFunc != null && Entity != null)
            {
                var selectedEntity = ValGetter?.Invoke(Entity);
                if (selectedEntity != null)
                {
                    _tooltip = GetTooltipFunc.Invoke(Entity, selectedEntity);
                }
            }
        }

        private async Task OnValueChanged(TRefEntity? selectedEntity)
        {
            Value = selectedEntity;
            if (Entity != null)
            {
                // Устанавливаем новое значение в объект
                ValSetter?.Invoke(Entity, selectedEntity);
                // Если установла функция для записи Entity.Id - вызываем ее
                if (ValIdSetter != null)
                {
                    int? entityId = (selectedEntity != null) ? selectedEntity.Id : null;
                    ValIdSetter.Invoke(Entity, entityId);
                }

                // Читаем значение из объекта. Из только что установленного Entity не читаем, поскольку в объекте что-то могло и измениться.
                if (selectedEntity != null)
                {
                    Text = ValGetter?.Invoke(Entity)?.Description ?? Text;
                }
                else
                {
                    Text = "";
                }

                // Если установлена функция получения значения Tooltip-а - вызываем ее и заполняем значение Tooltip
                if (GetTooltipFunc != null && selectedEntity != null)
                {
                    _tooltip = GetTooltipFunc.Invoke(Entity, selectedEntity);
                }
                else
                {
                    _tooltip = "";
                }
                
                await ValueChanged.InvokeAsync();
                StateHasChanged();
            }
            //await ValueChanged.InvokeAsync(selectedEntity);
        }

        private async Task Clear()
        {
            await OnValueChanged(null);
        }

        private async Task OpenDialogAsync()
        {
            if (Disabled)
            {
                return;
            }

            if (Entity == null)
            {
                _snackBar?.Add("В RefEdit не установлен основной объект (Entity)", Severity.Error);
                return;
            }

            if (_dialogService != null && ActiveObjectStorage != null && _snackBar != null)
            {
                DialogOptions options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
                DialogParameters<SelectDialog<TRefEntity>> parameters = new DialogParameters<SelectDialog<TRefEntity>>();
                
                Result<List<TRefEntity>> resList = await _getRefEntitiesList();
                
                if (resList.IsErrorOrNull)
                {
                    _snackBar?.Add($"Ошибка получения списка для выбора: {resList.ErrorResult}", Severity.Error);
                    return;
                }

                // Устанавливаем диалогу список
                parameters.Add(x => x.Items, resList.Value);
                
                // Если есть текущее значение - также его устанавливаем, чтобы спозиционировать курсор списка
                if (Value != null)
                {
                    parameters.Add(x => x.SelectedObject, Value);
                }
                
                var dialog = await _dialogService.ShowAsync<SelectDialog<TRefEntity>>($"Выберите значение", parameters, options);
                var r = await dialog.Result;
                if (r != null && !r.Canceled)
                {
                    TRefEntity? selectedEntity = (TRefEntity?)r.Data;
                    await OnValueChanged(selectedEntity);
                }
            }
        }

        private async Task<Result<List<TRefEntity>>> _getRefEntitiesList()
        {
            // Если список был уже установлен извне - просто возвращаем его, без обращения к БД
            if(RefEntities != null) return Result<List<TRefEntity>>.Success(RefEntities);
            
            if (ActiveObjectStorage == null) return Result<List<TRefEntity>>.Error("Не установлен ActiveObjectStorage");
            
            Result<IBaseRepository<TRefEntity>> result = ActiveObjectStorage.GetBaseRepository<TRefEntity>();
            if (result.IsErrorOrNull)
            {
                return Result<List<TRefEntity>>.Error($"Не удалось получить репозиторий от BaseRepository<{typeof(TEntity).Name}> от IObjectStorage");
            }

            IBaseRepository<TRefEntity> repository = result.Value!;
            repository.ConfigureQuerySpecification = ConfigureQuerySpecification;

            Result<List<TRefEntity>> resList = await repository.GetAllAsync();
            if (resList.IsErrorOrNull)
            {
                _snackBar?.Add(resList.ErrorResult, Severity.Error);
                return resList;
            }

            return resList;
        }

        private string _hrefUrl => HrefToFormUrl != null ? HrefToFormUrl() : "";
        private string _hrefText => HrefToFormText != null ? HrefToFormText() : "Открыть форму";
        
        private async Task OpenHref()
        {
            string href = _hrefUrl;
            if (!string.IsNullOrEmpty(href) && _jsRuntime != null)
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("window.open", href, "_blank");
                }
                catch (Exception e)
                {
                    _snackBar?.Add($"Ошибка открытия формы с объектом: {e.Message}", Severity.Error);
                }
            }
        }
    }
}