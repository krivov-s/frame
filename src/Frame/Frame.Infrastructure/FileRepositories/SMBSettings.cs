namespace Frame.Infrastructure.FileRepositories
{
    public class SMBSettings: IFileRepositirySettings
    {
        /// <summary>
        /// Отдельно значение User прописывается в переменных окружения в Vault.
        /// </summary>
        public string Login { get; set; } = "";

        /// <summary>
        /// Отдельно значение Password прописывается в переменных окружения в Vault.
        /// </summary>
        public string Password { get; set; } = "";

        /// <summary>
        /// Отдельно значение Path прописывается в переменных окружения в Vault.
        /// </summary>
        public string Path { get; set; } = "";
        
        /// <summary>
        /// Путь к папке внутри сервера, в которой будут храниться документы
        /// </summary>
        public string DocumentsHomePath { get; set; } = "";

        /// <summary>
        /// Максимально разрешенный размер файла
        /// </summary>
        public int MaxFileSize { get; set; } = 2048000;
        
        public void Verify()
        {
            if (Path == "")
            {
                throw new Exception(
                    $"{nameof(SMBSettings)}: отсутствует значение {nameof(Path)}");
            }
            if (Login == "")
            {
                throw new Exception(
                    $"{nameof(SMBSettings)}: отсутствует значение {nameof(Login)}");
            }
            if (Password == "")
            {
                throw new Exception(
                    $"{nameof(SMBSettings)}: отсутствует значение {nameof(Password)}");
            }
        }

        public string GetFileStorageType()
        {
            return FileStorageType.TypeSmb;
        }

        public string GetDocumentsHomePath()
        {
            return DocumentsHomePath;
        }
        
        public int GetMaxFileSize() => MaxFileSize;
    }
}
