using Microsoft.AspNetCore.Identity;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Infrastructure.Security.Test
{
    public class AppRoleTestRepository : IRoleStore<Role>
    {
        private static List<Role> _rolesList = [];
        public AppRoleTestRepository()
        {
            _rolesList = [];
            _rolesList.Add(new Role() { Id = 1, Name = "Administrator" });
            _rolesList.Add(new Role() { Id = 2, Name = "User" });
        }

        public Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken)
        {
            _rolesList.Add(new Role
            {
                Id = role.Id,
                Name = role.Name,
            });
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public Task<Role?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_rolesList.FirstOrDefault(u => u.Id == int.Parse(roleId)));
        }

        public Task<Role?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            return Task.FromResult(_rolesList.FirstOrDefault(u => u.NormalizedRoleName == normalizedRoleName));
        }

        public Task<string?> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetRoleIdAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string?> GetRoleNameAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetNormalizedRoleNameAsync(Role role, string? normalizedName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetRoleNameAsync(Role role, string? roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
