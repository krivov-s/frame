
namespace Frame.Domain.Entities.Core
{
    public interface IBaseGenericEntity<TEntity> where TEntity : BaseEntity
    {
        public List<Field<TEntity>> GetFields();
    }
}
