using Frame.Shared;
using MediatR;

namespace Frame.App.EventBus.Core
{
    /// <summary>
    /// Наследование от IRequest позволяет использовать MediatR + обязывает возвращать в качестве ответа на запрос Result.
    /// В случае если MediatR будет изменен на что-то другое, нужно будет просто убрать IRequest или заменить на другой базовый интерфейс
    /// </summary>
    public interface IAppRequest : IRequest<Result>
    {
    }
}
