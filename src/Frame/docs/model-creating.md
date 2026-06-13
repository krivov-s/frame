# Последовательность шагов для создания новой модели, настройки всех связей, проверок и пользовательского интерфейса

### 1. Создание класса модели
~~~~- Класс модели создается в проекте *.Domain в папке Entities
- Класс модели должен наследовать от BaseGenericEntity
- Внутри класса модели необходимо создать статический класс с именем Meta, содержащий метаданные по модели и ее полям. 
- Для описание полей модели используются классы [Field](xref:Frame.Domain.Entities.Core.Field`1) и [RefField](xref:Frame.Domain.Entities.Core.RefField`2)
- Пример простой модели:
  ```csharp
    public class TestObject  : BaseEntity, IBaseGenericEntity<TestObject>
    {
        public override string Description => TextAttr;

        public string TextAttr { get; set; } = string.Empty;
        public DateTime? DateTimeAttr { get; set; } = default;
        public int IntAttr { get; set; } = default;
        public decimal DecimalAttr { get; set; } = default;
        public virtual List<TestChildObject> ChildObjects { get; set; } = [];

        #region Metadata
        public static class Meta
        {
            public static readonly string HumanName = "Тестовый объект";
            public static readonly Field<TestObject> TextAttr = new() 
            { 
                Name = nameof(TextAttr), 
                StringGet = f => f.TextAttr, 
                StringSet = (x, val) => x.TextAttr = val ?? "", 
                Required = false, 
                MaxLength = 10, 
                HumanName = "Текст" 
            };
            public static readonly Field<TestObject> DateTimeAttr = new() 
            { 
                Name = nameof(DateTimeAttr), 
                DateTimeGet = f => f.DateTimeAttr, 
                DateTimeSet = (x, val) => x.DateTimeAttr = val, 
                Required = true, 
                HumanName = "Дата-время" 
            };
            public static readonly Field<TestObject> IntAttr = new() 
            { 
                Name = nameof(IntAttr), 
                IntGet = f => f.IntAttr, 
                IntSet = (x, val) => x.IntAttr = val ?? 0,
                Required = false, 
                HumanName = "Целое" 
            };
            public static readonly Field<TestObject> DecimalAttr = new() 
            { 
                Name = nameof(DecimalAttr), 
                DecimalGet = f => f.DecimalAttr,
                DecimalSet = (x, val) => x.DecimalAttr = val ?? 0,
                Required = false, 
                HumanName = "Целое" 
            };
            public static readonly List<Field<TestObject>> Fields = [TextAttr, DateTimeAttr, IntAttr, DecimalAttr];
        }
        public List<Field<TestObject>> GetFields() { return Meta.Fields; }
        #endregion
    }
  ```
  ### 2. Создание валидатора модели.
  - Для валидации используется библиотека [FluentValidation](https://docs.fluentvalidation.net/en/latest/index.html) 
  - По-умолчанию, при использовании стандартного <xref:Frame.Infrastructure.EntityRepositories.Core.EFBaseRepository`1>, при сохранении модели будет создан универсальный валидатор <xref:Frame.Domain.EntityValidators.Core.BaseEntityValidator`1>, который на основании метаданных, определенных в модели в классе Meta, выполнит базовые проверки перед сохранением.
  - В случае, если необходимы дополнительные специфичные проверки, необходимо реализовать собственный класс валидатора, который разместить в проекте *.Domain в папке EntityValidators. Пример валидатора:
  ```csharp
    public class UserValidator : BaseEntityValidator<User>
    {
        public UserValidator()
        {
            // Преобразование свойств
            RuleFor(x => x).Custom((user, context) => { user.NormalizedLogin = user.Login.Trim().ToUpper(); });
        }
    }
    ```
  ### 3. Создание репозитория модели
  - В рамках фреймворка определен стандартный универсальный репозиторий <xref:Frame.Infrastructure.EntityRepositories.Core.EFBaseRepository`1>. 
  - В конвейере DI по-умолчанию зарегистрирован поставщик репозиториев <xref:Frame.Infrastructure.EntityRepositories.Core.EFBaseRepositoryProvider>.
  - Везде, где необходимо получить возможность чтения/записи объектов из/в БД, необходмо или получить от DI свой пользовательский репозиторий, который заранее был зарегистрирован в конвейере DI, или запросить репозиторий через <xref:Frame.Infrastructure.EntityRepositories.Core.EFBaseRepositoryProvider>. Пример:
  ```csharp
  _repository = _repositoryProvider.GetBaseRepository<Role>();
  ```
  - Для максимальной гибкости, с целью избежать необходимости создания собственных репозиториев, у стандартного репозитория объявлены свойства, позволяющие динамически настраивать секции запросов Select (<xref:Frame.Infrastructure.EntityRepositories.Core.EFBaseRepository`1.ConfigureSelect>), Where <xref:Frame.Infrastructure.EntityRepositories.Core.EFBaseRepository`1.ConfigureWhere> и OrderBy <xref:Frame.Infrastructure.EntityRepositories.Core.EFBaseRepository`1.ConfigureOrderBy>. 
  - Универсальный репозиторий можно настраивать и регистрировать в конвейере DI. В этом случае из конвейера сразу же придет репозиторий, сконфигурированный в соответствии с параметрами, определенными при его регистрации. Пример:
  ```csharp
  services.AddBaseRepository<UserClaim>(options => options.ConfigureSelect = (qry) => qry.Include(ur => ur.User));
  ```
  - Универсальный репозиторий можно настраивать и после его получения из конвейера. Синтаксис настройки такой же, как и в примере выше.
  ```csharp
  _repository.ConfigureSelect = (qry) => qry.Include(x => x.ContragentType);
  ```
  ### 4. Создание формы со списком
  - Для создания окна со списком используется компонент <xref:Frame.WebUI.Components.Shared.EntitiesListDetail`2>. Это универсальный компонент, разработанный на основе DataGrid, осуществляющий CRUD, настраиваемый под нужды конкретного типа.
  - Компонент создается в проекте *.WebUI в папке Components/Pages.
  - Компонент состоит из двух частей: список и форма
  - Компонент может работать в режимах, определяемых <xref:Frame.WebLib.Components.Shared.EntitiesListEditMode> 
  - Пример наиболее удобного (FormContent):
  ```csharp
  @page "/contragent_type_list"
  @using SomeBusinessApp.Domain.Entities.Nsi
  @rendermode @(new InteractiveServerRenderMode(prerender:false))

  <MudPopoverProvider />
  @* <MudDialogProvider Position="DialogPosition.TopLeft" /> *@
  <MudSnackbarProvider />

  <EntitiesListDetail TEntity="ContragentType"
  TParentComponent="PL_ContragentType"
  Title="Список типов контрагентов"
  EditMode="EntitiesListEditMode.FormContent">

    <GridContent>
        <PropertyColumn T="@ContragentType" TProperty="@string" Property="x => x.Name" Title="@ContragentType.Meta.Name.HumanName"></PropertyColumn>
        <PropertyColumn T="@ContragentType" TProperty="@string" Property="x => x.Descr" Title="@ContragentType.Meta.Descr.HumanName"></PropertyColumn>
        <PropertyColumn T="@ContragentType" TProperty="@int" Property="x => x.Id" Title="Id" Editable="false" />
    </GridContent>
    
    <FormContent>
        <SimpleEditForm TEntity="ContragentType" TParentComponent="PL_ContragentType" Entity="context"></SimpleEditForm>
    </FormContent>
  </EntitiesListDetail>

  @code {
  
  }
  ```
  - В параметрах компонента <xref:Frame.WebUI.Components.Shared.EntitiesListDetail`2> также можно настраивать секции Select, Where и OrderBy. Принцип настройки полностью аналогичен репозиторию.

