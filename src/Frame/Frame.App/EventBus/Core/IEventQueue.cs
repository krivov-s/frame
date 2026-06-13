using Frame.Shared;

namespace Frame.App.EventBus.Core
{
    public interface IEventQueue<TEvent> where TEvent : IAppNotification
    {
        /// <summary>
        /// Запись события в очередь.
        /// </summary>
        /// <param name="event"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task WriteAsync(TEvent @event, CancellationToken cancellationToken = default);
        /// <summary>
        /// Метод ожидает появления нового события. Прерывается если пошел сигнал в CancellationToken
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Result<TEvent>> ReadAsync(CancellationToken cancellationToken = default);
    }
}
