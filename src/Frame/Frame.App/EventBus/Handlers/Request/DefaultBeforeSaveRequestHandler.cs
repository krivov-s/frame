using Frame.App.EventBus.Events.Request;
using Frame.Domain.Entities.Core;
using Frame.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Frame.App.EventBus.Handlers.Request
{
    public class DefaultBeforeSaveRequestHandler<TEntity> : IRequestHandler<EntityBeforeSaveRequest<TEntity>, Result> where TEntity : BaseEntity
    {
        private readonly ILogger<DefaultBeforeSaveRequestHandler<TEntity>> _logger;

        public DefaultBeforeSaveRequestHandler(ILogger<DefaultBeforeSaveRequestHandler<TEntity>> logger)
        {
            _logger = logger;
        }

        public Task<Result> Handle(EntityBeforeSaveRequest<TEntity> request, CancellationToken cancellationToken)
        {
            // _logger.LogInformation("Обработчик по-умолчанию BeforeSave для типа {t}", typeof(TEntity).Name);
            return Task.FromResult(Result.Success);
        }
    }
}
