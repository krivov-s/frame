namespace Frame.Shared;

/// <summary>
/// Базовое исключение, выброшенное системой (Frame)
/// </summary>
/// <param name="message"></param>
/// <param name="inner"></param>
public class FrameException(string message, Exception? inner = null) : Exception(message, inner);
