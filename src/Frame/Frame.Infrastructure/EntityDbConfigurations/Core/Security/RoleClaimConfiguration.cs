using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.Security
{
    public class RoleClaimConfiguration : BaseGenericEntityConfiguration<RoleClaim>, IEntityTypeConfiguration<RoleClaim>
    {
        public new void Configure(EntityTypeBuilder<RoleClaim> builder)
        {
            base.Configure(builder);

            // У роли не может быть двух Claim одинакового типа.
            builder.HasIndex(rc => new { rc.RoleId, rc.ClaimType }).IsUnique();
            
            // Каскадное удаление записей RoleClaim при удалении владельца - Role 
            builder
                .HasOne(rc => rc.Role)
                .WithMany(r => r.RoleClaims)
                .HasForeignKey(rc => rc.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            
        }
    }
}
