namespace Frame.Infrastructure.FileRepositories
{
    public class S3Settings: IFileRepositirySettings
    {
        public string ServiceUrl { get; set; } = "";
        public string AccessKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string BucketName { get; set; } = "";
        public int MaxFileSize { get; set; } = 2048000;

        public void Verify()
        {
            throw new NotImplementedException();
        }

        public string GetFileStorageType()
        {
            return FileStorageType.TypeS3;
        }

        public string GetDocumentsHomePath()
        {
            throw new NotImplementedException();
        }
        
        public int GetMaxFileSize() => MaxFileSize;
    }
}
