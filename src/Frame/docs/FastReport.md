## Заготовка для формирования отчетов FastReport Open Source
### Теория.
https://github.com/FastReports/FastReport.Documentation/tree/master?tab=readme-ov-file
Предполагаемый режим взаимодействия FastReport и нашего фреймворка.
#### Вариант 1 - через DataSet
1. Из приложения делаются доступными endpoint-ы, возвращающие описание схем данных в формате xsd. 
Пример реализован в Frame.WebLib.Controllers.Schema.UsersController.
2. FastReport designer подключается к источнику данных xml к данному endpoint-у.
3. Разрабатывается отчет.
4. При генерации отчета в runtime данные должны быть сконвертированы в DataTable (DataSet) и зарегистрированы в отчете.
```csharp
webReport.Report.RegisterData(data, "Users", 3);
```
#### Вариант 2 - через BusinessObjectDataSource (предпочтительный вариант, но походу доступен только в платной версии)
https://github.com/FastReports/FastReport/tree/master/Demos/OpenSource/Console%20apps/DataFromBusinessObject
1. Формирование отчетов на базе бизнес-объектов позволяет отображать в отчете вложенные коллекции объектов 
(например - список ролей текущего пользователя)
2. В дизайнере отчетов должна быть возможность зарегистрировать объектный источник данных - этот вопрос нужно доизучать 
при наличии платной версии генератора.
3. Данные готовятся любым доступным способом (отчеты, автооперации)
4. Перед построением отчета полученный List<TItem> регистрируется в отчете в качестве источника объектных данных. 
Ниже представлен пример (кусок шаблона отчета) из официального образца, который нормально отображает вложенные коллекции.
Он прекрасно работает в runtime, но его невозможно сконструировать в OpenSource версии.
```xml
  <Dictionary>
    <BusinessObjectDataSource Name="Categories" ReferenceName="Categories" DataType="System.Collections.Generic.List`1[[DataFromBusinessObject.Category, DataFromBusinessObject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]" Enabled="true">
      <Column Name="Name" DataType="System.String"/>
      <Column Name="Description" DataType="System.String"/>
      <BusinessObjectDataSource Name="Products" DataType="System.Collections.Generic.List`1[[DataFromBusinessObject.Product, DataFromBusinessObject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]" Enabled="true">
        <Column Name="Name" DataType="System.String"/>
        <Column Name="UnitPrice" DataType="System.Decimal"/>
      </BusinessObjectDataSource>
    </BusinessObjectDataSource>
  </Dictionary>
```

### Шаги для интеграции в приложение.
#### 1. Необходимо добавить ссылки на сборки:
- FastReport.OpenSource.Web
- FastReport.OpenSource.Export.PdfSimple
- Иные необходимые сборки для источников данных FastReport
#### 2. Настроить приложение
- program.cs
    ```
  builder.Services.AddControllersWithViews()
        .AddApplicationPart(typeof(Frame.WebLib.Controllers.S3StorageController).Assembly)
        .AddApplicationPart(typeof(FastReportController).Assembly)
        .AddApplicationPart(typeof(Ccps.WebUI.Controllers.AccServiceController).Assembly)
        .AddRazorRuntimeCompilation(options =>
        {
            var assembly = typeof(FastReportController).Assembly;
            var embeddedFileProvider = new EmbeddedFileProvider(assembly, "Frame.Rep");
            options.FileProviders.Add(embeddedFileProvider);
        });
  ...
  
  app.UseFastReport();
  ``` 
- DependencyInjection.cs (Frame.Rep)
```csharp
services.AddFastReport();
```
#### 3. Реализовать контроллер, обрабатывающий запросы на построение отчетов.
Рабочая заготовка лежит в Frame.Rep.Controllers с именем FastReportController.cs

#### 4. Реализовать View для просмотра отчетов в приложении.
Рабочая заготовка лежит в Frame.Rep.Views.FastReport

#### 5. Заготовки отчетов лежат в папке Ccps.WebUI/Reports/FastReport
