using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Frame.Infrastructure
{
    internal static class PropertyBuilderExtensions
    {
        //private static DateTime? FromCodeToData(DateTime? fromCode, string name)
        //{
        //    if(fromCode == null || fromCode.Value.Kind == DateTimeKind.Utc)
        //    {
        //        return fromCode;
        //    }
        //    else
        //    {
        //        throw new InvalidOperationException($"Колонка {name} принимает дату-время исключительно в формате UTC");
        //    }
        //}

        //private static DateTime? FromDataToCode(DateTime? fromData)
        //{
        //    if (fromData != null)
        //    {
        //        if(fromData.Value.Kind == DateTimeKind.Unspecified)
        //        {
        //            return new DateTime?(DateTime.SpecifyKind(fromData.Value, DateTimeKind.Utc));
        //        }
        //        else
        //        {
        //            return new DateTime?(fromData.Value.ToUniversalTime());
        //        }
        //    }
        //    return fromData;
        //}

        private static DateTime FromCodeToData(DateTime fromCode, string name)
            => fromCode.Kind == DateTimeKind.Utc ? fromCode : throw new InvalidOperationException($"Колонка {name} принимает дату-время исключительно в формате UTC");

        private static DateTime FromDataToCode(DateTime fromData)
            => fromData.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(fromData, DateTimeKind.Utc) : fromData.ToUniversalTime();

        public static PropertyBuilder<DateTime?> UsesUtc(this PropertyBuilder<DateTime?> property)
        {
            var name = property.Metadata.Name;
            return property.HasConversion<DateTime?>(
                fromCode => fromCode != null ? FromCodeToData(fromCode.Value, name) : default,
                fromData => fromData != null ? FromDataToCode(fromData.Value) : default
            );
        }

        public static PropertyBuilder<DateTime> UsesUtc(this PropertyBuilder<DateTime> property)
        {
            var name = property.Metadata.Name;
            return property.HasConversion(
                fromCode => FromCodeToData(fromCode, name), 
                fromData => FromDataToCode(fromData)
            );
        }
    }
}
