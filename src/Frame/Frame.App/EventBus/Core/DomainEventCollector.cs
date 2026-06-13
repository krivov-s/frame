using Frame.Domain.Events;

namespace Frame.App.EventBus.Core
{
    public class DomainEventCollector : IDomainEventCollector
    {
        public List<IAppNotification> EventsToQueue = [];
        public List<IAppNotification> EventsToSend = [];

        /// <summary>
        /// Оборачивает доменное событие в <see cref="IAppEventWrap{TEvent}"/> и переправляет Диспетчеру событий
        /// </summary>
        /// <param name="domainEvent"></param>
        /// <returns></returns>
        public void CollectToQueue<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
        {
            IAppEventWrap<TEvent> wEvent = new(domainEvent);

            EventsToQueue.Add(wEvent);
        }

        public void CollectToSend<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
        {
            IAppEventWrap<TEvent> wEvent = new(domainEvent);

            EventsToSend.Add(wEvent);
        }
    }
}
