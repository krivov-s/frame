using Frame.Domain.Entities.Core.Security;
using Frame.Shared;

namespace Frame.App.IEntityLoaders
{
    public interface IRoleLoader
    {
        /// <summary>
        /// Загрузка роли по имени.
        /// </summary>
        /// <param name="strName">Имя (login) роли</param>
        /// <returns><see cref="Role">Роль</see></returns>
        /// <exception cref="Exception">Роль не найдена или существует более одной роли с таким именем</exception>
        public Task<Result<Role>> LoadByNameAsync(string strName);
        /// <summary>
        /// Загрузка роли по нормализованному имени.
        /// </summary>
        /// <param name="strNormalizedName">Имя (login) пользователя</param>
        /// <returns><see cref="Role">Роль</see></returns>
        /// <exception cref="Exception">Роль не найдена или существует более одной роли с таким именем</exception>
        public Task<Result<Role>> LoadByNormalizedNameAsync(string strNormalizedName);
        /// <summary>
        /// Загрузка роли по id
        /// </summary>
        /// <param name="id">Идентификатор роли</param>
        /// <returns><see cref="Role">Роль</see></returns>
        /// <exception cref="Exception">Роль с таким id не найдена</exception>
        public Task<Result<Role>> LoadByIdAsync(int id);
    }
}
