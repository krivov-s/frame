using Microsoft.Extensions.DependencyInjection;
using Frame.Domain.EntityValidators;
using Frame.Shared;
using Microsoft.Extensions.Logging;

namespace Frame.Domain
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Здесь будем добавлять все неободимые зависимости
        /// <para>
        /// Необходимо, чтобы любые другие сборки уровня Domain в названии содержали `.Domain`.
        /// Только в сборках, содержащих в имени `.Domain` будет осуществляться поиск валидаторов, событий EventBus
        /// </para>
        /// </summary>
        /// <param name="services"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IServiceCollection AddFrameDomainServices(this IServiceCollection services, ILogger? logger = null)
        {
            logger?.LogInformation("Конфигурирование FrameDomainServices...");
            // Здесь убедимся, что загружены все сборки уровня Domain
            ServiceTools.LoadAssembliesToApplication("*.Domain.dll");
            
            // Список сборок, в имени которых содержится .Domain
            var assemblies = ServiceTools.GetAssemblies(".Domain");
            
            // Добавляем в конвейер все реализации интерфейса IValidator<TEntity>
            services.AddBaseEntityValidationFactory(assemblies);
            
            // По идее данный вызов не нужен, потому что все специфичные валидаторы также добавляются выше
            // services.AddValidatorsFromAssemblies(assemblies);
            logger?.LogInformation("Конфигурирование FrameDomainServices завершено");

            return services;
        }
    }
}
