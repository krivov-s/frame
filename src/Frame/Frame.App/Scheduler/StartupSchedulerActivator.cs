using Frame.App.IEntityRepositories;
using Frame.App.Security;
using Frame.Domain.Entities.Core.Scripting;
using Frame.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Frame.App.Scheduler;

public class StartupSchedulerActivator(IServiceScopeFactory serviceScopeFactory, 
    ILogger<StartupSchedulerActivator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("StartupSchedulerActivator: активация всех ScriptCommand, настроенных на запуск по расписанию");
        
        try
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            IObjectStorage storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
            SchedulerService scheduler = scope.ServiceProvider.GetRequiredService<SchedulerService>();
            IServiceUserContext serviceUserContext = scope.ServiceProvider.GetRequiredService<IServiceUserContext>();
            
            await serviceUserContext.UseServiceUserNameAsync();
            
            var res = await storage.GetListAsync<ScriptCommand>(spec => 
                spec.Where(x => x.ScheduleEnabled));

            if (res.IsErrorOrNull) return;

            int iCount = 0;
            foreach (var cmd in res.Value!)
            {
                Result resSch = await scheduler.ScheduleCommandAsync(cmd);
                if(!resSch.IsError) iCount++;
            }
            logger.LogInformation("StartupSchedulerActivator: количество активированных ScriptCommand: {Count}", iCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StartupSchedulerActivator: ошибка активации преднастроенных операций: {Message}", ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
