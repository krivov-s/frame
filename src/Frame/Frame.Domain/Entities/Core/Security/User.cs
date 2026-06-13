using Frame.Domain.Entities.Core.Params;
using Frame.Shared;

namespace Frame.Domain.Entities.Core.Security
{
    public class User : BaseEntity, IBaseGenericEntity<User>
    {
        public const string SystemUserName = "system";

        #region ========== BaseEntity attributes ========== 
        public override string Description => FIO.Length > 0 ? $"{Meta.HumanName} {FIO}" : $"{Meta.HumanName} {Login}";
        #endregion

        public string Login { get; set; } = string.Empty;
        public string NormalizedLogin { get; set; } = string.Empty;
        public string PasswordHash { get; protected set; } = string.Empty;
        public string FIO { get; set; } = string.Empty;

        public virtual UserProfile UserProfile
        {
            get
            {
                if (_userProfile == null)
                {
                    _userProfile = new()
                    {
                        UserId = Id,
                        User = this
                    };
                }
        
                return _userProfile;
            }
            set => _userProfile = value;
        }
        protected UserProfile? _userProfile;
        
        /// <summary>
        /// Подразделение, к которому относится пользователь
        /// </summary>
        public virtual Division? Division { get; set; }
        public int? DivisionId { get; set; }
        
        public bool ChangePassOnNextLogin { get; set; }

        /// <summary>
        /// Комментарий к пользователю для дополнительной информации.
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Пользователь заблокирован.
        /// </summary>
        public bool Disabled { get; set; }
        
        public virtual List<UserClaim> UserClaims { get; set; } = [];
        public virtual List<UsersRoles> UsersRoles { get; set; } = [];

        public string UserRolesNames
        {
            get
            {
                string[] roleNames = [.. UsersRoles
                    .Select(x => x.Role != null ? x.Role.Name : string.Empty)];
                if (roleNames == null || roleNames.Length == 0)
                    return string.Empty;
                return roleNames.Aggregate((first, second) => $"{first}, {second}");
            } 
        }

        public List<Field<User>> GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Пользователь";
            public static readonly Field<User> Login = new()
            {
                Name = nameof(Login), 
                StringGet = x => x.Login, 
                StringSet = (x, val) => x.Login = val ?? "", 
                Required = true, 
                MaxLength = 80, 
                HumanName = "Логин"
            };
            public static readonly Field<User> NormalizedLogin = new()
            {
                Name = nameof(NormalizedLogin), 
                StringGet = x => x.NormalizedLogin, 
                StringSet = (x, val) => x.NormalizedLogin = val ?? "", 
                Required = true, 
                MaxLength = 80, 
                HumanName = "UpperCase login"
            };
            public static readonly Field<User> PasswordHash = new()
            {
                Name = nameof(PasswordHash), 
                StringGet = x => x.PasswordHash, 
                StringSet = (x, val) => x.PasswordHash = val ?? "", 
                Required = true, 
                MaxLength = 255, 
                HumanName = "Password hash"
            };
            public static readonly Field<User> FIO = new()
            {
                Name = nameof(FIO),
                StringGet = x => x.FIO,
                StringSet = (x, val) => x.FIO = val ?? "",
                Required = false,
                MaxLength = 255,
                HumanName = "ФИО пользователя"
            };
            public static readonly Field<User> Comment = new()
            {
                Name = nameof(Comment),
                StringGet = x => x.Comment,
                StringSet = (x, val) => x.Comment = val ?? "",
                Required = false,
                MaxLength = 255,
                HumanName = "Примечание"
            };
            public static readonly Field<User> Disabled = new()
            {
                Name = nameof(Disabled),
                BoolGet = x => x.Disabled,
                BoolSet = (x, val) => x.Disabled = val ?? false,
                Required = true,
                HumanName = "Пользователь заблокирован"
            };                     
            public static readonly RefField<User, UserProfile> UserProfile = new()
            {
                Name = nameof(UserProfile),
                RefGetter = x => x.UserProfile,
                RefSetter = (x, val) => x.UserProfile = val ?? new(),
                Required = false,
                HumanName = "Профиль пользователя"
            };
            public static readonly RefField<User, Division> Division = new()
            {
                Name = nameof(Division),
                RefGetter = x => x.Division,
                RefIdGetter = x => x.DivisionId,
                RefSetter = (x, val) => x.Division = val,
                RefIdSetter = (x, val) => x.DivisionId = val ?? 0,
                Required = false,
                HumanName = "Подразделение"
            };
            public static readonly Field<User> ChangePassOnNextLogin = new()
            {
                Name = nameof(ChangePassOnNextLogin),
                BoolGet = x => x.ChangePassOnNextLogin,
                BoolSet = (x, val) => x.ChangePassOnNextLogin = val ?? false,
                Required = true,
                HumanName = "Смена пароля при следующем входе"
            };
            
            public static readonly Field<User> UserRolesNames = new()
            {
                Name = nameof(UserRolesNames),
                StringGet = x => x.UserRolesNames,
                Required = false,
                HumanName = "Роли",
                IsPersistent = false
            };


            public static readonly List<Field<User>> Fields = [Login, NormalizedLogin, PasswordHash, FIO, 
                                                               UserProfile, Division, ChangePassOnNextLogin];
        }
        
        /// <summary>
        /// Хэширование и сохранения пароля пользователя
        /// </summary>
        /// <param name="password"></param>
        /// <exception cref="T:BCrypt.Net.SaltParseException">Исключение из используемого класса <see cref="BCrypt"/>
        /// при ошибке хэширования</exception>
        public void SetPassword(string password)
        {
            PasswordHash = (password.Length > 0) ? HashPassword(password) : "";
        }


        /// <summary>
        /// Хэширование пароля
        /// </summary>
        /// <param name="password">Пароль для хэширования</param>
        /// <exception cref="T:BCrypt.Net.SaltParseException">Исключение из используемого класса <see cref="BCrypt"/>
        /// при ошибке хэширования</exception>
        public static string HashPassword(string password)
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(password);
            return hash;
        }
        
        /// <summary>
        /// Проверка пароля на совпадение с ранее установленным паролем пользователя
        /// </summary>
        /// <param name="password">Пароль для проверки</param>
        /// <returns></returns>
        /// <exception cref="FrameException">если вдруг в БД у пользователя отсутствует пароль</exception>
        /// <exception cref="ArgumentException">Exception из BCrypt.Net.BCrypt</exception>
        /// <exception cref="BCrypt.Net.SaltParseException">Exception из BCrypt.Net.BCrypt</exception>
        public bool VerifyPassword(string password)
        {
            if(password.Length > 0 && PasswordHash.Length == 0)
            {
                throw new FrameException("Попытка проверки пароля с пользователем, у которого в БД отсутствует хэш пароли!");
            }
            bool success = BCrypt.Net.BCrypt.Verify(password, PasswordHash);
            return success;
        }
        
    }
}
