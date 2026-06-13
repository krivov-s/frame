using FluentValidation;
using System.Reflection;
using Frame.Domain.Entities.Metadata;
using Frame.Shared;


namespace Frame.Domain.EntityValidators
{
    public interface IEntityValidatorFactory
    {
        IValidator<TEntity>? GetValidator<TEntity>();
        IValidator? GetValidator(Type entityType);
        IValidator? GetValidator(object entity);
    }

    public class EntityValidatorFactory: IEntityValidatorFactory
    {
        private static readonly Dictionary<Type, Type> _validatorMap = new();
        private static readonly Dictionary<Type, IValidator> _validatorInstances = new();
        private static readonly object _lock = new();
        private static bool _isInitialized;

        public static void Initialize(List<Assembly> assemblies)
        {
            lock (_lock)
            {
                // Если к этому моменту кто-то уже карту инициализировал - выходим.
                if(_isInitialized)
                {
                    return;
                }

                // На всякий случай очищаем карты, вдруг это повторная инициализация, а в прошлой что-то пошло не так где-нибудь в середине...
                _validatorMap.Clear();
                _validatorInstances.Clear();

                var entityTypes = EntityMetadata.GetAllEntitiesClassTypes();

                // Делаем 2 прохода. Сначала все специфичные валидаторы,
                // потом для тех для кого не нашлось специфичных - регистрируем базовый

                // 1 проход
                foreach (var assembly in assemblies)
                {
                    // Получаем все типы валидаторов из текущей сборки
                    var validatorTypes = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && IsValidatorType(t));

                    foreach (var entityType in entityTypes)
                    {
                        // Ищем специфичный валидатор для текущего типа сущности
                        var specificValidator = validatorTypes.FirstOrDefault(v =>
                            IsValidatorForType(v, entityType));

                        if (specificValidator != null && !_validatorMap.TryAdd(entityType, specificValidator))
                        {
                            string err = $"Для типа {entityType} зарегистрирован дублирующий валидатор {specificValidator.Name} в сборке {assembly.FullName}";
                            throw new FrameException(err);
                        }
                    }
                }

                // 2 проход - добиваем стандартным валидатором для тех, кого не нашли
                foreach (var entityType in entityTypes)
                {
                    if (!_validatorMap.ContainsKey(entityType))
                    {
                        try
                        {
                            var baseValidatorType = typeof(BaseEntityValidator<>).MakeGenericType(entityType);
                            _validatorMap[entityType] = baseValidatorType;
                        }
                        catch (Exception)
                        {
                            // Игнорируем ошибку. 
                            // 100% возникает ошибка с типами, которые не наследуют напрямую от BaseGenericEntity<>
                        }
                    }
                }

                // После успешного завершения инициализации - устанавливаем флаг.
                _isInitialized = true;
            }
        }

        private static bool IsValidatorType(Type type)
        {
            return type.GetInterfaces()
                .Any(i => i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValidator<>));
        }

        private static bool IsValidatorForType(Type validatorType, Type entityType)
        {
            return validatorType.GetInterfaces()
                .Any(i => i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValidator<>) &&
                    i.GetGenericArguments()[0] == entityType);
        }

        private static IValidator? CreateValidatorInstance(Type validatorType)
        {
            try
            {
                object? oValidator = Activator.CreateInstance(validatorType);
                if(oValidator == null)
                {
                    return null;
                }
                else
                {
                    return (IValidator)oValidator;
                }
            }
            catch (Exception)
            {
                // Если ошибка - игнорируем ее, просто считаем что такого валидатора нет или есть но он некорректный
                return null;
            }
        }

        private static IValidator? GetOrCreateValidator(Type entityType)
        {
            if (!_validatorMap.TryGetValue(entityType, out var validatorType))
            {
                throw new InvalidOperationException($"Validator not found for type {entityType.Name}");
            }

            lock (_lock)
            {
                if (!_validatorInstances.TryGetValue(entityType, out var validator))
                {
                    validator = CreateValidatorInstance(validatorType);
                    if(validator != null)
                        _validatorInstances[entityType] = validator;
                }
                return validator;
            }
        }

        public IValidator<TEntity>? GetValidator<TEntity>()
        {
            var validator = GetOrCreateValidator(typeof(TEntity));
            return (validator != null) ? (IValidator<TEntity>)validator : null;
        }

        public IValidator? GetValidator(Type entityType)
        {
            return GetOrCreateValidator(entityType);
        }

        public IValidator? GetValidator(object entity)
        {
            return (entity == null) ? null : GetValidator(entity.GetType());
        }

        // Метод для очистки кэша валидаторов (может быть полезен для тестирования)
        public static void ClearCache()
        {
            lock (_lock)
            {
                _validatorInstances.Clear();
            }
        }
    }
}
