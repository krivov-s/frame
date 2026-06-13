using System.Linq.Expressions;
using Frame.Domain.QuerySpec;

namespace Frame.Domain.Entities.Core
{
    /// <summary>
    /// Каким образом осуществляется клонирование данной ссылки
    /// <b>WARNING!!! В настоящее время клонирование не реализовано!!!</b>
    /// </summary>
    public enum RefFieldCloneBehavour
    {
        /// <summary>
        /// При клонировании новому объекту устанавливается Null (и Id = 0)
        /// </summary>
        SetNull,
        /// <summary>
        /// При клонировании данные просто копируются (и объект, и Id)
        /// </summary>
        LeaveAsIs,
        /// <summary>
        /// При клонировании создается новая копия объекта, у нее обнуляется Id
        /// </summary>
        CreateCopy
    };
    
    public class RefField<TEntity, TRefEntity> : Field<TEntity> where TEntity : BaseEntity 
                                                                where TRefEntity : BaseEntity
    {
        /// <summary>
        /// Тип ассоциированного объекта. Используется при динамическом создании RefEdit
        /// </summary>
        public override Type RefEntityType => typeof(TRefEntity);

        #region Переопределение делегатов специально для RefField, изначально объявленных в Field
        public override LambdaExpression? RefGet => RefGetter;
        public override Delegate? RefSet => RefSetter;

        public override Expression<Func<TEntity, int?>>? RefIdGet => RefIdGetter;
        public override Action<TEntity, int?>? RefIdSet => RefIdSetter;

        public override Delegate? RefConfigureQuerySpecification => ConfigureQuerySpecification;

        #endregion

        public override void CopyValue(TEntity source, TEntity target)
        {
            object? val = GetValueFromEntity(source);
            
            switch (CloneBehavour)
            {
                case RefFieldCloneBehavour.SetNull:
                    SetValueToEntity(target, null);
                    if (RefIdSetter != null)
                    {
                        RefIdSetter(target, null);
                    }
                    break;
                case RefFieldCloneBehavour.LeaveAsIs:
                    if (val != null)
                    {
                        BaseEntity? valEntity = val as BaseEntity;
                        if (valEntity != null)
                        {
                            SetValueToEntity(target, valEntity as TRefEntity);
                            if (RefIdSetter != null)
                            {
                                RefIdSetter(target, valEntity.Id);
                            }
                        }
                    }
                    break;
                case RefFieldCloneBehavour.CreateCopy:
                    if (val != null)
                    {
                        throw new NotImplementedException("Возможность создания копии RefField пока не реализована");
                    }
                    break;
            }
        }

        /// <summary>
        /// Алгоритм копирования ссылки при клонировании объекта. По-умолчанию - значение остается неизменным,
        /// т.е. вместе с объектом копируется ссылка на тот же самый связанный объект.
        /// </summary>
        public RefFieldCloneBehavour CloneBehavour { get; set; } = RefFieldCloneBehavour.LeaveAsIs;
        
        #region Типизированные делегаты Configure, Getters, Setters. Должны быть определены разработчиком при использовании класса.
        public Expression<Func<TEntity, TRefEntity?>>? RefGetter { get; init; } = null;
        public Action<TEntity, TRefEntity?>? RefSetter { get; init; } = null;

        public Expression<Func<TEntity, int?>>? RefIdGetter { get; init; } = null;
        public Action<TEntity, int?>? RefIdSetter { get; init; } = null;

        public Func<IQuerySpecification<TRefEntity>, IQuerySpecification<TRefEntity>>? ConfigureQuerySpecification { get; set; } = null;
        
        #endregion

        #region Методы чтения и установки нового значения TRefEntuty в главный объект TEntity
        public void SetValueToEntity(TEntity entity, TRefEntity? value)
        {
            if (entity != null && RefSetter != null)
            {
                RefSetter(entity, value);
            }
        }

        public object? GetValueFromEntity(TEntity entity, TRefEntity? defaultValue = null)
        {
            if (entity != null && RefGet != null)
            {
                Delegate func = RefGet.Compile();
                return func.DynamicInvoke(entity) ?? defaultValue;
            }
            return defaultValue;
        }
        #endregion
    }
}
