using System.Diagnostics;
using Frame.App.EventBus.Events.Request;
using Frame.App.EventBus.Handlers.Request;
using Frame.Domain.Entities.Core;
using Frame.Shared;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Frame.Domain.Entities.Metadata;

namespace Frame.App.EventBus.Core
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Регистрация событий BeforeSave и AfterSave для всех моделей из Domain.
        /// Регистрация обработчика по-умолчанию BeforeSave для всех моделей из Domain 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBeforeAfterSaveEvents(this IServiceCollection services)
        {
            var factory = new AppEventFactory();

            var assembliesWithEntities = ServiceTools.GetAssemblies(".Domain").ToArray(); 

            var entityTypes = assembliesWithEntities
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(BaseEntity).IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericType && !EntityMetadata.IsProxy(t)); // + из списка типов убираем все Miscrosoft Proxies от LazyLoading

            MethodInfo? methodInfoBeforeSave = typeof(AppEventFactory).GetMethod(nameof(AppEventFactory.RegisterEntityBeforeSaveRequest));
            MethodInfo? methodInfoAfterSave  = typeof(AppEventFactory).GetMethod(nameof(AppEventFactory.RegisterEntityAfterSaveNotification));

            foreach (var entityType in entityTypes)
            {
                // Регистрируем событие BeforeSave
                if (methodInfoBeforeSave != null)
                {
                    var registerMethodBeforeSave = methodInfoBeforeSave.MakeGenericMethod(entityType);
                    if (registerMethodBeforeSave != null)
                    {
                        registerMethodBeforeSave.Invoke(factory, null);
                    }
                }

                // Регистрируем событие AfterSave
                if (methodInfoAfterSave != null)
                {
                    var registerMethodAfterSave = methodInfoAfterSave.MakeGenericMethod(entityType);
                    if (registerMethodAfterSave != null)
                    {
                        registerMethodAfterSave.Invoke(factory, null);
                    }
                }

                // Регистрация обработчика по умолчанию BeforeSave для каждого типа сущности (иначе MediatR каждый раз будет бросать Exceptions. Некрасиво.)
                var requestType = typeof(EntityBeforeSaveRequest<>).MakeGenericType(entityType);
                var handlerType = typeof(DefaultBeforeSaveRequestHandler<>).MakeGenericType(entityType);
                var serviceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(Result));

                if (services.Any(d => d.ServiceType == serviceType) == false)
                {
                    services.AddTransient(serviceType, handlerType);
                }
                else
                {
                    Debug.Print($"Сервис {serviceType} уже зарегистрирован");
                }
            }

            services.AddSingleton<IAppEventFactory>(factory);

            return services;
        }
    }
}
