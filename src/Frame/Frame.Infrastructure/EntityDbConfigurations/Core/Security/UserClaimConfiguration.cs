using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.Security
{
    public class UserClaimConfiguration : BaseGenericEntityConfiguration<UserClaim>, IEntityTypeConfiguration<UserClaim>
    {
        public new void Configure(EntityTypeBuilder<UserClaim> builder)
        {
            base.Configure(builder);

            // У пользователя не может быть двух Claim одинакового типа.
            builder.HasIndex(rc => new { rc.UserId, rc.ClaimType }).IsUnique();
            
            // Каскадное удаление записей UserClaim при удалении владельца - User 
            builder
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserClaims)
                .HasForeignKey(rc => rc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
        }
    }
}
