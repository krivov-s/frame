using Frame.Domain.Entities.Core.Security;
using Frame.Shared;

namespace Frame.App.IEntityLoaders
{
    public interface IUserLoader
    {
        /// <summary>
        /// Загрузка пользователя по имени (login) с полным комплектом вложенных объектов (роли, требования, права доступа к роли).
        /// </summary>
        /// <param name="strName">Имя (login) пользователя</param>
        /// <param name="forceReload">Перезачитка данных из БД (без этого флага будет возращено значения из DBContext)</param>
        /// <returns><see cref="User">Пользователь</see></returns>
        /// <exception cref="Exception">Пользователь не найден или существует более одного пользователя с таким именем</exception>
        public Result<User?> LoadByName(string strName, bool forceReload = false);
        /// <summary>
        /// Загрузка пользователя по имени (login) с полным комплектом вложенных объектов (роли, требования, права доступа к роли). Async - версия.
        /// </summary>
        /// <param name="strName">Имя (login) пользователя</param>
        /// <param name="forceReload">Перезачитка данных из БД (без этого флага будет возращено значения из DBContext)</param>
        /// <returns><see cref="User">Пользователь</see></returns>
        /// <exception cref="Exception">Пользователь не найден или существует более одного пользователя с таким именем</exception>
        public Task<Result<User?>> LoadByNameAsync(string strName, bool forceReload = false);
        /// <summary>
        /// Загрузка пользователя по нормализованному имени (login) с полным комплектом вложенных объектов (роли, требования, права доступа к роли).
        /// </summary>
        /// <param name="strNormalizedName">Имя (login) пользователя</param>
        /// <param name="forceReload">Перезачитка данных из БД (без этого флага будет возращено значения из DBContext)</param>
        /// <returns><see cref="User">Пользователь</see></returns>
        /// <exception cref="Exception">Пользователь не найден или существует более одного пользователя с таким именем</exception>
        public Task<Result<User?>> LoadByNormalizedNameAsync(string strNormalizedName, bool forceReload = false);
        /// <summary>
        /// Загрузка пользователя по его id с полным комплектом вложенных объектов (роли, требования, права доступа к роли).
        /// </summary>
        /// <param name="id">Идентификатор пользователя</param>
        /// <param name="forceReload">Перезачитка данных из БД (без этого флага будет возращено значения из DBContext)</param>
        /// <returns><see cref="User">Пользователь</see></returns>
        /// <exception cref="Exception">Пользователь с таким id не найден</exception>
        public Task<Result<User?>> LoadByIdAsync(int id, bool forceReload = false);
    }
}
