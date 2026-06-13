
namespace Frame.Domain.Entities.Core.Security
{
    public class UsersRoles : BaseEntity, IBaseGenericEntity<UsersRoles>
    {
        public override string Description => "Связка Пользователь - Роль";

        virtual public User? User { get; set; }
        public int UserId { get; set; }
        virtual public Role? Role { get; set; }
        public int RoleId { get; set; }

        public List<Field<UsersRoles>> GetFields() { return Meta.Fields; }

        #region Metadata
        public static class Meta
        {
            public static readonly string HumanName = "Связка Роль-Пользователь";
            public static readonly RefField<UsersRoles, User> User = new()
            {
                Name = nameof(User),
                RefGetter = x => x.User,
                RefIdGetter = x => x.UserId,
                RefSetter = (x, val) => x.User = val,
                RefIdSetter = (x, val) => x.UserId = val ?? 0,
                Required = true,
                HumanName = "Пользователь"
            };
            public static readonly RefField<UsersRoles, Role> Role = new()
            {
                Name = nameof(Role),
                RefGetter = x => x.Role,
                RefIdGetter = x => x.RoleId,
                RefSetter = (x, val) => x.Role = val,
                RefIdSetter = (x, val) => x.RoleId = val ?? 0,
                Required = true,
                HumanName = "Роль"
            };

            public static readonly List<Field<UsersRoles>> Fields = [User, Role];
        }
        #endregion
    }
}
