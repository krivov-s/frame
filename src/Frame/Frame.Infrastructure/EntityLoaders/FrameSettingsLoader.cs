using Frame.App.IEntityLoaders;
using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Params;
using Frame.Domain.Entities.Core.Security;
using Frame.Infrastructure.DBContext;
using Frame.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frame.Infrastructure.EntityLoaders;

public class FrameSettingsLoader(IDbContextFactory<NoSecurityDbContext> dbContextFactory,
                                 ILogger<FrameSettingsLoader> logger) : IFrameSettingsLoader
{
    private readonly IDbContextFactory<NoSecurityDbContext> _dbContextFactory = dbContextFactory 
        ?? throw new ArgumentNullException(nameof(dbContextFactory));
    private readonly ILogger<FrameSettingsLoader> _logger = logger 
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Result<FrameSettings>> InitFrameSettingsAsync()
    {
        try
        {
            NoSecurityDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            IQueryable<FrameSettings> query = dbContext.Set<FrameSettings>();
            FrameSettings? settings = await GetSettingsAsync(query);
            if (settings == null)
            {
                _logger.LogWarning("Системные настройки отсутствуют: создаем новую запись.");
                settings = new FrameSettings();

                IQueryable<User> queryUser = dbContext.Set<User>(); 
                queryUser = queryUser.Include(u => u.UserProfile);

                User? systemUser = await queryUser.FirstOrDefaultAsync(q => q.Login == User.SystemUserName);
                if (systemUser == null)
                {
                    _logger.LogWarning("Системный пользователь отсутствует: создаем нового.");
                    systemUser = new User()
                    {
                        Login = User.SystemUserName, 
                    };
                    systemUser.SetPassword("_");
                    
                    UserProfile profile = new()
                    {
                        User = systemUser
                    };
                    systemUser.UserProfile = profile;
                    
                    dbContext.Add(systemUser);
                    dbContext.Add(profile);

                    if (await dbContext.SaveChangesAsync() > 0)
                    {
                        _logger.LogInformation("Новый системный пользователь сохранен в БД. Id={0}", systemUser.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Команда сохранения не привела к созданию в БД нового системного пользователя!");
                    }
                }

                if (systemUser.UserProfile is null)
                {
                    throw new FrameException("У системного пользователя отсутствует Id профиля! Продолжение невозможно!");
                }
                settings.SystemUserProfile = systemUser.UserProfile;
                settings.SystemUserProfileId = systemUser.UserProfile.Id;

                dbContext.Add(settings);

                if (await dbContext.SaveChangesAsync() > 0)
                {
                    _logger.LogInformation("Новый экземпляр системных настроек сохранен в БД. Id={0}", settings.Id);
                }
                else
                {
                    _logger.LogWarning("Команда сохранения не привела к созданию в БД нового экземпляра системных настроек!");
                }

            }

            return Result<FrameSettings>.Success(settings);
        }
        catch (Exception ex)
        {
            string err = $"Ошибка чтения/инициализации FrameSettings: {ex.Message}"; 
            _logger.LogError(ex, "{0}", err);
            return Result<FrameSettings>.Error(err, ex);
        }
    }

    public async Task<Result<FrameSettings>> LoadFrameSettingsAsync(IObjectStorage objectStorage)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (objectStorage == null)
        {
            return Result<FrameSettings>.Error("Не передан ObjectStorage!");
        }

        Result<List<FrameSettings>> resSettings = 
            await objectStorage.GetListAsync<FrameSettings>(spec => spec
                .Include(fs => fs.SystemUserProfile)
                .Include(fs => fs.ReportFileDocumentType)
                .Build()); 

        if (resSettings.IsError || resSettings.Value == null)
        {
            return Result<FrameSettings>.Error(resSettings);
        }

        List<FrameSettings> list = resSettings.Value;
        
        if (list.Count == 0)
        {
            string err = $"В БД отсутствует запись о системных настройках Settings";
            _logger.LogError(err);
            return Result<FrameSettings>.Error(err);
        }

        if (list.Count > 1)
        {
            string err = $"В БД внесено несколько ({list.Count}) записей о системных настройках Settings";
            _logger.LogError(err);
            return Result<FrameSettings>.Error(err);
        }

        return Result<FrameSettings>.Success(list[0]);
    }
    
    // Функция получения settings. Если не найдены - создаем новые
    private static Task<FrameSettings?> GetSettingsAsync(IQueryable<FrameSettings> query)
    {
        query = query
            .Include(s => s.SystemUserProfile)
            .Include(s => s.ReportFileDocumentType);
        return query.OrderBy(fs => fs.Id).FirstOrDefaultAsync();
    }
}