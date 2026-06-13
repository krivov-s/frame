using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Frame.Infrastructure.DBContext;
using Frame.Shared;
using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core.Params;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Params;

namespace Frame.Infrastructure.EntityRepositories
{
    public class EfUserProfileRepository(NoSecurityDbContext dbContext, ILogger<EfUserProfileRepository> logger) : IUserProfileRepository
    {
        private readonly NoSecurityDbContext _dbContext = dbContext;
        private  readonly ILogger<EfUserProfileRepository> _logger = logger;

        public Result<ParamList> LoadParamList(string loginUser)
        {
            Result res = CheckLoginParam(loginUser);
            if (res.IsError) return Result<ParamList>.Error(res.ErrorResult);
            
            try
            {
                IQueryable<UserProfile> query = BuildQuery(loginUser);
                UserProfile? entity = query.FirstOrDefault();
                if (entity == null)
                {
                    return Result<ParamList>.Success(new ParamList());
                }
                else
                {
                    try
                    {
                        return Result<ParamList>.Success(entity.GetParamList());
                    }
                    catch (FrameParamListSerializeException ex)
                    {
                        // Произошла ошибка десериализации. Мы при этом ничего не мочим, просто возвращаем чистый список параметров
                        string strError = $"Не удалось десериализовать список параметров пользователя {loginUser}: {ex.Message}. Возвращаем пустой список.";
                        _logger.LogError(ex, "{Err}", strError);
                        return Result<ParamList>.Success(new ParamList());
                    }
                }
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка чтения списка параметров из БД: {ex}";
                _logger.LogError("{strError}", strError);
                return Result<ParamList>.Error(strError);
            }
        }

        public async Task<Result<UserProfile>> LoadUserProfileAsync(string loginUser)
        {
            Result res = CheckLoginParam(loginUser);
            if (res.IsError) return Result<UserProfile>.Error(res.ErrorResult);
            
            try
            {
                IQueryable<UserProfile> query = BuildQuery(loginUser);
                UserProfile? entity = await query.FirstOrDefaultAsync();
                return ProcessResult(loginUser, entity);
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка чтения списка параметров из БД: {ex}";
                _logger.LogError("{strError}", strError);
                return Result<UserProfile>.Error(strError);
            }
        }

        public Result<UserProfile> LoadUserProfile(string loginUser)
        {
            Result res = CheckLoginParam(loginUser);
            if (res.IsError) return Result<UserProfile>.Error(res.ErrorResult);
            
            try
            {
                IQueryable<UserProfile> query = BuildQuery(loginUser);
                UserProfile? entity = query.FirstOrDefault();
                return ProcessResult(loginUser, entity);
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка чтения списка параметров из БД: {ex}";
                _logger.LogError("{strError}", strError);
                return Result<UserProfile>.Error(strError);
            }
        }

        private static Result<UserProfile> ProcessResult(string loginUser, UserProfile? entity)
        {
            if (entity == null)
            {
                string strParamListName = (loginUser == User.SystemUserName) 
                    ? "Системный список параметров" 
                    : $"Список параметров пользователя {loginUser}";
                return Result<UserProfile>.Error($"{strParamListName} не найден в БД!");
            }

            return Result<UserProfile>.Success(entity);
        }

        private IQueryable<UserProfile> BuildQuery(string loginUser)
        {
            IQueryable<UserProfile> query = _dbContext.Set<UserProfile>();
            return query.Where(pl => pl.User!.Login == loginUser);
        }

        public async Task<Result> SaveUserProfileAsync(UserProfile entity)
        {
            try
            {
                if (entity != null)
                {
                    entity.User = null;     // Обнуляем пользователя, чтобы сохранить только профайл
                    if (entity.Id != 0)
                    {
                        _dbContext.Update(entity);
                    }
                    else
                    {
                        _dbContext.Add(entity);
                    }

                    await _dbContext.SaveChangesAsync();
                }
                // TODO: Future: Optimization: реализовать обновление списка параметров пользователя в кэше IUserCore->UserParamList и системного списка в IServerCore->ReloadSystemParamList
                //if (entity.UserId == 0)
                //{
                //    _serverCore.ReloadSystemParamList();
                //}
                //else if (entity.User != null)
                //{
                //}
                return Result.Success;
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка сохранения списка параметров в БД: {ex}";
                _logger.LogError("{strError}", strError);
                return Result.Error(strError);
            }
        }

        public Result SaveUserProfile(UserProfile entity)
        {
            try
            {
                _dbContext.CheckAddToContext(entity);
                _dbContext.SaveChanges();
                //if (entity.UserId == 0)
                //{
                //    _serverCore.ReloadSystemParamList();
                //}
                //else if (entity.User != null)
                //{
                //}
                return Result.Success;
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка сохранения списка параметров в БД: {ex}";
                _logger.LogError("{strError}", strError);
                return Result.Error(strError);
            }
        }
        
        private Result CheckLoginParam(string loginUser)
        {
            if (loginUser.Length == 0)
            {
                string err = "Не передан login пользователя";
                _logger?.LogError(err);
                return Result.Error(err);
            }

            return Result.Success;
        }
    }
}
