using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Frame.Domain.Entities.Core;

namespace Frame.Infrastructure.EntityDbConfigurations.Core
{
    public class BaseGenericEntityConfiguration<T> where T : BaseEntity, IBaseGenericEntity<T>, new()
    {
        public void Configure(EntityTypeBuilder<T> builder)
        {
            EntityConfigurationHelpers.DoConfigure(builder);
        }
    }

    public static class EntityConfigurationHelpers
    {
        public static void DoConfigure<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : BaseEntity, new()
        {
            builder.Property(p => p.Id).UseIdentityColumn();
            builder.Property(p => p.DateCreate).IsRequired().UsesUtc();
            builder.Property(p => p.DateModify).IsRequired(false).UsesUtc();

            List<Field<TEntity>>? fields = null;
            // Фейковое создание временного объекта, чтобы получить список полей
            TEntity obj = new();
            // List<Field<TEntity>> fields = obj.GetFields();
            // if (obj is BaseGenericEntity<TEntity> baseGenericEntity)
            // {
            //     fields = baseGenericEntity.GetFields();
            // }
            if (obj is IBaseGenericEntity<TEntity> baseGenericEntityI)
            {
                fields = baseGenericEntityI.GetFields();
            }
 
            if (fields == null)
            {
                string err = $"Невозможно выполнить конфигурирование БД для {typeof(TEntity).Name}: не удалось получить список полей";
                throw new Exception(err);
            }
            
            foreach (Field<TEntity> field in fields)
            {
                string fieldName = field.Name;
                if (fieldName.Length == 0)
                {
                    throw new Exception($"Тип {typeof(TEntity).Name}, поле {field.HumanName}: не задан атрибут Name");
                }

                if (field.IsPersistent)
                {
                    if (field.IntGet != null)
                        builder.Property(fieldName).IsRequired(field.Required);
                    if (field.StringGet != null)
                        builder.Property(fieldName).IsRequired(field.Required).HasMaxLength(field.MaxLength);
                    if (field.DateTimeGet != null)
                        builder.Property<DateTime?>(fieldName).IsRequired(field.Required).UsesUtc();
                    if (field.BoolGet != null)
                        builder.Property(fieldName).IsRequired(field.Required);
                    if (field.DecimalGet != null)
                        builder.Property(fieldName).IsRequired(field.Required);
                }
                else
                {
                    builder.Ignore(fieldName);
                }
                // Здесь нельзя ставить конфигурирование навигационных свойств, поскольку это входит в конфликт
                // с настрйками EF Core по-умолчанию!!!
                // if (field.RefGet != null)
                //     builder.Property(fieldName).IsRequired(field.Required);
            }
        }
    }
}






//public class SomeEntityConfiguration : IEntityTypeConfiguration<SomeEntity>
//{
//}