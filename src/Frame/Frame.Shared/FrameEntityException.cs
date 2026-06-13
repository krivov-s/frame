
namespace Frame.Shared
{
    /// <summary>
    /// Исключение, которое выбрасывается в случае возникновения ошибок, связанных непосредственно с бизнес-логикой моделей (в методах OnBefore/OnAfter/Save/Delete, 
    /// просто внутри моделей, возможно - в слое App при обработке моделей)
    /// </summary>
    public class FrameEntityException(string message, Exception? inner = null) : FrameException(message, inner);
}
