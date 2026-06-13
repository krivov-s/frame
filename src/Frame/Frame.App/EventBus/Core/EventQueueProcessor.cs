using Frame.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Frame.App.EventBus.Core
{
    /// <summary>
    /// Реализация обработчика очереди сообщений внутри основного приложения.
    /// В перспективе можно заменить реализацию <see cref="IFrameEventBus"/> c <see cref="InMemoryMessageQueue"/> на RabbitMQ, или Redis, или еще на что-либо
    /// и обрабатывать сообщения из очереди в других процессах или на других серверах.
    /// </summary>
    public class EventQueueProcessor<TEvent> : BackgroundService where TEvent : IAppNotification
    {
        private readonly IEventQueue<TEvent> _eventQueue;
        private readonly ILogger<EventQueueProcessor<TEvent>> _logger;
        //private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IAppEventDispatcher _appEventDispatcher;

        // IServiceScopeFactory serviceScopeFactory,
        public EventQueueProcessor(IEventQueue<TEvent> eventQueue, IAppEventDispatcher appEventDispatcher, ILogger<EventQueueProcessor<TEvent>> logger)
        {
            _eventQueue = eventQueue;
            //_serviceScopeFactory = serviceScopeFactory;
            _appEventDispatcher = appEventDispatcher;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Result<TEvent> res = await _eventQueue.ReadAsync(stoppingToken);
                if (!res.IsError && res.Value != null)
                {
                    try
                    {
                        await ProcessMessageAsync(res.Value, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Непредвиденная ошибка при обработке чтения из очереди! Тем не менее продолжаем работу...");
                    }
                }
                else if (stoppingToken.IsCancellationRequested == false)
                {
                    // Если не удалось получить сообщение, но это не из-за отмены,
                    // подождем некоторое время перед следующей попыткой
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }

            _logger.LogInformation("Получен сигнал на остановку сервиса.");
        }

        private async Task ProcessMessageAsync(TEvent @event, CancellationToken _)
        {
            //_logger.LogInformation("Processing message: {Message}", message);
            try
            {
                // -------------------------------------------------------------------
                // Внимание! Если обработчик для данного сообщения не зарегистрирован,
                // то PublishEvent вернет ошибку, и она просто проигнорируется.
                // -------------------------------------------------------------------
                await _appEventDispatcher.PublishAsync(@event);
                // -------------------------------------------------------------------
                
                //using IServiceScope scope = _serviceScopeFactory.CreateScope();

                //// Здесь получаем MediatR
                //IPublisher mediator = scope.ServiceProvider.GetRequiredService<IPublisher>();

                //// Отдаем полученный из очереди Event Mediator-у
                //await mediator.Publish(@event, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке события!");
            }

        }
    }
}
