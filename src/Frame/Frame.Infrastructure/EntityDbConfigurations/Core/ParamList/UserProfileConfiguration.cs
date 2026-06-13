using Frame.Domain.Entities.Core.Params;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.ParamList
{
    public class UserProfileConfiguration : BaseGenericEntityConfiguration<UserProfile>, IEntityTypeConfiguration<UserProfile>
    {
        public new void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            base.Configure(builder);

            builder.HasIndex(pl => pl.UserId).IsUnique();

            builder
                .HasOne(p => p.User)
                .WithOne(u => u.UserProfile)
                .HasForeignKey<UserProfile>(p => p.UserId)
                .IsRequired() // Делаем внешний ключ обязательным
                .OnDelete(DeleteBehavior.Cascade); // Каскадное удаление
        }
    }
}
