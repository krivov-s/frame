using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Frame.App.Cores;
using Frame.Shared;
using Frame.Domain.Entities.Core;

namespace Frame.Infrastructure.DBContext
{
    public partial class AppDbContext : DbContext, IAppDbContext
    {
        //protected IGetCurrentUserNameService _currentUserService; //{ get; set; }
        //protected string _currentUserLogin = "";
        private readonly IUserCore _userCore;

        /// <summary>
        /// Метод заблокирован: вместо него необходимо использовать <see cref="GetSet{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override DbSet<T> Set<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.Interfaces)] T>()
        {
            throw new NotImplementedException("Использование данного метода заблокировано. Используйте GetSet<TEntity>");
        }

        /// <summary>
        /// Получает и возвращает IQuerable<typeparamref name="TEntity"/> с предварительно прописанными фильтрами системы безопасности для активного пользователя.
        /// Если параметры безопасности для данного типа для данного пользователя отсутствуют - dbSet возвращается без ограничений.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public IQueryable<TEntity> GetSet<TEntity>() where TEntity : BaseEntity
        {
            // Для текущего пользователя нужно получить фильтры беопасности для dbset-а и добавить их. 
            // Фильтры могут быть в виде строк, они будут обработаны с помощью Linq.Dynamic.Cores
            IQueryable<TEntity> query = base.Set<TEntity>();
            Result<IQueryable<TEntity>> result = _userCore.ApplyReadQueryFilter(query);
            result.CheckAndThrow($"GetSet<{typeof(TEntity).Name}>");
            return result.Value!;
        }

        public IQueryable? GetSet(Type entityType)
        {
	        // Используем метод Set<Type>() через рефлексию, чтобы получить IQueryable для заданного типа
	        MethodInfo? method = GetType()
		        .GetMethods()
		        .FirstOrDefault(m => m.Name == nameof(GetSet) 
		                             && m.IsGenericMethod
		                             && m.GetParameters().Length == 0);
		        
	        if (method != null)
	        {
		        var methodGeneric = method.MakeGenericMethod(entityType);
		        return (IQueryable?)methodGeneric.Invoke(this, null);
	        }

	        return null;
        }
        
        /// <summary>
        /// Проверка прав доступа на создание/редактирование/удаление объектов из списка изменений. Если хотя бы одного разрешения нет - выбрасывается FrameSecurityException.
        /// </summary>
        /// <exception cref="FrameSecurityException"></exception>
        protected void CheckAddModifyDeleteRights()
        {
			ChangeTracker.DetectChanges();

            IEnumerable<EntityEntry> entitiesToTrack = base.ChangeTracker.Entries().Where(
																e => e.Entity is not Domain.Entities.Core.Security.AuditRecord
																&& e.State != EntityState.Detached
																&& e.State != EntityState.Unchanged
																&& e.Entity is BaseEntity);

            Result<bool> proceedOperation = Result<bool>.Success(false);
            foreach (EntityEntry entityEntry in entitiesToTrack)
            {
                BaseEntity o = (BaseEntity)entityEntry.Entity;
                if(o != null)
                {
					switch (entityEntry.State)
					{
						case EntityState.Added:
                            proceedOperation = _userCore.CanAdd(o);
							break;
						case EntityState.Modified:
                            proceedOperation = _userCore.CanModify(o, entityEntry.GetChangedPropValues());
                            if (proceedOperation is { IsErrorOrNull: false, Value: true })
                            {
	                            // Фиксируем дату/время изменения объекта
	                            o.DateModify = DateTime.UtcNow;
                            }
                            break;
						case EntityState.Deleted:
                            if (entityEntry.Properties.Any(p => p.IsModified))
                            {
                                throw new FrameSecurityException($"Операция {entityEntry.State} запрещена для объекта с измененными атрибутами {o.GetType().Name} ({o.Description})");
                            }
                            proceedOperation = _userCore.CanDelete(o);
                            break;
					}
                    if (proceedOperation.IsErrorOrNull || !proceedOperation.Value)
                    {
	                    string err = $"Операция {entityEntry.State} не разрешена для {o.GetType().Name} ({o.Description})";
	                    if (proceedOperation.ErrorResult.Length > 0)
	                    {
		                    err += $": {proceedOperation.ErrorResult}";
	                    }
	                    throw new FrameSecurityException(err);
                    }
                }
			}
		}
    }
}
