using System.Linq.Expressions;
using Frame.Shared;

namespace Frame.Domain.Entities.Core
{
    public enum EntityFieldType
    {
        Int,
        String,
        DateTime,
        DateOnly,
        Bool,
        Decimal,
        List,
        Ref,
        Undefined
    };

    public class SelectListItem
    {
        public int Value { get; set; }
        public string Description { get; set; } = "";
        /// <summary>
        /// Вспомогательный метод, осуществляющий поиск <see cref="SelectListItem"/> в коллекции по значению <see cref="SelectListItem.Value"/>
        /// </summary>
        /// <param name="list">Коллекция List{SelectListItem}</param>
        /// <param name="value">Искомое значение SelectListItem.Value</param>
        /// <returns></returns>
        public static string GetNameByValue(List<SelectListItem> list, int value)
        {
            SelectListItem? item = list.FirstOrDefault(item => item.Value == value);
            return item?.Description ?? "";
        }
    }

    public interface IField
    {
        /// <summary>
        /// Наименование поля в классе
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Тип поля
        /// </summary>
        public EntityFieldType FieldType { get; set; }
        
        /// <summary>
        /// "Человеческое" имя поля. Так оно будет показываться в интерфейсе пользователя.
        /// </summary>
        public string HumanName { get; init; }

        /// <summary>
        /// Тип объекта, к которому относится данное поле
        /// </summary>
        public Type EntityType { get; }
    }
    
    /// <summary>
    /// Описание метаданных для полей моделей (Entities).
    /// Привязка метаданных к конкретной модели осуществляется в ее описании класса путем создания внутреннего статического класса Meta.
    /// Чтобы эти метаданные использовались при созднаии таблиц БД необходимо явно реализовывать <see cref="IEntityTypeConfiguration"/> для каждого типа.
    /// </summary>
    public class Field<TEntity> : IField where TEntity : BaseEntity
    {
        /// <summary>
        /// Признак того, должно поле сохраняться в БД или нет.
        /// <b>Используется только при реализации <see cref="IEntityTypeConfiguration"/></b>
        /// </summary>
        public bool IsPersistent { get; set; } = true;
        
        /// <summary>
        /// Тип объекта, к которому относится данное поле
        /// </summary>
        public Type EntityType => typeof(TEntity);
        
        /// <summary>
        /// Для поля типа RefField (ссылка) - тип объекта, на который ссылаемся
        /// </summary>
        public virtual Type? RefEntityType { get; } = null;
        
        /// <summary>
        /// Для поля типа ListField (ссылка) - тип содержимого списка
        /// </summary>
        public virtual Type? ListItemType { get; } = null;

        /// <summary>
        /// Наименование поля, так как оно названо в объекте TEntity
        /// </summary>
        public required string Name { get; set; } = "";
        
        /// <summary>
        /// Признак обязательности поля
        /// </summary>
        public required bool Required { get; init; }
        
        /// <summary>
        /// Максимальная длина текстового поля
        /// </summary>
        public int MaxLength { get; init; }

        /// <summary>
        /// Формат, который будет использован по-умолчанию при отображении значения пользователю
        /// </summary>
        public string Format { get; set; } = "";
        
        /// <summary>
        /// Список возможных или допустимых значений текстового поля. Будет отображаться в виде списка в интерфейсе пользователя. 
        /// Проверки должны выполняться в валидаторах (при необходимости)
        /// </summary>
        public List<string> StringValues { get; init; } = [];
        
        /// <summary>
        /// Список возможных или допустимых значений текстового поля. Будет отображаться в виде списка в интерфейсе пользователя. 
        /// Проверки должны выполняться в валидаторах (при необходимости)
        /// </summary>
        public List<SelectListItem> IntValues { get; init; } = [];
        
        /// <summary>
        /// "Человеческое" имя поля. Так оно будет показываться в интерфейсе пользователя.
        /// </summary>
        public required string HumanName { get; init; } = "";

        /// <summary>
        /// Дополнительное описание, смысловая подсказка
        /// </summary>
        public string HelperText { get; set; } = "";
        
        private EntityFieldType _fieldType = EntityFieldType.Undefined;
        public EntityFieldType FieldType
        {
            get
            {
                if (_fieldType == EntityFieldType.Undefined)
                {
                    if (StringGet != null)
                    {
                        _fieldType = EntityFieldType.String;
                    }
                    else if (IntGet != null)
                    {
                        _fieldType = EntityFieldType.Int;
                    }
                    else if (DateTimeGet != null)
                    {
                        _fieldType = EntityFieldType.DateTime;
                    }
                    else if (DateOnlyGet != null)
                    {
                        _fieldType = EntityFieldType.DateOnly;
                    }
                    else if (BoolGet != null)
                    {
                        _fieldType = EntityFieldType.Bool;
                    }
                    else if (DecimalGet != null)
                    {
                        _fieldType = EntityFieldType.Decimal;
                    }
                    else if (ListGet != null)
                    {
                        _fieldType = EntityFieldType.List;
                    }
                    else if (RefGet != null || RefIdGet != null)
                    {
                        _fieldType = EntityFieldType.Ref;
                    }
                }
                return _fieldType;
            }
            set => throw new FrameException("Это метод-заглушка, он не должен вызываться вообще!");
        }

