namespace Frame.Domain.Params;

/// <summary>
/// Класс для обеспечения возможности загрузки файла в список параметров 
/// </summary>
public class FileParam
{
    /// <summary>
    /// Имя файла
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// Расширение файла маленькими буквами
    /// </summary>
    public string FileExt => Path.GetExtension(FileName).ToLower();
        
    /// <summary>
    /// Содержимое файла
    /// </summary>
    public byte[] FileData { get; set; } = [];
}