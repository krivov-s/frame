using Frame.Domain.Entities.Core.FileDocuments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.FileDocuments
{
    public class FileDocumentTypeConfiguration : BaseGenericEntityConfiguration<FileDocumentType>, IEntityTypeConfiguration<FileDocumentType>
    {
        public new void Configure(EntityTypeBuilder<FileDocumentType> builder)
        {
            base.Configure(builder);

            // Уникальный индекс по имени
            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
