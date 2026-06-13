using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.Security
{
    public class UserConfiguration : BaseGenericEntityConfiguration<User>, IEntityTypeConfiguration<User>
    {
        public new void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);

            // Уникальный индекс по логину пользователя
            builder.HasIndex(rc => rc.Login).IsUnique();
        }
    }
}
