using Frame.Domain.Entities.Core.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Frame.Infrastructure.EntityDbConfigurations.Core.Reports
{
    public class FrameReportTypeConfiguration : BaseGenericEntityConfiguration<FrameReportType>, IEntityTypeConfiguration<FrameReportType>
    {
        public new void Configure(EntityTypeBuilder<FrameReportType> builder)
        {
            base.Configure(builder);

            // Уникальный индекс по имени
            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
