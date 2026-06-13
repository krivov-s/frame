using Frame.Domain.Entities.Core;

namespace Frame.Infrastructure.DBContext
{
    public interface IAppDbContext
    {
        public IQueryable<TEntity> GetSet<TEntity>() where TEntity : BaseEntity;
    }
}
