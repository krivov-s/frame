using Microsoft.Extensions.DependencyInjection;
using Frame.App.Cores;
using Frame.App.EntityTemplates;
using Frame.App.EventBus.Core;
using Frame.App.Scheduler;
using Frame.App.Scripting;
using Frame.Shared;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Frame.App
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Здесь будем добавлять все неободимые зависимости.
        /// <para>
        /// Необходимо, чтобы любые другие сборки уровня Application в названии содержали `.App`.
        /// Только в сборках, содержащих в имени `.App` будет осуществляться поиск валидаторов, событий EventBus
        /// </para>
        /// </summary>
        /// <param name="services"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IServiceCollection AddFrameApplicationServices(this IServiceCollection services, ILogger? logger = null)
        {
            logger?.LogInformation("Конфигурирование FrameApplicationServices...");

            _forceLoadAssemblies();
            
            // Здесь убедимся, что загружены все сборки уровня Application
            ServiceTools.LoadAssembliesToApplication("*.App.dll");
            
            services.AddScoped<IUserCore, UserCore>();
            services.AddScoped<IServerCore, ServerCore>();
            services.AddTransient<IScriptCore, ScriptCore>();
            services.AddHostedService<ScriptCoreStartup>();
            services.AddTransient<AppCoreProvider>();
            
            // Реализации событий MediatR. Ищем во всех сборках, не только на уровне Application.
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            // Именно здесь подхватываются из сборок все объекты, реализующие интерфейс IRequestHandler 
            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssemblies(assemblies); });

            // Очередь сообщений для реализации EventBus на уровне Application
            services.AddBeforeAfterSaveEvents();
            
            services.AddHostedService<EventQueueProcessor<IAppNotification>>();
            
            // Сервис по отправке сообщений делаем Scoped, чтобы была возможность накапливать и
            // отправлять сообщения группами: например в рамках общей транзакции из разных сервисов
            // (создание проводки - проведение проводки)
            // НЕТ! ПОКА НЕ ДЕЛАЕМ! ПРИВЯЗЫВАЕМ К ТРАНЗАКЦИИ В ObjectStorage!
            services.AddTransient<IAppEventDispatcher, AppEventDispatcher>();

            // Добавляем Quartz.Net sheduler
            services.AddQuartz();
            services.AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);
            
            services.AddScoped<SchedulerService>();
            services.AddHostedService<StartupSchedulerActivator>();

            // Сервис по работе с шаблонами объектов системы
            services.AddTransient<IEntityTemplateService, EntityTemplateService>();

            // Добавление всех расширений шаблонов документов
            services.AddTemplateCustomization(logger);
            
            logger?.LogInformation("Конфигурирование FrameApplicationServices успешно завершено");
            
            return services;
        }
        
        // Обеспечиваем принудительную загрузку сборок, содержащих необходимые библиотечные типы
        // Это нужно, чтобы эти типы сразу же были доступны в скриптах
        private static void _forceLoadAssemblies()
        {
            var x1 = typeof(MailKit.Net.Smtp.SmtpClient).Assembly;
            var x2 = typeof(MimeKit.MimeMessage).Assembly;
        }
        
    }
}
