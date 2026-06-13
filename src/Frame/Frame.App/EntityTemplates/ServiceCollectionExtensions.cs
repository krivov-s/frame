using System.Diagnostics;
using System.Reflection;
using Frame.App.EntityTemplates;
using Frame.App.EventBus.Core;
using Frame.App.EventBus.Events.Request;
using Frame.App.EventBus.Handlers.Request;
using Frame.Domain.Entities.Metadata;
using Frame.Shared;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Frame.App.EntityTemplates;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрация в DI всех расширений сериализации/десериализации бизнес-объектов системы
    /// (реализации интерфейса <see cref="ITemplateCustomization{TEntity}"/>)
    /// Поиск осуществляется во всех загруженных сборках из домена .App
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddTemplateCustomization(this IServiceCollection services, ILogger? logger)
    {
        var assembliesWithEntities = ServiceTools.GetAssemblies(".App").ToArray(); 

        var types = assembliesWithEntities
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false } 
                        && !EntityMetadata.IsProxy(t)); // + из списка типов убираем все Miscrosoft Proxies от LazyLoading

        foreach (var implementationType in types)
        {
            // Проверяем типы на реализацию нужного интерфейса
            var implementedInterfaces = implementationType.GetInterfaces()
                .Where(i => i.IsConstructedGenericType &&
                            i.GetGenericTypeDefinition() == typeof(ITemplateCustomization<>));

            if (implementedInterfaces != null && implementedInterfaces.Any())
            {
                foreach (var iface in implementedInterfaces)
                {
                    // Для каждой реализации добавляем её в сервисы
                    services.AddTransient(iface, implementationType);
                    logger?.LogInformation("Зарегистрировано расширение для сериализации {Name}", implementationType.Name);
                }
            }
        }

        return services;
    }
    
    // Вспомогательная функция для проверки типов на наличие определенного универсального интерфейса
    private static bool _isAssignableFrom_ITemplateCustomization(Type givenType)
    {
        return givenType.GetInterfaces().Any(interfaceType =>
        {
            return interfaceType.IsConstructedGenericType &&
                   interfaceType.GetGenericTypeDefinition() == typeof(ITemplateCustomization<>);
        });
    }        
}
