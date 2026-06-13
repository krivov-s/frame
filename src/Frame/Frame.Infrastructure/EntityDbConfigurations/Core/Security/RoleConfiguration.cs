using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.Security
{
    public class RoleConfiguration : BaseGenericEntityConfiguration<Role>, IEntityTypeConfiguration<Role>
    {
        public new void Configure(EntityTypeBuilder<Role> builder)
        {
            base.Configure(builder);
            
            // Уникальный индекс по имени роли
            builder.HasIndex(rc => rc.Name).IsUnique();
        }
    }
}
