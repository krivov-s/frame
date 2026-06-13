using System.Linq.Expressions;

namespace Frame.Domain.Entities.Core
{
    public class ListField<TEntity, TListItem> : Field<TEntity> where TEntity : BaseEntity 
    {
        /// <summary>
        /// Тип ассоциированного объекта. Используется при динамическом создании RefEdit
        /// </summary>
        public override Type ListItemType => typeof(TListItem);

        #region Переопределение делегатов специально для ListField, изначально объявленных в Field
        
        public override LambdaExpression? ListGet => ListGetter;
        public override Delegate? ListSet => ListSetter;

        #endregion

        #region Типизированные делегаты Configure, Getters, Setters. Должны быть определены разработчиком при использовании класса.
        public Expression<Func<TEntity, List<TListItem?>>>? ListGetter { get; init; } = null;
        public Action<TEntity, List<TListItem?>?>? ListSetter { get; init; } = null;
        #endregion

        #region Методы чтения и установки нового значения TRefEntuty в главный объект TEntity
        public void SetValueToEntity(TEntity entity, List<TListItem?>? value)
        {
            if (entity != null && ListSetter != null)
            {
                ListSetter(entity, value);
            }
        }

        public object? GetValueFromEntity(TEntity entity, List<TListItem?>? defaultValue = default)
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
