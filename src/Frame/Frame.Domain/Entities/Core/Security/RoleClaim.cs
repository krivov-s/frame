using System.ComponentModel.DataAnnotations;

namespace Frame.Domain.Entities.Core.Security
{
    public class RoleClaim : BaseEntity, IBaseGenericEntity<RoleClaim>
    {
        [Required] virtual public Role? Role { get; set; }
        [Required] public int RoleId { get; set; }
        [Required] public string ClaimType { get; set; } = string.Empty;
        [Required] public string ClaimValue { get; set; } = string.Empty;

        #region Metadata
        public static class Meta
        {
            public static readonly string HumanName = "Требование роли";
            public static readonly RefField<RoleClaim, Role> Role = new()
            {
                Name = nameof(Role),
                RefGetter = x => x.Role,
                RefIdGetter = x => x.RoleId,
                RefSetter = (x, val) => x.Role = val,
                RefIdSetter = (x, val) => x.RoleId = val ?? 0,
                Required = true,
                HumanName = "Роль"
            };
            public static readonly Field<RoleClaim> ClaimType = new()
            {
                Name = nameof(ClaimType), 
                StringGet = f => f.ClaimType, 
                StringSet = (f, val) => f.ClaimType = val ?? "", 
                Required = true, 
                MaxLength = 80, 
                HumanName = "Тип требования"
            };
            public static readonly Field<RoleClaim> ClaimValue = new()
            {
                Name = nameof(ClaimValue), 
                StringGet = f => f.ClaimValue, 
                StringSet = (f, val) => f.ClaimValue = val ?? "", 
                Required = true, 
                HumanName = "Значение требования"
            };

            public static readonly List<Field<RoleClaim>> Fields = [Role, ClaimType, ClaimValue];
        }
        public List<Field<RoleClaim>> GetFields() { return Meta.Fields; }
        #endregion
    }
}
