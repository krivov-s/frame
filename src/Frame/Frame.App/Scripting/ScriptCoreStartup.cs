using Frame.App.IEntityLoaders;
using Frame.App.Security;
using Frame.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Frame.App.Scripting
{
    /// <summary>
    /// Класс, который регистрируется в DependencyInjection как HostedService и вызывается автоматически при старте приложения.
    /// Его назначение - инициализация ScriptCore.
    /// </summary>
    public class ScriptCoreStartup(
        IServiceProvider serviceProvider,
        IEntityScriptLoader entityScriptLoader,
        ILogger<ScriptCoreStartup> logger)
        : IHostedService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly IEntityScriptLoader _entityScriptLoader = entityScriptLoader ?? throw new ArgumentNullException(nameof(entityScriptLoader));
        private readonly ILogger<ScriptCoreStartup> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Используется механизм Scope поскольку IHostedService регистрируется как Singleton, а Singleton не может получать Scoped сервисы.
                using (var scope = _serviceProvider.CreateScope())
                {
                    // Инициализируем системные настройки (в случае их отсутствия)
                    IFrameSettingsLoader frameSettingsLoader = scope.ServiceProvider.GetRequiredService<IFrameSettingsLoader>();
                    await frameSettingsLoader.InitFrameSettingsAsync();
                    
                    IServiceUserContext serviceUserContext = scope.ServiceProvider.GetRequiredService<IServiceUserContext>();
                    await serviceUserContext.UseServiceUserNameAsync();
                    
                    IScriptCore scriptCore = scope.ServiceProvider.GetRequiredService<IScriptCore>();
                    Result resInit = await scriptCore.InitializeAsync(_entityScriptLoader);
                    if (!resInit.IsError)
                    {
                        _logger.LogInformation("Инициализация ScriptCore завершена успешно.");
                    }
                    else
                    {
                        resInit.CheckAndThrow("Ошибка инициализации ScriptCore");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка при инициализации ScriptCore: {0}", ex.Message);
                _logger.LogCritical("ВНИМАНИЕ!!! СЕРВЕР ЗАПУСКАЕТСЯ БЕЗ ФУНКЦИОНИРУЮЩЕГО ScriptCore!!!");
                // _hostApplicationLifetime.StopApplication();
                // throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
