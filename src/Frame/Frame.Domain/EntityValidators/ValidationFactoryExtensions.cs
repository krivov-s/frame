using System.Reflection;
using FluentValidation;
using Frame.Domain.Entities.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Domain.EntityValidators
{
    public static class ValidationFactoryExtensions
    {
        /// <summary>
        /// Фабрика валидаторов. При инициализации добавляет в DI все реализации интерфейса <see cref="IValidator{T}"/> 
        /// из всех загруженных несистемных сборок.
        /// <para>Для типов, наследованных от <see cref="BaseEntity"/>, для которых нет специфичных реализаций <see cref="IValidator{T}"/>,
        /// создает и регистрирует стандартный валидатор на основе <see cref="BaseEntityValidator{TEntity}"/></para>
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblies">Список сборок, в которых осуществлять поиск типов для регистрации</param>
        /// <returns></returns>
        public static IServiceCollection AddBaseEntityValidationFactory(this IServiceCollection services, List<Assembly> assemblies)
        {
            // var assemblies = EntityMetadata.GetEntityAssemblies();
            EntityValidatorFactory.Initialize(assemblies);

            services.AddSingleton<IEntityValidatorFactory, EntityValidatorFactory>();

            return services;
        }
        //public static IServiceCollection AddBaseEntityValidators(this IServiceCollection services)
        //{
        //    // 1. Добавляем в DI все реализации интерфейса IValidator<TEntity> из всех загруженных несистемных сборок
        //    var assemblies = EntityMetadata.GetEntityAssemblies();
        //    foreach (var assembly in assemblies)
        //    {
        //        services.AddValidatorsFromAssembly(assembly);
        //    }

        //    // 2. Теперь для каждого типа, наследованного от BaseEntity (кроме BaseGenericEntity), проверяем наличие в коллекции валидатора
        //    // Если есть - пропускаем, если нет - создаем базовый валидатор
        //    foreach (Type tEntity in EntityMetadata.GetAllEntitiesClassTypes())
        //    {
        //        Type tValidatorInterface = typeof(IValidator<>).MakeGenericType(tEntity);
        //        bool isExists = services.Any(d => d.ServiceType == tValidatorInterface);

        //        if (isExists == false)
        //        {
        //            try
        //            {
        //                Type tValidatorObject = typeof(BaseEntityValidator<>).MakeGenericType(tEntity);
        //                services.AddScoped(tValidatorInterface, tValidatorObject);
        //            }
        //            catch(Exception)
        //            {
        //                // Если происходит какой-либо Exception - игнорируем, просто не будет валидатора для данного типа.
        //            }
        //        }
        //    }
        //    EntityValidatorProvider provider = new();
        //    services.AddSingleton<>();

        //    return services;
        //}
    }
}
