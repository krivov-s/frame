using MediatR;

namespace Frame.App.EventBus.Core
{
    /// <summary>
    /// Наследование от INotification позволяет использовать MediatR.
    /// В случае если MediatR будет изменен на что-то другое, нужно будет просто убрать INotification или заменить на другой базовый интерфейс
    /// </summary>
    public interface IAppNotification : INotification
    {
    }
}
