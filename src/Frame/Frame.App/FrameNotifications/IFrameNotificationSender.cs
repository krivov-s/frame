using Frame.Shared;

namespace Frame.App.FrameNotifications
{
    public interface IFrameNotificationSender
    {
        public Task<Result> SendAsync(string userLogin, string theme, string message, int formatCode);
        public Task<Result> SendAsync(int userId, string theme, string message, int formatCode);
    }
}
