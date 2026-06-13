using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.Security
{
    public class TEntityRightsConfiguration : BaseGenericEntityConfiguration<TEntityRights>, IEntityTypeConfiguration<TEntityRights>
    {
        public new void Configure(EntityTypeBuilder<TEntityRights> builder)
        {
            base.Configure(builder);

            builder.HasIndex(er => new { er.RoleId, er.EntityTypeName }).IsUnique();
            
            // Каскадное удаление записей TEntityRights при удалении владельца - Role 
            builder
                .HasOne(er => er.Role)
                .WithMany(r => r.EntityRights)
                .HasForeignKey(er => er.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
