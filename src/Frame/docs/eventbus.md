# Очередь событий
В системе реализована очередь событий на основе Channel и MedatR.

## Основные тезисы.
1. Есть два потока событий
	1. Прямые вызовы внутри системы через MediatR (IAppRequest, IAppNotification)
	1. Асинхронная очередь сообщений, IEventQueue, пока реализованная с использованием Channels, InMemory. 
	   Выполнение событий после успешного сохранения объекта в БД осуществляется асинхронно и никак не влияет 
	   на оригинальный процесс сохранения.
1. Диспетчер событий - AppEventDispatcher
1. Мониторинг асинхронной очереди - в отдельном сервисе EventQueueProcessor
1. События OnBeforeSave/Delete маршрутизируются через MediatR как IAppRequest
1. События OnAfterSave/Delete маршрутизируются через IEventQueue как IAppNotification
1. На уровне ядра реализована регистрация событий на OnBeforeSave/Delete и OnAfterSave/Delete для всех объектов системы.
   Для этого сделана динамическая фабрика AppEventFactory, в которую на старте для каждого TEntity регистрируются 
   EntityAfterSaveNotification<TEntity> и EntityBeforeSaveRequest<TEntity>
1. Поскольку по требованиям MediatR для каждого IAppRequest обязательно должен быть зарегистрирован IRequestHandler, для всех TEntity
   зарегистрирован DefaultBeforeSaveRequestHandler<TEntity>, который ничего не делает. Просто чтобы MediatR не выбрасывал Exceptions.
1. При необходимости обработки события OnBeforeSave необходимо зарегистрировать новый типизированный Handler, которы в этом случае 
   перекроет DefaultBeforeSaveRequestHandler и будет вызываться вместо него.
1. **Что нужно еще сделать**:
	1. Реализовать динамическую регистрацию Handler-а (по аналогии с DefaultBeforeSaveRequestHandler) для всех зарегистрированных 
	   IEntityScriptMethod (=> IAppRequest). Можно для этого приспособить Handler (IAppNotification), который на по факту 
	   сохранения IEntityScriptMethod дорегистрирует Handler  ?????? нужно подумать.......

## Порядок регистрации произвольного события IAppNotification и его обработчиков
1. Создать класс-наследник IAppNotification (это само сообщение). Отличие от IAppRequest в том, что для IAppNotofocation 
   можно регистрировать сколь угодно много обработчиков, на разных уровнях приложения. 
2. Создать класс-наследник INotificationHandler<TNotification> (это обработчик сообщения)
   1. Все обработчики, реализующие интерфейс INotificationHandler - это обработчики MediatR. 
   	  Они автоматически "подтягиваются" при инициализации MediatR в Di Frame.App в методе services.AddMediatR()
3. "Прямой" вызов (отправка уведомления с немедленным ответом) осуществляется через вызов 
```csharp
appEventDispatcher.PublishAsync(IAppNotification notification);
```
4. "Отложенный" вызов (отправка уведомления без ожидания ответа, через асинхронную очередь) осуществляется через вызов
```csharp
appEventDispatcher.QueueAsync(IAppNotification notification);
```
5. Можно регистрировать несколько обработчиков на один IAppNotification - все они будут вызваны.

