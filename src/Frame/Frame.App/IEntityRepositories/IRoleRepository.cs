using Frame.Domain.Entities.Core.Security;
using Frame.Shared;

namespace Frame.App.IEntityRepositories
{
    public interface IRoleRepository
    {
        // Отличие этого метода от базового IBaseRepository:GetByIdAsync в том, что базовый зачитывает
        // только линейные атрибуты объекта, а данный метод - со всеми связанными объектами и списками
        public Task<Result<Role>> GetRoleAsync(int id);
    }
}
