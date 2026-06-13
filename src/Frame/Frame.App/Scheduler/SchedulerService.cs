using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core.Scripting;
using Frame.Shared;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Frame.App.Scheduler;

public class SchedulerService(ISchedulerFactory schedulerFactory, IObjectStorageProvider objectStorageProvider, 
    ILogger<SchedulerService> logger)
{
    /// <summary>
    /// Синхронизировать запись в Scheduler с параметрами ScriptCommand. Это приведет или к запуску/перезапуску
    /// или к остановке расписания для данной команды.
    /// </summary>
    /// <param name="command"></param>
    /// <returns>Result.Success, если все удалось и расписание синхронизировано с параметрами ScriptCommand</returns>
    public async Task<Result> ScheduleCommandAsync(ScriptCommand command)
    {
        try
        {
            var scheduler = await schedulerFactory.GetScheduler();
            ScriptCommandJobStatus status = await _getScriptCommandJobStatus(command, scheduler);

            if (!command.ScheduleEnabled)
            {
                // В команде признак активности сброшен - нужно убедиться что операция не активна в планировщике
                if (!status.IsActive) return Result.Success;
                
                // Планировщик активен - деактивируем и выходим
                Result resUnsch = await _unscheduleCommandAsync(command, scheduler);
                return resUnsch;
            }
            
            // Здесь мы если требуется наличие активного расписания для операции.
            // 1. Проверяем, есть задача уже активна - проверяем CronExpression
            if (status.IsActive)
            {
                // 1.1 Проверяем совпадение CronExpression
                if(status.CronExpression == command.CronExpression) return Result.Success;
                
                // 1.2 Не паримся с отдельными триггерами: удаляем задачу целиком
                Result resUnsch = await _unscheduleCommandAsync(command, scheduler);
                if(resUnsch.IsError) return resUnsch;
            }
            
            // 2. Создаем новую задачу в планировщике
            var job = JobBuilder.Create<ScriptCommandJob>()
                .WithIdentity(command.Key)
                .UsingJobData(new JobDataMap { { "command", command } })
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"{command.Key}-trigger")
                .WithCronSchedule(command.CronExpression)
                .Build();
            
            await scheduler.ScheduleJob(job, trigger);
            logger.LogInformation("Активирован запуск по расписанию для скрипт-операции {Command}. Cron = [{Cron}]", 
                command.Description, command.CronExpression);
            
            return Result.Success;
        }
        catch (Exception ex)
        {
            string err = $"Ошибка при активации запуска по расписанию для скрипт-операции {command}: {ex.Message}";
            logger.LogError(ex, "{Err}", err);
            return Result.Error(err, ex);
        }
    }

    private async Task<Result> _unscheduleCommandAsync(ScriptCommand command, IScheduler scheduler)
    {
        try
        {
            var jobKey = new JobKey(command.Key);
            await scheduler.DeleteJob(jobKey);
            logger.LogInformation("Деактивирован запуск по расписанию для скрипт-операции {Command}", 
                command.Description);
            return Result.Success;
        }
        catch (Exception ex)
        {
            string err = $"Ошибка при деактивации запуска по расписанию для скрипт-операции {command}: {ex.Message}";
            logger.LogError(ex, "{Err}", err);
            return Result.Error(err, ex);
        }
    }

    /// <summary>
    /// Получение статуса операции в планировщике. CronExpression устанавливается только если триггер один.
    /// Если триггеров нет, или их несколько - CronExpression не будет установлен. 
    /// </summary>
    /// <param name="command">Операция</param>
    /// <param name="scheduler">Экземпляр планировщика</param>
    private static async Task<ScriptCommandJobStatus> _getScriptCommandJobStatus(ScriptCommand command, IScheduler scheduler)
    {
        ScriptCommandJobStatus scriptCommandJobStatus = new();
        
        JobKey jobKey = new JobKey(command.Key);
        scriptCommandJobStatus.IsActive = await scheduler.CheckExists(jobKey);

        scriptCommandJobStatus.Triggers = await scheduler.GetTriggersOfJob(jobKey);

        if (scriptCommandJobStatus.Triggers.Count == 1)
        {
            ITrigger trigger = scriptCommandJobStatus.Triggers.First();
            if (trigger is ICronTrigger cronTrigger)
            {
                scriptCommandJobStatus.CronExpression = cronTrigger.CronExpressionString ?? "";
            }
        }
        return scriptCommandJobStatus;
    }
}

public class ScriptCommandJobStatus
{
    /// <summary>
    /// Операция в планировщике активна
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Cron-выражение, установленное для операции в планировщике
    /// </summary>
    public string CronExpression { get; set; } = "";

    /// <summary>
    /// Список триггеров для операции
    /// </summary>
    public IReadOnlyCollection<ITrigger> Triggers { get; set; } = [];

}