### 5. Создание формы изменения объекта.
- В рамках фреймворка определен стандартный универсальный компонент <xref:Frame.WebUI.Components.Shared.SimpleEditForm`2>, который без дополнительной настройки на основании метаданных модели может сконструировать форму ввода и редактирования информации.
- Возможно самостоятельно сформировать дизайн формы. В этом случае все поля располагаются между тегами <SimpleEditForm></SimpleEditForm>. 
- Для упрощения создания полей используются компоненты-фабрики <xref:Frame.WebUI.Components.Shared.FieldEditFactory`1> и <xref:Frame.WebUI.Components.Shared.RefEditFactory`1>, которые получают всю необходимую информацию из метаданных поля. Пример разметки формы:
```csharp
<SimpleEditForm @ref="_simpleEditForm"
                TEntity="REPlanLine"
                TParentComponent="REPlanLineForm"
                Id="@REPlanLineId"
                OnBeforeEditFormContent="(pl, repo) => InitNewObject(pl, repo)">
    @if (_rePlanLine != null && _simpleEditForm != null)
    {
        <MudGrid>
            <MudItem xs="12" sm="6">
                <RefEditFactory TEntity="REPlanLine" Entity="_rePlanLine" Field="REPlanLine.Meta.REPlan" Disabled="@REPlanDisabled" ValueChanged="_simpleEditForm.SetDataChanged"></RefEditFactory>
            </MudItem>
            <MudItem xs="12" sm="6">
                <RefEditFactory TEntity="REPlanLine" Entity="_rePlanLine" Field="REPlanLine.Meta.REObject" Disabled="@REObjectDisabled"  ValueChanged="_simpleEditForm.SetDataChanged"></RefEditFactory>
            </MudItem>
            <MudItem xs="12" sm="4">
                <FieldEditFactory TEntity="REPlanLine" Entity="_rePlanLine" Field="REPlanLine.Meta.Limit"></FieldEditFactory>
            </MudItem>
            <MudItem xs="12" sm="4">
                <FieldEditFactory TEntity="REPlanLine" Entity="_rePlanLine" Field="REPlanLine.Meta.DueDate"></FieldEditFactory>
            </MudItem>
            <MudItem xs="12" sm="4">
                <FieldEditFactory TEntity="REPlanLine" Entity="_rePlanLine" Field="REPlanLine.Meta.FinishDate"></FieldEditFactory>
            </MudItem>
            <MudItem xs="12" sm="12">
                <FieldEditFactory TEntity="REPlanLine" Entity="_rePlanLine" Field="REPlanLine.Meta.Comment" Lines="2"></FieldEditFactory>
            </MudItem>
        </MudGrid>
    }
</SimpleEditForm>
``` 
- В примере кода выше обратите внимание на **OnBeforeEditFormContent**: данный Action в случае его определения, будет вызван после создания или чтения из БД объекта, который отображается в форме. Это дает возможность сохранить в служебных переменных и сам объект и репозиторий, который использовался для чтения из хранилища, или осуществить донастройку или инициализацию объекта.
- Параметр компонента ConfigureSelect позволяет донастроить секцию Select запроса. Это полезно в тех случаях, когда для отображения необходимо зачитать связанные данные модели.
- Все остальные свойства/события можно посмотреть в заголовке и документации по <xref:Frame.WebUI.Components.Shared.EntitiesListDetail`2>

### 6. Регистрация меню.
- Для отображения пользователю меню необходимо прописать соответствующие инструкции в роли, которой будет предоставлено право доступа к данному функционалу.
- Инструкция прописывается в формате json в объекте <xref:Frame.Domain.Entities.Core.Security.RoleClaim> с типом "NavMenu". Более подробно см. [описание дизайна web-интерфейса](web.md).