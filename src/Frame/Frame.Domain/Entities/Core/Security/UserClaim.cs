using System.ComponentModel.DataAnnotations;

namespace Frame.Domain.Entities.Core.Security
{
    public class UserClaim : BaseEntity, IBaseGenericEntity<UserClaim>
    {
        public override string Description => "";

        virtual public User? User { get; set; }
        public int UserId { get; set; }
        public string ClaimType { get; set; } = string.Empty;
        public string ClaimValue { get; set; } = string.Empty;

        #region Metadata
        public static class Meta
        {
            public static readonly string HumanName = "Требование пользователя";
            public static readonly RefField<UserClaim, User> User = new()
            {
                Name = nameof(User),
                RefGetter = x => x.User,
                RefIdGetter = x => x.UserId,
                RefSetter = (x, val) => x.User = val,
                RefIdSetter = (x, val) => x.UserId = val ?? 0,
                Required = true,
                HumanName = "Пользователь"
            };
            public static readonly Field<UserClaim> ClaimType = new()
            {
                Name = nameof(ClaimType), 
                StringGet = f => f.ClaimType, 
                StringSet = (f, val) => f.ClaimType = val ?? "", 
                Required = true, 
                MaxLength = 80, 
                HumanName = "Тип требования"
            };
            public static readonly Field<UserClaim> ClaimValue = new()
            {
                Name = nameof(ClaimValue), 
                StringGet = f => f.ClaimValue, 
                StringSet = (f, val) => f.ClaimValue = val ?? "", 
                Required = true, 
                MaxLength = 8192, 
                HumanName = "Значение требования"
            };

            public static readonly List<Field<UserClaim>> Fields = [User, ClaimType, ClaimValue];
        }
        public List<Field<UserClaim>> GetFields() { return Meta.Fields; }
        #endregion

    }
}
