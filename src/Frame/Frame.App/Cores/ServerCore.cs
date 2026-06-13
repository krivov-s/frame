using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Params;
using Frame.Shared;

namespace Frame.App.Cores
{
    public class ServerCore : IServerCore
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private static ParamList? _systemParamList;

        public ServerCore(IUserProfileRepository userProfileRepository)
        {
            ArgumentNullException.ThrowIfNull(userProfileRepository);

            _userProfileRepository = userProfileRepository;
            if(_systemParamList == null)
            {
                ReloadSystemParamList();
            }
        }

        public Result ReloadSystemParamList()
        {
            try
            {
                Result<ParamList> resultParamList = _userProfileRepository.LoadParamList(User.SystemUserName);
                resultParamList.CheckAndThrow("Загрузка SystemParamList из БД");
                _systemParamList = resultParamList.Value!;
                return Result.Success;
            }
            catch (Exception ex)
            {
                return Result.Error(ex.ToString());
            }
        }

        public ParamList SystemParamList { get => (_systemParamList == null) ? new() : _systemParamList; }

    }
}
