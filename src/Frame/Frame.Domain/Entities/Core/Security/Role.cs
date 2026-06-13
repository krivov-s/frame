
namespace Frame.Domain.Entities.Core.Security
{
    public class Role : BaseEntity, IBaseGenericEntity<Role>
    {
        #region ========== BaseEntity attributes ========== 
        //public override int Id { get; set; }
        public override string Description => Name;
        #endregion

        public string Name { get; set; } = string.Empty;
        public string NormalizedRoleName { get; set; } = string.Empty;
        public string Descr { get; set; } = "";
        public virtual List<RoleClaim> RoleClaims { get; set; } = [];
        public virtual List<UsersRoles> UsersRoles { get; set; } = [];
        public virtual List<TEntityRights> EntityRights { get; set; } = [];

        public int UsersCount => UsersRoles.Count;

        public List<Field<Role>> GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Роль пользователя";
            public static readonly Field<Role> Name = new()
            {
                Name = nameof(Name), 
                StringGet = x => x.Name, 
                StringSet = (x, val) => x.Name = val ?? "", 
                Required = true, 
                MaxLength = 80, 
                HumanName = "Имя роли"
            };
            public static readonly Field<Role> NormalizedRoleName = new()
            {
                Name = nameof(NormalizedRoleName), 
                StringGet = x => x.NormalizedRoleName, 
                Required = true, 
                MaxLength = 80, 
                HumanName = "UpperCase role name"
            };
            public static readonly Field<Role> Descr = new()
            {
                Name = nameof(Descr), 
                StringGet = x => x.Descr, 
                StringSet = (x, val) => x.Descr = val ?? "", 
                Required = false, 
                MaxLength = 2048, 
                HumanName = "Описание"
            };
            public static readonly Field<Role> UsersCount = new()
            {
                Name = nameof(UsersCount), 
                IntGet = x => x.UsersCount, 
                Required = false, 
                HumanName = "Кол-во пользователей", 
                IsPersistent = false
            };

            public static readonly List<Field<Role>> Fields = [Name, NormalizedRoleName, Descr, UsersCount];
        }
    }
}
