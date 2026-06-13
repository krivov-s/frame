using Frame.Domain.Entities.Core.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.Scripting
{
    public class ScriptCommandTypeConfiguration : BaseGenericEntityConfiguration<ScriptCommandType>, IEntityTypeConfiguration<ScriptCommandType>
    {
        public new void Configure(EntityTypeBuilder<ScriptCommandType> builder)
        {
            base.Configure(builder);

            // Прописываем уникальный индекс для наименования ScriptCommandType
            builder.HasIndex(sc => sc.Name).IsUnique();
        }
    }
}
