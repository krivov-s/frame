using Frame.Domain.Entities.Core.FileDocuments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.FileDocuments
{
    public class FileDocumentConfiguration : BaseGenericEntityConfiguration<FileDocument>, IEntityTypeConfiguration<FileDocument>
    {
        public new void Configure(EntityTypeBuilder<FileDocument> builder)
        {
            base.Configure(builder);

            // Уникальный индекс по ключу файла
            builder.HasIndex(x => x.FileKey).IsUnique();
        }
    }
}
