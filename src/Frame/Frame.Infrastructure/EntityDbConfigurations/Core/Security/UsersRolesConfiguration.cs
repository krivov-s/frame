using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.Security
{
    public class UsersRolesConfiguration : BaseGenericEntityConfiguration<UsersRoles>, IEntityTypeConfiguration<UsersRoles>
    {
        public new void Configure(EntityTypeBuilder<UsersRoles> builder)
        {
            base.Configure(builder);

            builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
        }
    }
}
