using Frame.Shared;

namespace Frame.App.IEntityRepositories
{
    public interface IObjectStorageProvider
    {
        /// <summary>
        /// Получение нового объектного хранилища. 
        /// <para>
        /// В случае работы с EF.Core основная задача - получение нового экземпляра контекста и передача его во вновь создаваемый IObjectStorage. 
        /// </para>
        /// </summary>
        /// <returns></returns>
        public Result<IObjectStorage> GetObjectStorage();
    }
}
