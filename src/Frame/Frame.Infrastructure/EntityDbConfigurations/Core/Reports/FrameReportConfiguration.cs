using Frame.Domain.Entities.Core.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.Reports
{
    public class FrameReportConfiguration : BaseGenericEntityConfiguration<FrameReport>, IEntityTypeConfiguration<FrameReport>
    {
        public new void Configure(EntityTypeBuilder<FrameReport> builder)
        {
            base.Configure(builder);

            // Уникальный индекс по имени
            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
