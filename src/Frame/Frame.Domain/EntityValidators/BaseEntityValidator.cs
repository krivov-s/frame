using FluentValidation;
using System.Linq.Expressions;
using FluentValidation.Results;
using Frame.Domain.Entities.Core;

namespace Frame.Domain.EntityValidators
{
    /// <summary>
    /// Минимальный валидатор по-умолчанию для Entities, наследованных от <see cref="BaseEntity"/>.
    /// <para>
    /// Валидатор проверяет наличие значений обязательных свойств,
    /// основываясь на коллекции <see cref="IBaseGenericEntity{TEntity}.GetFields"/>.
    /// </para>
    /// <para>
    /// Для строковых полей дополнительно проверяется предельно допустимый размер (если установлен)
    /// </para>
    /// <para>
    /// Для более сложной, дополнительной валидации типов, можно полностью самостоятельно реализовать отдельный валидатор 
    /// или прописать дополнительные правила в классе-наследнике <see cref="BaseEntityValidator{TEntity}"/>. 
    /// Места где допустима донастройка: конструктор, переопределение <see cref="AddDefaultRules(TEntity)"/>, переопределение <see cref="PreValidate(ValidationContext{TEntity}, ValidationResult)"/>
    /// </para>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class BaseEntityValidator<TEntity> : AbstractValidator<TEntity> where TEntity : BaseEntity, IBaseGenericEntity<TEntity>
    {
        private bool _initialized;
        /// <summary>
        /// Формирование проверок по-умолчанию для типа TEntity.
        /// <para>Этот метод один раз всегда вызывается из <see cref="PreValidate(ValidationContext{TEntity}, ValidationResult)"></see> (если сам PreValidate не был переопределен). 
        /// Если вызов метода нужно полностью отменить или переделать - нужно или переопределить сам метод, или переопределить <see cref="PreValidate(ValidationContext{TEntity}, ValidationResult)"></see>.
        /// </para>
        /// </summary>
        /// <param name="entity">Экземпляр TEntity нужен только для того, чтобы получить список Fields из метаданных класса.</param>
        protected virtual void AddDefaultRules(TEntity entity)
        {
            List<Field<TEntity>> fields = entity.GetFields();

            foreach (Field<TEntity> field in fields)
            {
                if (field.IntGet != null)
                {
                    if (field.Required)
                    {
                        RuleFor(field.IntGet).NotEmpty().WithMessage($"Поле \"{field.HumanName}\" должно быть обязательно заполнено.");
                    }
                }
                else if (field.StringGet != null)
                {
                    if (field.Required)
                    {
                        RuleFor(field.StringGet).NotEmpty().WithMessage($"Поле \"{field.HumanName}\" должно быть обязательно заполнено.");
                    }
                    if (field.MaxLength > 0)
                    {
                        RuleFor(field.StringGet).MaximumLength(field.MaxLength).WithMessage("Превышена максимальная длина поля {PropertyName}.");
                    }

                    if (field.StringValues.Count > 0)
                    {
                        if (field.Required)
                        {
                            // Для обязательного поля допустимо только одно из значений
                            RuleFor(field.StringGet).Must(v => field.StringValues.Contains(v))
                                .WithMessage($"Значение не совпадает с перечнем доступных значений для поля {field.HumanName}.");
                        }
                        else
                        {
                            // Для необязательного поля допустимы null или ""
                            RuleFor(field.StringGet).Must(v => string.IsNullOrEmpty(v) || field.StringValues.Contains(v))
                                .WithMessage($"Значение не совпадает с перечнем доступных значений для поля {field.HumanName}.");
                        }
                    }
                }
                else if (field.DateTimeGet != null)
                {
                    if (field.Required)
                    {
                        RuleFor(field.DateTimeGet).NotEmpty().WithMessage($"Поле \"{field.HumanName}\" должно быть обязательно заполнено.");
                    }
                }
                else if (field.BoolGet != null)
                {
                    if (field.Required)
                    {
                        RuleFor(field.BoolGet).NotEmpty().WithMessage($"Поле \"{field.HumanName}\" должно быть обязательно заполнено.");
                    }
                }
                else if (field.DecimalGet != null)
                {
                    if (field.Required)
                    {
                        RuleFor(field.DecimalGet).NotEmpty().WithMessage($"Поле \"{field.HumanName}\" должно быть обязательно заполнено.");
                    }
                }
                //else if (field.RefGet != null && field.RefIdGet != null)
                //{
                //    var convertedExpression = ConvertExpression(field.RefGet);
                //    //RuleFor(entity => convertedExpression).NotEmpty().WithMessage($"Поле \"{field.HumanName}\" должно быть обязательно заполнено.");
                //    //RuleFor(entity => field.RefIdGet).NotEmpty().WithMessage($"Поле \"{field.HumanName}\" должно быть обязательно заполнено.");
                //    When(entity => field.RefIdGet == null, () =>
                //    {
                //        RuleFor(entity => convertedExpression).NotEmpty().WithMessage($"Поле \"{field.HumanName}\" должно быть обязательно заполнено.");
                //    });
                //    When(entity => convertedExpression == null, () =>
                //    {
                //        RuleFor(entity => field.RefIdGet).NotEmpty().WithMessage($"Поле \"{field.HumanName}\" должно быть обязательно заполнено.");
                //    });
                //}
                else if (field is { RefIdGet: not null, Required: true })
                {
                    if (field.RefGet != null)
                    {
                        var refGetExpr = ConvertExpression(field.RefGet);
                        var refGetFunc = refGetExpr.Compile();
                        RuleFor(field.RefIdGet)
                            .GreaterThan(0)
                            .When(x => refGetFunc(x) == null)
                            .WithMessage($"Поле \"{field.HumanName}\" (Id) должно быть обязательно заполнено (объект не установлен).");
                    }
                    else
                    {
                        RuleFor(field.RefIdGet).Must(id => id is > 0).WithMessage($"Поле \"{field.HumanName}\" (Id) должно быть обязательно заполнено.");
                    }
                    // RuleFor(field.RefIdGet).Must(id => id is > 0).WithMessage($"Поле \"{field.HumanName}\" (Id) должно быть обязательно заполнено.");
                }
                else if (field is { RefGet: not null, RefIdGet: null, Required: true })
                {
                    // Данный набор условий говорит о том, что проверка на наличие объекта выполняется только в случае, если
                    // RefIdGet не установлен, т.е. при заполненном RefIdGet отсутствие объекта не будет считаться ошибкой
                    var convertedExpression = ConvertExpression(field.RefGet);
                    RuleFor(convertedExpression).NotEmpty().WithMessage($"Поле \"{field.HumanName}\" (объект) должно быть обязательно заполнено.");
                }
            }
        }

        protected static Expression<Func<TEntity, object?>> ConvertExpression(LambdaExpression expression)
        {
            var parameter = Expression.Parameter(typeof(TEntity));
            var convertedBody = Expression.Convert(Expression.Invoke(expression, parameter), typeof(object));
            return Expression.Lambda<Func<TEntity, object?>>(convertedBody, parameter);
        }

        protected override bool PreValidate(ValidationContext<TEntity> context, ValidationResult result)
        {
            if (!_initialized)
            {
                if (context.InstanceToValidate == null)
                {
                    result.Errors.Add(new ValidationFailure("", "Экземпляр объекта для валидации не передан"));
                    return false;
                }
                AddDefaultRules(context.InstanceToValidate);
                _initialized = true;
            }
            return true;
        }

        public Func<TEntity, IEnumerable<string>> Validation => ValidateEntity;
        protected IEnumerable<string> ValidateEntity(TEntity arg)
        {
            var result = Validate(arg);
            if (result.IsValid)
                return [];
            return result.Errors.Select(e => e.ErrorMessage);
        }


        /// <summary>
        /// Этот метод был сделан для работы валидации на уровней полей формы MudBlazor
        /// В универсальной SimpleEditForm не удалось заставить это работать по-нормальному: контролам требуется наличие свойства "For",
        /// однако конструкция For="@(() => field.GetValueFromEntity(Entity, ""))" не прокатила, выдала ошибку в Runtime.
        /// Пока забил на это, валидация делается как и раньше - при добавлении в IBaseRepository.
        /// </summary>
        public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
        {
            var result = await ValidateAsync(ValidationContext<TEntity>.CreateWithOptions((TEntity)model, x => x.IncludeProperties(propertyName)));
            if (result.IsValid) return [];
            return result.Errors.Select(e => e.ErrorMessage);
        };
    }
}

