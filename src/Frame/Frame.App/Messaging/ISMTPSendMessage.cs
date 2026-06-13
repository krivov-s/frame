using Frame.Shared;

namespace Frame.App.Messaging
{
    public interface ISMTPSendMessage
    {
        /// <summary>
        /// Отправка сообщения.
        /// </summary>
        /// <param name="emailSettings">Даныне для отправки.</param>
        /// <returns></returns>
        public Task<Result> SendMessage(EmailSettings emailSettings);
    }
}
