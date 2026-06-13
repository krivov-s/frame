using Frame.Domain.Entities.Core.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.Scripting
{
    public class ScriptCommandConfiguration : BaseGenericEntityConfiguration<ScriptCommand>, IEntityTypeConfiguration<ScriptCommand>
    {
        public new void Configure(EntityTypeBuilder<ScriptCommand> builder)
        {
            base.Configure(builder);

            // Прописываем уникальный индекс для наименования ScriptCommand
            builder.HasIndex(sc => sc.ScriptName).IsUnique();

            builder.Property(sc => sc.CommandType)
                .HasConversion<int>();
            builder.Property(sc => sc.CodeType)
                   .HasConversion<int>();
            builder.Property(sc => sc.ThreadType)
                .HasConversion<int>();
        }
    }
}
