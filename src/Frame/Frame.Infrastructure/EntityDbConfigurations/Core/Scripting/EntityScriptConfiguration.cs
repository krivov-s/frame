using Frame.Domain.Entities.Core.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.Scripting
{
    public class EntityScriptConfiguration : BaseGenericEntityConfiguration<EntityScript>, IEntityTypeConfiguration<EntityScript>
    {
        public new void Configure(EntityTypeBuilder<EntityScript> builder)
        {
            base.Configure(builder);

            // Прописываем уникальный индекс для пары (EntityType, HookType)
            builder.HasIndex(sc => new { sc.EntityType, sc.HookType }).IsUnique();

            builder.Property(sc => sc.HookType)
                   .HasConversion<int>();

            builder.Property(sc => sc.CodeType)
                   .HasConversion<int>();
        }
    }
}
