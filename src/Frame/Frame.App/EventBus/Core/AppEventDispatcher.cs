using System.Text;
using Frame.Shared;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Frame.App.EventBus.Core
{
    public class AppEventDispatcher(IMediator mediator, IEventQueue<IAppNotification> messageQueue,
        IServiceScopeFactory serviceScopeFactory, ILogger<AppEventDispatcher> logger) : IAppEventDispatcher
    {
        private readonly List<IAppNotification> _collectedToQueue = [];

        private bool _transactionStarted = false;
        
        private const string _sendError = "SendAsync: Ошибка отправки сообщения в MediatR";
        private const string _publishError = "SendAsync: Ошибка отправки сообщения в MediatR";
        private const string _queueError = "QueueAsync: Ошибка постановки сообщения в очередь MessageQueue";

        public void CollectToSend(IAppRequest? request)
        {
            throw new NotImplementedException(nameof(CollectToSend));
        }

        public void CollectToQueue(IAppNotification? notification)
        {
            throw new NotImplementedException(nameof(CollectToQueue));
        }

        public Task<Result> DispatchAllCollectedAsync()
        {
            throw new NotImplementedException(nameof(DispatchAllCollectedAsync));
        }

        public void ForgetAllCollected()
        {
            throw new NotImplementedException(nameof(ForgetAllCollected));
        }

        public void StartTransaction() => _transactionStarted = true;

        public async Task CommitTransaction()
        {
            _transactionStarted = false;
            foreach (var notification in _collectedToQueue)
            {
                await QueueAsync(notification);
            }
            _collectedToQueue.Clear();
        }

        public void RollbackTransaction()
        {
            _transactionStarted = false;
            _collectedToQueue.Clear();
        }
        
        /// <summary>
        /// Отправка запроса зарегистрированному обработчику с ожиданием возврата (по сути - прямой вызов). 
        /// По правилам MediatR - <strong>только одному</strong> зарегистрированному.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<Result> SendAsync(IAppRequest? message)
        {
            if(message == null) return Result.Success;
            try
            {
                Result res = await mediator.Send(message);
                return res;
            }
            catch (FrameEntityException ex)
            {
                logger.LogError(ex, _sendError);
                return Result.Error(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, _sendError);
                return Result.Error(ex.Message);
            }
        }

        public async Task<Result> PublishAsync(IAppNotification? notification)
        {
            if(notification == null) return Result.Success;
            try
            {
                using IServiceScope scope = serviceScopeFactory.CreateScope();

                // Здесь получаем MediatR как scoped. Это не ошибка, реально нужно, в связи с тем что 
                // IUserCore scoped. Без этого возникнет ошибка.
                IPublisher _mediator = scope.ServiceProvider.GetRequiredService<IPublisher>();

                // Отдаем полученный Event Mediator-у для публикации зарегистрированным подписчикам
                await _mediator.Publish(notification);
                return Result.Success;
            }
            catch(FrameEntityException ex)
            {
                logger.LogError(ex, _publishError);
                return Result.Error(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, _publishError);
                return Result.Error(ex.Message);
            }
        }

        public async Task<Result> QueueAsync(IAppNotification? notification)
        {
            if(notification == null) return Result.Success;
            try
            {
                if (_transactionStarted)
                {
                    _collectedToQueue.Add(notification);
                }
                else
                {
                    await messageQueue.WriteAsync(notification);
                }

                return Result.Success;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, _queueError);
                return Result.Error(ex.Message);
            }
        }
    }
}
