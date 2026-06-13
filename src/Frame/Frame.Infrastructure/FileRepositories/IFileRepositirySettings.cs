namespace Frame.Infrastructure.FileRepositories;

public interface IFileRepositirySettings
{
    public string GetFileStorageType();
    public string GetDocumentsHomePath();

    public int GetMaxFileSize();
}