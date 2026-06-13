
namespace Frame.Shared
{
    /// <summary>
    /// Исключение, которое выбрасывается в случае возникновения ошибок, связанных
    /// с сериализацией / десериализацией списка параметров. 
    /// </summary>
    public class FrameParamListSerializeException(string message, Exception? inner = null) : FrameException(message, inner);
}
