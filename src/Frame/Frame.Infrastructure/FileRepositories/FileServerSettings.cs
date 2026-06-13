namespace Frame.Infrastructure.FileRepositories;

public class FileServerSettings: IFileRepositirySettings
{
    public string FileServerPath { get; set; } = "";
    public string DocumentsHomePath { get; set; } = "";
    
    /// <summary>
    /// Максимально разрешенный размер файла
    /// </summary>
    public int MaxFileSize { get; set; } = 2048000;

    public void Verify()
    {
        if (FileServerPath == "")
        {
            throw new Exception(
                $"{nameof(FileServerSettings)}: отсутствует значение {nameof(FileServerPath)}");
        }
    }

    public string GetFileStorageType()
    {
        return FileStorageType.TypeNetworkFolder;
    }

    public string GetDocumentsHomePath()
    {
        return DocumentsHomePath;
    }

    public int GetMaxFileSize() => MaxFileSize;
}