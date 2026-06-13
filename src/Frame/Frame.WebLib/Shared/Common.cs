
using Frame.Domain.Entities.Core;

namespace Frame.WebLib.Shared
{
    public static class WebLibHelpers
    {
        public enum ECascadingParamType
        {
            CanAdd,
            CanModify,
            CanDelete
        };
        public static string GetCascadingParamName<TEntity>(ECascadingParamType paramType)
        {
            switch (paramType)
            {
                case ECascadingParamType.CanAdd:
                    return $"{typeof(TEntity)}_CanAdd";
                case ECascadingParamType.CanModify:
                    return $"{typeof(TEntity)}_CanModify";
                case ECascadingParamType.CanDelete:
                    return $"{typeof(TEntity)}_CanDelete";
                default:
                    return string.Empty;
            }
        }
        
        public static string GetDefaultFormat<TEntity>(Field<TEntity> field) where TEntity : BaseEntity
        {
            if(field == null)
            {
                return "";
            }
            
            return field.GetDefaultFormat();

            // if (!string.IsNullOrEmpty(field.Format))
            // {
            //     return field.Format;
            // }
            //
            // Type t = typeof(TValue);
            // if (t == typeof(int))
            // {
            //     return "N0";
            // }
            //
            // if (t == typeof(decimal))
            // {
            //     return "N2";
            // }
            //
            // if (t == typeof(DateTime))
            // {
            //     return "dd.MM.yyyy HH:mm:ss";
            // }
            //
            // if (t == typeof(DateOnly))
            // {
            //     return "dd.MM.yyyy";
            // }
            //
            // return "";
        }
        
    }
}
