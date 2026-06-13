## Динамические запросы
### SavedQuery
#### Общее описание
Данный объект, наряду с визуальными компонентами, позволяет в run-time формировать запросы, выполнять их и получать данные из БД.
Используется System.Linq.Dynamic.Core.
Для преодоления ошибки ```nullable object must have a value```, которая может возникать в случае, когда в запросе фигурируют обязательные поля связанного объекта,
а при этом сам объект отсутствует, необходимо использовать конструкцию np() (null propagation). Пример запроса:
```csharp
select new ( Login, FIO, np(Division.Name) as Division_Name, np(Division.SysCode) as Division_SysCode)
```
#### Примеры фильтров
```csharp
Division.SysCode = {AppCore.UserParamList.CountAll} or Login = "{AppCore.CurrentUser.Login}" or Login = "{ParamList.UserName.Value}"
```