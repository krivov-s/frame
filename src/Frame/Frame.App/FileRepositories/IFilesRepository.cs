using Frame.Shared;

namespace Frame.App.FileRepositories
{
    public interface IFilesRepository
    {
        /// <summary>
        /// Сохранение файла по заданному ключу
        /// </summary>
        /// <param name="strKey">Полный путь к файлу относительно корневой папки на сервере</param>
        /// <param name="file">Сохраняемый поток</param>
        /// <returns></returns>
        public Task<Result> PutFileAsync(string strKey, Stream file);

        /// <summary>
        /// Чтение файла
        /// </summary>
        /// <param name="strKey">Полный путь к файлу относительно корневой папки на сервере</param>
        /// <returns></returns>
        public Task<Result<byte[]>> GetFileAsync(string strKey);
        
        /// <summary>
        /// Удаление файла
        /// </summary>
        /// <param name="strKey">Полный путь к файлу относительно корневой папки на сервере</param>
        /// <returns></returns>
        public Task<Result> DeleteFileAsync(string strKey);
        
        /// <summary>
        /// Получение списка файлов в заданной папке и всех подпапках
        /// </summary>
        /// <param name="path">Полный путь к папке относительно корня сервера</param>
        /// <returns></returns>
        public Task<Result<List<string>>> LoadFileListAsync(string path);
    }
}
