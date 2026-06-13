using FluentValidation;
using Frame.App.Scripting;
using Frame.App.Security;
using Frame.Domain.Entities.Core.Scripting;
using Frame.Shared;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Frame.App.Scheduler;

public class ScriptCommandJob(IServiceUserContext serviceUserContext, IScriptCore scriptCore, 
    ILogger<ScriptCommandJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        string prefix = nameof(ScriptCommandJob);
        var cmd = context.MergedJobDataMap["command"];
        
        ScriptCommand? command = (ScriptCommand)cmd;
        if (command == null)
        {
            string err = 
                $"{prefix}: на исполнение получена команда неизвестного типа {cmd.GetType().Name}";
            logger.LogError("{Err}", err );
            return;
        }

        try
        {
            logger.LogInformation("{Prefix}: запуск на исполнение по расписанию операции {Command} (Id = {Id})", 
                prefix, command.Description, command.Id.ToString());

            // Операция выполняется под системным пользователем!
            await serviceUserContext.UseServiceUserNameAsync();
            
            await scriptCore.ExecuteScriptCommandAsync(command.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "{Prefix}: ошибка при запуске на исполнение по расписанию операции {Command}: {Message}", 
                prefix, command.Description, ex.Message);
        }
    }
}
