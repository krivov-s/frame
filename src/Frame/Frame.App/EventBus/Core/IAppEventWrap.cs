using Frame.Domain.Events;

namespace Frame.App.EventBus.Core
{
    /// <summary>
    /// Обертка для <see cref="IDomainEvent"/> чтобы сделать их пригодными для работы с интерфейсами и объектами слоя App (в т.ч. с библиотекой <see cref="MediatR"/>)
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public class IAppEventWrap<TEvent>(TEvent @event) : IAppNotification where TEvent : IDomainEvent
    {
        public TEvent Event { get; } = @event;
    }
}
