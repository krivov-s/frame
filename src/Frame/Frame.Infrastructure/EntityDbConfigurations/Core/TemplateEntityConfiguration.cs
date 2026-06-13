using Frame.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Frame.Infrastructure.EntityDbConfigurations.Core;

public class TemplateEntityConfiguration : BaseGenericEntityConfiguration<TemplateEntity>, IEntityTypeConfiguration<TemplateEntity>
{
    public new void Configure(EntityTypeBuilder<TemplateEntity> builder)
    {
        base.Configure(builder);

        // Прописываем уникальный индекс Имя шаблона - Пользователь-владелец
        builder.HasIndex(x => new { x.Name, x.EntityTypeName, x.UserOwnerId }).IsUnique();
        
        builder.Ignore(re => re.EntityType);
    }
}
