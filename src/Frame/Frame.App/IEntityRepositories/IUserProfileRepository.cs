using Frame.Domain.Entities.Core.Params;
using Frame.Domain.Params;
using Frame.Shared;

namespace Frame.App.IEntityRepositories
{
    /// <summary>
    /// Текущая реализация осуществляет работу с БД в обход стандартной Security. Есть тема подумать, насколько это правильно...
    /// </summary>
    public interface IUserProfileRepository
    {
        /// <summary>
        /// Считывает из БД список параметров заданного пользователя и возвращает экземпляр <see cref="ParamList"/>.
        /// Если у заданного пользователя список параметров отсутствует - возвращает пустой новый <see cref="ParamList"/>.
        /// </summary>
        /// <param name="loginUser"></param>
        /// <returns></returns>
        public Result<ParamList> LoadParamList(string loginUser);
        public Task<Result<UserProfile>> LoadUserProfileAsync(string loginUser);
        public Result<UserProfile> LoadUserProfile(string loginUser);
        public Task<Result> SaveUserProfileAsync(UserProfile entity);
        public Result SaveUserProfile(UserProfile entity);
    }
}
