
namespace Frame.Shared
{
    /// <summary>
    /// Исключение, которое выбрасывается в случае возникновения ошибок, связанных с безопасностью.
    /// </summary>
    public class FrameSecurityException(string message, Exception? inner = null) : FrameException(message, inner);
}
