# Использование скриптов
## Общие сведения
Для редактирования скриптов планируется использование компонента [AceJsEditor](https://github.com/bzins-app/Blazor.AceJS)
Для его функционирования необходимо добавление скриптов в js-скрипты web-проекта
```html
<script src="_content/Blazor.AceEditorJs/BlazorAceEditor.min.js"></script>
```

## Основные объекты и интерфейсы
## Использование скриптов в приложении
### Расширения объектов (аналог OnBeforeSave и OnAfterSave)
### Пользовательские скрипт-команды (ScriptCommand)

- Если необходимо позвать скриптовые расширения данных методов, это можно сделать вручную, 
  путем вызова соответствующего метода ScriptCore. Например:
```csharp 
  appCore.ScriptCore?.ExecuteEntityScriptAsync(this, Domain.Scripting.EHookType.OnBeforeSave, Domain.Scripting.EThreadType.Sync);
``` 
  > При этом соответствующие расширения должны быть предварительно зарегистрированы в ScriptCore путем создания и сохранения 
  соответствующих объектов [EntityScript](xref:Frame.Domain.Entities.Core.Scripting.EntityScript)
