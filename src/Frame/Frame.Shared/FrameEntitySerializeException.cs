
namespace Frame.Shared
{
    /// <summary>
    /// Исключение, которое выбрасывается в случае возникновения ошибок, связанных
    /// с сериализацией / десериализацией объекта системы. 
    /// </summary>
    public class FrameEntitySerializeException(string message, Exception? inner = null) : FrameException(message, inner);
}
