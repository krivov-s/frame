using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Frame.Domain.Entities.Test;

namespace Frame.Infrastructure.EntityDbConfigurations.Test
{
    internal class TestConfigurations : IEntityTypeConfiguration<TestDerivedClass>
    {
        public void Configure(EntityTypeBuilder<TestDerivedClass> builder)
        {
            builder.Property(p => p.DateAttr).UsesUtc();
        }
    }
}
