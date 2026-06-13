# Проект Frame.WebUI

- Проект Asp.Net Core Blazor ([MudBlazor](https://mudblazor.com/docs/overview)) уровня Presentation по иерархии Clean Architectue
- В этом проекте происходит объединение всех уровней иерархии Clean Architecture
- Все зависимости выстраиваются в файле Program.cs
    - Внутри вызываются DependencyInjection с уровней Infrastructure и Application
	```csharp
	builder.Services.AddInfrastructureServices(builder.Configuration);
	builder.Services.AddApplicationServices();
	```
- Аутентификация реализована на основе Cookies, без использования Microsoft Identity

## Структура папки Frame.Weblib.Components
- AdminPanel - все компоненты, связанные с интерфейсом панели администратора (роли, пользователи)
- FileDocument - универсальный интерфейс пользователя для хранилища файлов
- Scripting - базовый интерфейс пользователя для скрипт-операций
- Shared - универсальные компоненты пользовательского интерфейса _(AuditRecord, EntitiesListDetail, SimpleEditForm, SelectDialog и др.)_

## Динамическое меню.
- В иерархии User-Roles-RoleClaims допускается создание объектов RoleClaim с типом "NavMenu".
Значение данного RoleClaim - Json с описанием раздела <MudNavGroup></MudNavGroup>.
Пример:
```json
{
	"Name":"Администрирование",
	"IconClass": "Filled",
	"IconName": "AdminPanelSettings",
	"MenuItems":[
		{
			"Title": "Роли",
			"Href": "Rolelist",
			"IconClass": "Filled",
			"IconName": "Shield"
		},
		{
			"Title": "Пользователи",
			"Href": "Userlist",
			"IconClass": "Filled",
			"IconName": "Man"
		},
		{
			"Group": {
				"Name": "План мероприятий",
				"IconClass": "Filled",
				"IconName": "MenuBook",
				"MenuItems": [
					{
						"Title": "Планы",
						"Href": "replan_list",
						"IconClass": "Filled",
						"IconName": ""
					}
				]
			}
		}
	]
}
```
- Описание иконок берется из статического класса Icons.Material.Outline или Icons.Material.Filled
- NavMenu формируется программно, в одноименном компоненте. У текущего пользователя запрашивается список ролей, для каждой роли, у которой есть RoleClaim с типом "NavMenu"
берется значение RoleClaim и преобразуется в <MudNavGroup>. Таким образом в зависимости от назначенных ролей у пользователя сформируется свой набор меню.

## Реализация пользовательского интерфейса на основе шаблонных компонентов
- Списки (datagrid) реализуются на базе компонента [EntitiesListDetail](xref:Frame.WebUI.Components.Shared.EntitiesList`2)
- Формы реализуются на базе компонента [SimpleEditForm](xref:Frame.WebUI.Components.Shared.SimpleEditForm`2) 
- При динамическом создании фреймворк использует информацию из классов Meta, создаваемых внутри каждой модели.

_Более подробно - см. в конкретных примерах (образцах) кода, здесь пока расписывать не стал_

## Особенности реализации интерактива Blazor
- В файле App.razor по-умолчанию включается режим InteractiveServer без пререндеринга
> <HeadOutlet @rendermode=__@(new InteractiveServerRenderMode(false))__ />
> .....
> <Routes @rendermode=__@(new InteractiveServerRenderMode(false))__ />
- В файле MainLayout.razor нельзя использовать <MudTooltip>: возникает ошибка из-за отсутствия <MudPopoverProvider />. 
  Попытка добавить <MudPopoverProvider /> приводит к появлению сообщения о его дублировании. Четко выявить где дубль не удалось.
- <MudPopoverProvider /> прописывается в каждой форме индивидуально.
