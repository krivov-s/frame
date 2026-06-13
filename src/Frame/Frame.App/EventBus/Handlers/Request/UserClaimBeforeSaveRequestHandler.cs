using Frame.App.EventBus.Events.Request;
using Frame.Domain.Entities.Core.Security;
using Frame.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Frame.App.EventBus.Handlers.Request
{
    public class UserClaimBeforeSaveRequestHandler : IRequestHandler<EntityBeforeSaveRequest<UserClaim>, Result>
    {
        private readonly ILogger<UserClaimBeforeSaveRequestHandler> _logger;

        public UserClaimBeforeSaveRequestHandler(ILogger<UserClaimBeforeSaveRequestHandler> logger)
        {
            _logger = logger;
        }

        public Task<Result> Handle(EntityBeforeSaveRequest<UserClaim> request, CancellationToken cancellationToken)
        {
            if (request.Entity == null)
            {
                return Task.FromResult(Result.Error("Передан нулевой объект!!!"));
            }

            UserClaim entity = request.Entity;
            //_logger.LogInformation("Событие ПЕРЕД сохранением UserClaim {name}", entity.Key);

            return Task.FromResult(Result.Success);
        }
    }
}
