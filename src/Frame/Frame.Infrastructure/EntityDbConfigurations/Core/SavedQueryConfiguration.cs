using Frame.Domain.Entities.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Frame.Infrastructure.EntityDbConfigurations.Core
{
    public class SavedQueryConfiguration : BaseGenericEntityConfiguration<SavedQuery>, IEntityTypeConfiguration<SavedQuery>
    {
        public new void Configure(EntityTypeBuilder<SavedQuery> builder)
        {
            base.Configure(builder);
            
            // Прописываем уникальный индекс для наименования
            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
