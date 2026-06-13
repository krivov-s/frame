using Frame.App.FrameNotifications;
using Frame.App.IEntityLoaders;
using Frame.App.IEntityRepositories;
using Frame.App.Security;
using Frame.Domain.Entities.Core.FrameNotifications;
using Frame.Domain.Entities.Core.Security;
using Frame.Shared;

namespace Frame.Infrastructure.FrameNotifications
{
    public class FrameNotificationSender(
        IObjectStorage objectStorage, 
        IGetCurrentUserService? currentUserService,
        IUserLoader userLoader) : IFrameNotificationSender
    {
        private readonly IObjectStorage _objectStorage =
            objectStorage ?? throw new ArgumentNullException(nameof(objectStorage));
        private readonly IGetCurrentUserService _currentUserService =
            currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        private readonly IUserLoader _userLoader =
            userLoader ?? throw new ArgumentNullException(nameof(userLoader));

        public async Task<Result> SendAsync(string userLogin, string theme, string message, int formatCode)
        {
            Result<User> senderResult = await _currentUserService.GetCurrentUserAsync();
            if(senderResult.IsErrorOrNull)
                return senderResult;

            Result<User?> receiverResult = await _userLoader.LoadByNameAsync(userLogin);
            if(receiverResult.IsErrorOrNull)
                return receiverResult;

            FrameNotification frameNotification = new()
            {
                SenderId = senderResult.Value!.Id,
                ReceiverId = receiverResult.Value!.Id,
                DateCreate = DateTime.UtcNow,
                FormatCode = formatCode,
                Theme = theme,
                Message = message,
                IsNew = false
            };

            Result addResult = _objectStorage.Add(frameNotification);
            if(addResult.IsError)
                return addResult;

            Result saveResult = await _objectStorage.SaveChangesAsync();
                return saveResult;
        }

        public async Task<Result> SendAsync(int userId, string theme, string message, int formatCode)
        {
            Result<User> senderResult = await _currentUserService.GetCurrentUserAsync();
            if (senderResult.IsError)
                return senderResult;

            Result<User?> receiverResult = await _userLoader.LoadByIdAsync(userId);
            if (receiverResult.IsError)
                return receiverResult;

            FrameNotification frameNotification = new()
            {
                SenderId = senderResult.Value!.Id,
                ReceiverId = receiverResult.Value!.Id,
                DateCreate = DateTime.UtcNow,
                FormatCode = formatCode,
                Theme = theme,
                Message = message,
                IsNew = false
            };

            Result addResult = _objectStorage.Add(frameNotification);
            if (addResult.IsError)
                return addResult;

            Result saveResult = await _objectStorage.SaveChangesAsync();
            return saveResult;
        }
    }
}
