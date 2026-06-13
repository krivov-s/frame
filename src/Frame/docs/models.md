# Модели
## Ссылки
- Следует создавать в модели как объектную ссылку на другую модель, так и поле Id (например UsersRoles.Role и UsersRoles.RoleId).
	При этом при создании в контексте объекта со ссылкой на уже существующий объект, если его нет в контексте - будет попытка
	его добавления, что в корне неверно. Поэтому он либо уже должен быть в контексте, либо нужно присваивать ссылке только Id.
- Ссылки следует объявлять через virtual, поскольку в системе дана возможность использовать EF Core LazyLoadingProxies.

## Копирование
- пока не реализовано. 

## Метаданные.
- Все объекты (Entities) наследуют от BaseEntity и реализуют интерфейс IBaseGenericEntity<TEntity>
- Таблицы в БД создаются только для конечных типов, это нормально отрабатывает по-умолчанию.
- Возможно осуществлять наследование бизнес-объектов. Например: 
  ```csharp
     class NewDivision : Division, IBaseGenericEntity<NewDivision>
     {
         // Здесь определяются поля в дополнение к Division
         public string NewName {get;set;}
         
        public new static class Meta
        {
            public static readonly string HumanName = "Новое подразделение";
            public static readonly Field<NewDivision> FieldFromBaseType = new() { Name = nameof(FieldFromBaseType), IntGet = x => x.FieldFromBaseType, IntSet = (x, val) => x.FieldFromBaseType = val ?? 0, Required = true, HumanName = "Системный код" };
            public static readonly Field<NewDivision> NewName = new() { Name = nameof(NewName), StringGet = x => x.NewName, StringSet = (x, val) => x.NewName = val ?? "", Required = true, MaxLength = 20, HumanName = "Новое название" };
        }
     }
  ```
  При такой конфигурации в терминах EF.Core будет реализовано наследование в режиме __Table per Hierarchy (TPH)__. 
- В каждом конкретном TEntity должен быть определен подкласс Meta, а также переопределен виртуальный метоод GetFields, 
который	возвращает список объектов Field с метаданными по полям класса.
- Основные метаданные модели (длина поля, обязательность) фиксируются во внутреннем классе Meta каждого типа. Тогда длина поля, обязательность, и еще что-нибудь (будет добавляться по мере необходимости) задаются для конкретного типа и используются и при создании БД и валидаторов

## Валидация
- Валидация модели осуществляется с использованием FluentValidation. Реализуется в слое Domain в папке EntityValidators. 
	- При старте сервера в Frame.Domain.DependencyInjection вызывается метод расширения AddBaseEntityValidationFactory, который добавляет в свою внутреннюю карту валидаторы для всех типов, наследованных от BaseEntity. Это будут либо специфичные реализации IValidator<TEntity> либо [BaseEntityValidator](xref:Frame.Domain.EntityValidators.BaseEntityValidator`1)
    - **ВАЖНО!!! В DI валидаторы не добавляются, только фабрика [IEntityValidatorFactory](xref:Frame.Domain.EntityValidators.IEntityValidatorFactory)!** Получение валидаторов только через фабрику. В текущей реализации BaseRepository ожидает валидатор только в конструкторе через DI, самостоятельно его не создает! Поэтому валидация будет происходить только в момент сохранения в ObjectStorage.
	- Есть базовый валидатор [BaseEntityValidator](xref:Frame.Domain.EntityValidators.BaseEntityValidator`1), который добавляет правила на основании метаданных моделей (класс Meta и метод GetFields каждой модели). 
	- Если базовой валидации недостаточно - необходимо наследовать от BaseEntityValidator и добавить дополнительные проверки или создать свой собственный валидатор.
    - Если свой валидатор зарегистрировать в DI - то он будет попадать и в BaseRepository, таким образом валидация будет осуществляться дважды: на этапе добавления в репозиторий и на этапе сохранения в БД. 
	- Валидатор может осуществлять окончательную донастройку/проверку объекта и всех вложенных подъобъектов (связей). Эта опция - замена ранее существовавшего виртуального метода модели OnBeforaSave.
    - Каскадная проверка связанных объектов (если необходима) должна быть предусмотрена разработчиком в валидаторе. **Автоматическое формирование и вызов валидаторов связанных объектов, которые могут зависеть ит изменяемых, а при этом сами не изменялись - не осуществляется.**
    - ObjectStorage осуществляет каскадную проверку только тех объектов, которые изменились и будут сохранены в БД.
	- Валидация происходит в двух местах:
		- если валидатор зарегистрирован в DI - то перед сохранением модели, методы вызываются из класса-реализации репозитория [EFBaseRepository](xref:Frame.Infrastructure.EntityRepositories.EFBaseRepository);
		- в ходе сохранения модели на уровне ObjectStorage: валидация осуществляется для каждого объекта, который на уровне контекста подлежит сохранению в БД. Даже если в ходе валидации или выполнения методов расширения произойдет появление новых объектов или изменение существующих - их валидация произойдет рекурсивно, до тех пор пока все планируемые к записи объекты не будут провалидированы.
		- опционально - валидаторы можно переиспользовать в визуалке на уровне компонентов MudBlazor
