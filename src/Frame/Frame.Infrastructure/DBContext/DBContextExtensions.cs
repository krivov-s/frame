using Frame.App.Security;
using Frame.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Frame.Infrastructure.DBContext
{
    public static class DBContextExtensions
    {
        /// <summary>
        /// Метод проверяет статус объекта в контексте. Если объект не отслеживается - вызывается context.Add, если отслеживается - то ничего не вызывается.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entity">Объект, который нужно проверить.</param>
        public static void CheckAddToContext<T>(this DbContext context, T entity) where T : BaseEntity
        {
            var entry = context.Entry(entity);

            switch (entry.State)
            {
                case EntityState.Detached:
                    T? existingEntity = context.Set<T>().Local.Where(e => e.Id == entity.Id).FirstOrDefault();
                    if(existingEntity == null)
                    {
                        context.Update(entity);
                    }
                    else
                    {
                        // А вот тут уже нужно скопировать все атрибуты нового объекта в существующий объект
                        context.Entry(existingEntity).CurrentValues.SetValues(entity);
                    }
                    break;
                case EntityState.Unchanged:
                case EntityState.Modified:
                    // Объект уже отслеживается контекстом
                    break;
                case EntityState.Added:
                case EntityState.Deleted:
                    // Обработка по необходимости
                    break;
            }
        }
        
        public static List<ChangedPropValue> GetChangedPropValues(this EntityEntry entry)
        {
            List<ChangedPropValue> values = entry.Properties.Where(p =>
                    p is { IsTemporary: false, IsModified: true } && p.CurrentValue != p.OriginalValue)
                .Select(p => new ChangedPropValue()
                {
                    AttrName = p.Metadata.Name, 
                    OldValue = p.OriginalValue, 
                    NewValue = p.CurrentValue
                }).ToList();
            return values;
        }

        
    }
}