        public virtual void CopyValue(TEntity source, TEntity target)
        {
            switch (FieldType)
            {
                case EntityFieldType.Int:
                    SetValueToEntity(target, GetValueFromEntity(source, int.MinValue));
                    break;
                case EntityFieldType.String:
                    SetValueToEntity(target, GetValueFromEntity(source,""));
                    break;
                case EntityFieldType.DateTime:
                    SetValueToEntity(target, GetValueFromEntity(source, DateTime.MinValue));
                    break;
                case EntityFieldType.DateOnly:
                    SetValueToEntity(target, GetValueFromEntity(source, DateOnly.FromDateTime(DateTime.MinValue)));
                    break;
                case EntityFieldType.Bool:
                    SetValueToEntity(target, GetValueFromEntity(source, false));
                    break;
                case EntityFieldType.Decimal:
                    SetValueToEntity(target, GetValueFromEntity(source, decimal.Zero));
                    break;
            }
        }
        
        #region General getters - получение значений из экземпляров TEntity
        public string? GetValueFromEntity(TEntity entity, string? defaultValue = "")
        {
            if (entity == null || StringGet == null) return defaultValue;
            
            var func = StringGet.Compile();
            return func(entity) ?? defaultValue;
        }
        public int? GetValueFromEntity(TEntity entity, int? defaultValue = 0)
        {
            if (entity == null || IntGet == null) return defaultValue;
            
            var func = IntGet.Compile();
            return func(entity) ?? defaultValue;
        }

        public DateTime? GetValueFromEntity(TEntity entity, DateTime? defaultValue = null)
        {
            if (entity == null || DateTimeGet == null) return defaultValue;
            
            var func = DateTimeGet.Compile();
            return func(entity) ?? defaultValue;
        }

        public DateOnly? GetValueFromEntity(TEntity entity, DateOnly? defaultValue = null)
        {
            if (entity == null || DateOnlyGet == null) return defaultValue;
            
            var func = DateOnlyGet.Compile();
            return func(entity) ?? defaultValue;
        }
        public bool? GetValueFromEntity(TEntity entity, bool? defaultValue = null)
        {
            if (entity == null || BoolGet == null) return defaultValue;
            
            var func = BoolGet.Compile();
            return func(entity) ?? defaultValue;
        }

        public decimal? GetValueFromEntity(TEntity entity, decimal? defaultValue = 0)
        {
            if (entity == null || DecimalGet == null) return defaultValue;
            
            var func = DecimalGet.Compile();
            return func(entity) ?? defaultValue;
        }


        public DateTime? GetDateOnlyValueFromEntityAsDateTime(TEntity entity, DateTime? defaultValue = null)
        {
            if (entity == null || DateOnlyGet == null) return defaultValue;
            
            var func = DateOnlyGet.Compile();
            DateOnly? dateOnly = func(entity);
            return dateOnly?.ToDateTime(TimeOnly.MinValue) ?? defaultValue;
        }

        public DateOnly? GetDateOnlyValueFromDateTimeEntity(TEntity entity, DateOnly? defaultValue = null)
        {
            if (entity == null || DateTimeGet == null) return defaultValue;
            
            var func = DateTimeGet.Compile();
            DateTime? dateTime = func(entity);
            return dateTime != null ? DateOnly.FromDateTime(dateTime.Value) : defaultValue;
        }

        public TimeOnly? GetTimeOnlyValueFromDateTimeEntity(TEntity entity, TimeOnly? defaultValue = null)
        {
            if (entity == null || DateTimeGet == null) return defaultValue;
            
            var func = DateTimeGet.Compile();
            DateTime? dateTime = func(entity);
            if (dateTime != null)
            {
                return TimeOnly.FromDateTime(dateTime.Value);
            }
            
            return defaultValue;
        }

        #endregion

        #region General setters - установка (запись) значений в экземпляры TEntity
        public void SetValueToEntity(TEntity entity, string? value)
        {
            if (entity != null && StringSet != null)
            {
                StringSet(entity, value);
            }
        }
        public void SetValueToEntity(TEntity entity, int? value)
        {
            if (entity != null && IntSet != null)
            {
                IntSet(entity, value);
            }
        }
        public void SetValueToEntity(TEntity entity, DateTime? value)
        {
            if (entity != null && DateTimeSet != null)
            {
                DateTimeSet(entity, value);
            }
        }