- Предусмотрена возможность вызова дополнительных методов (в т.ч. скриптовых) перед и после сохранения объекта. 
  - Метды реализуются в виде обработчиков событий [IEntityBeforeSaveRequest](xref:Frame.App.EventBus.Events.Request.IEntityBeforeSaveRequest) и 
    [IEntityAfterSaveNotification](xref:Frame.App.EventBus.Events.Request.IEntityAfterSaveNotification).
  - Перед записью объекта во внешнюю БД [ObjectStorage](xref:Frame.Infrastructure.EntityRepositories.EFObjectStorage) направит 
    в [AppEventDispatcher.SendAsync](xref:Frame.App.EventBus.Core.IAppEventDispatcher.SendAsync) сообщение, зарегистрированное 
	для данного TEntity в [AppEventFactory.RegisterEntityBeforeSaveScriptRequest](xref:Frame.App.EventBus.Events.Core.AppEventFavtory.RegisterEntityBeforeSaveScriptRequest).
  - После успешного завершения транзакции сохранения [ObjectStorage](xref:Frame.Infrastructure.EntityRepositories.EFObjectStorage)
    направит в [AppEventDispatcher.PublishAsync](xref:Frame.App.EventBus.Core.IAppEventDispatcher.PublishAsync) сообщения (возможно несколько), 
	зарегистрированные для данного TEntity в [AppEventFactory.RegisterEntityAfterSaveNotification](xref:Frame.App.EventBus.Events.Request.IEntityAfterSaveNotification.RegisterEntityAfterSaveNotification)
 
## Конфигурирование БД 
- Для настройки БД реализуются интерфейс _IEntityTypeConfiguration<Entity>_ для каждой таблицы, если нужно что-то изменить относительно соглашений по-умолчанию. Использует метаданные из классов Meta внутри класса конкретной модели.
- [BaseGenericEntityConfiguration](xref:Frame.Infrastructure.EntityDbConfigurations.Core.BaseGenericEntityConfiguration`1) является базовым для настройки БД. На основании метаданных из класса Meta модели (список из GetFields) он устанавливает для полей в БД параметры IsRequired, HasMaxLength. Поэтому при наследовании от него данные параметры устанавливать не нужно, только дополнительные (отношения между объектами, при необходимости). Чтобы DbContext увидел все IEntityTypeConfiguration наследование должно быть прямым, поэтому каждый класс из EntityDbConfigurations должен иметь множественное наследование: от BaseGenericEntityConfiguration и от IEntityTypeConfiguration.
- Таблица в БД создается по имени явно указанного свойства DbSet<User> Users => в данном случае будет создана таблица Users. При динамическом оповещении контекста о существующих моделях (путем вызова modelBuilder.Entity(type)) будут созданы таблицы	с точным именем типа: т.е. если тип модели User - будет создана таблица User, а не Users. Поэтому, для наших двух контекстов AppDbContext и NoSecurityDbContext, в котором модели объявляются явно, чтобы не было несоответствий между динамическим (в AppDbContext) и статическим (в NoSecurityDbContext) объявлением модели, статические модели объявляются в единственном числе (например - public DbSet<Role> Role { get; set; })
- Настройка чтения моделей (по сути - запросов к БД) может осуществляться:
	- нигде, при этом при обращении будут считываться только линейные атрибуты модели, без ссылок;
	- в DependencyInjection.cs на уровне Infrastructure можно регистрировать свои специализированные репозитории или 
	  универсальный репозиторий с дополнительной настройкой секций запроса через ConfigureSelect, ConfigureWhere, ConfigureOrderBy
	- в параметрах SimpleForm и EntitiesList можно:
		- настроить секции запроса через ConfigureSelect, ConfigureWhere, ConfigureOrderBy
		- явным образом указать использование LazyLoading (по-умолчанию LazyLoading не используется)
- DateTime fields
	- В части контроля полей DateTime: **В БД ДОЛЖНЫ ХРАНИТЬСЯ ТОЛЬКО UTC ЗНАЧЕНИЯ!**
	Для контроля реализовано расширение [UsesUtc](xref:Frame.Infrastructure.PropertyBuilderExtensions.UsesUtc). Этот метод должен использоваться 
	в настройках моделей в полях DateTime. Данное расширение предотвращает использование DateTime не в формате Utc.
- Конфликты сохранения		
	- По-умолчанию проверка на изменение объекта в БД между вызовами чтения и сохранения отсутствует.
		В отдельных случаях можно рассмотреть это на уровне конкретного репозитория. 
		В этом случае перед сохранением нужно получить дату/время последнего обновления и сравнивать с датой/временем последнего обновления у объекта.
		Если различаются - ахтунг, не сохранять.

## Миграции
- Команда миграции для запуска из папки src\SomeBusinessApp:
```
dotnet ef migrations add SettingsReportFileDocumentType --project .\SomeBusinessApp.Migrations --startup-project .\VSPassport.WebUI --context NoSecurityDbContext;
```