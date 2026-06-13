namespace Frame.Infrastructure.FileRepositories;

public static class FileStorageType
{
    public const string ParamName_FileStorageType = "FileStorageType";
    
    public const string TypeNetworkFolder = "NetworkFolder";   // обычная папка (локальная или сетевая)
    public const string TypeSmb = "SMB";                       // Samba-сервер
    public const string TypeS3 = "S3";                         // S3 сервис (Amazon, Yandex)

    public static void Verify(string value)
    {
        if (value != TypeNetworkFolder 
            && value != TypeSmb
            && value != TypeS3)
        {
            throw new Exception(
                $"{nameof(FileStorageType)}: недопустимое значение {nameof(FileStorageType)} = '{value}'");
        }
    }
}