        public void SetValueToEntity(TEntity entity, DateOnly? value)
        {
            if (entity != null && DateOnlySet != null)
            {
                DateOnlySet(entity, value);
            }
        }

        public void SetValueToEntity(TEntity entity, bool? value)
        {
            if (entity != null && BoolSet != null)
            {
                BoolSet(entity, value);
            }
        }

        public void SetValueToEntity(TEntity entity, decimal? value)
        {
            if (entity != null && DecimalSet != null)
            {
                DecimalSet(entity, value);
            }
        }
        
        public void SetDateTimeAsDateOnlyValueToEntity(TEntity entity, DateTime? value)
        {
            DateOnly? dateOnly = null;
            if (value != null)
            {
                dateOnly = DateOnly.FromDateTime(value.Value);
            }
            SetValueToEntity(entity, dateOnly);
        }

        public void SetDatePartOfDateTimeValueToEntity(TEntity entity, DateTime? newDate)
        {
            DateTime? dateTime = GetValueFromEntity(entity, newDate);
            
            if (newDate != null)
            {
                dateTime = dateTime != null ? newDate.Value.Date + dateTime.Value.TimeOfDay : newDate.Value.Date;
            }
            else if (dateTime != null)
            {
                dateTime = null;
            }

            SetValueToEntity(entity, dateTime);
        }

        public void SetTimePartOfDateTimeValueToEntity(TEntity entity, TimeSpan? newTime)
        {
            DateTime? dateTime = GetValueFromEntity(entity, DateTime.MinValue);
            if (newTime.HasValue)
            {
                dateTime = dateTime.HasValue ? dateTime.Value.Date + newTime.Value : DateTime.UtcNow + newTime.Value;
            }
            else if (dateTime.HasValue)
            {
                dateTime = dateTime.Value.Date;
            }
            SetValueToEntity(entity, dateTime);
        }

        #endregion

        // Это по сути Getter-ы для получения значений полей, используются в FluentValidation, в конфигурации EntityTypeBuilder
        // Они должны быть в виде Expression - это требование FluentValidation.
        #region Functor getters
        public Expression<Func<TEntity, int?>>? IntGet { get; init; }
        public Expression<Func<TEntity, string?>>? StringGet { get; init; }
        public Expression<Func<TEntity, DateTime?>>? DateTimeGet { get; init; }
        public Expression<Func<TEntity, DateOnly?>>? DateOnlyGet { get; init; }
        public Expression<Func<TEntity, bool?>>? BoolGet { get; init; }
        public Expression<Func<TEntity, decimal?>>? DecimalGet { get; init; }
        #endregion

        #region Functor setters
        public Action<TEntity, int?>? IntSet { get; init; } = null;
        public Action<TEntity, string?>? StringSet { get; init; } = null;
        public Action<TEntity, DateTime?>? DateTimeSet { get; init; } = null;
        public Action<TEntity, DateOnly?>? DateOnlySet { get; init; } = null;
        public Action<TEntity, bool?>? BoolSet { get; init; } = null;
        public Action<TEntity, decimal?>? DecimalSet { get; init; } = null;
        #endregion

        // Делегаты, которые должны переопределяться в дочернем классе для RefField
        #region Genertal delegates for RefField
        public virtual LambdaExpression? RefGet { get; }
        public virtual Delegate? RefSet { get; }

        public virtual Expression<Func<TEntity, int?>>? RefIdGet { get; }
        public virtual Action<TEntity, int?>? RefIdSet { get; }

        public virtual Delegate? RefConfigureQuerySpecification { get; set; }

        #endregion
        
        // Делегаты, которые должны переопределяться в дочернем классе для ListField
        #region Genertal delegates for ListField
        
        public virtual LambdaExpression? ListGet { get; }
        public virtual Delegate? ListSet { get; }
        
        #endregion
        
        public string GetDefaultFormat()
        {
            if (!string.IsNullOrEmpty(Format))
            {
                return Format;
            }

            return FieldType switch
            {
                EntityFieldType.Int => ServiceTools.GetDefaultFormat<int>(),  
                EntityFieldType.Bool => ServiceTools.GetDefaultFormat<bool>(),
                EntityFieldType.Decimal => ServiceTools.GetDefaultFormat<decimal>(),
                EntityFieldType.DateTime => ServiceTools.GetDefaultFormat<DateTime>(),
                EntityFieldType.DateOnly => ServiceTools.GetDefaultFormat<DateOnly>(),
                _ => ""
            };
        }
    }
}
