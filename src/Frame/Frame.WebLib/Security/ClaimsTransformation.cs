using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Frame.App.Security;
using Frame.Shared;

namespace Frame.WebLib.Security
{
    public class ClaimsTransformation : IClaimsTransformation
    {
        private readonly IUserSecurityDataManager _userSecurityDataManager;
        private UserSession? _userSession;

        public ClaimsTransformation(IUserSecurityDataManager userSecurityDataManager) //, IUserCore userCore
        {
            _userSecurityDataManager = userSecurityDataManager ?? throw new ArgumentNullException(nameof(userSecurityDataManager));
            //_userCore = userCore ?? throw new ArgumentNullException(nameof(userCore));
            _userSession = null;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new Exception("В метод TransformAsync передан ClaimsPrincipal = null");
            }
            if (principal.Identity == null)
            {
                throw new Exception("В метод TransformAsync передан ClaimsPrincipal у которого Identity = null");
            }
            if (string.IsNullOrEmpty(principal.Identity.Name))
            {
                throw new Exception("В метод TransformAsync передан ClaimsPrincipal.Identity у которого не установлено имя");
            }

            // Cast the principal identity to a Claims identity to access claims etc...
            ClaimsIdentity oldIdentity = (ClaimsIdentity)principal.Identity;

            // "Clone" the old identity to avoid nasty side effects.
            // Последний аргумент (ClaimTypes.Role) задает тот тип Claim-а, который системой будет считаться как тип, определяющий роль
            // После этого вызов ClaimsPrincipal.IsInRole в этом ClaimsIdentity будет искать роль в Claim-ах с типом ClaimTypes.Role
            // Вместо него можно было бы использовать свой тип, свою строку, например VSPassportUserRole, но я решил оставить системную.
            ClaimsIdentity newIdentity = new(
                oldIdentity.Claims,
                oldIdentity.AuthenticationType,
                oldIdentity.NameClaimType,
                ClaimTypes.Role);

            if (string.IsNullOrEmpty(newIdentity.Name))
            {
                throw new Exception("В метод TransformAsync передан ClaimsPrincipal.Identity у которого не установлено имя");
            }
            
            string login = newIdentity.Name;
            Result<UserSession> resSession = await _userSecurityDataManager.LoadUserSessionAsync(login);
            if(resSession.IsError)
            {
                return new ClaimsPrincipal(new ClaimsIdentity());
                // throw new Exception(resSession.ErrorResult);
            }
            _userSession = resSession.Value;
            if (_userSession == null)
            {
                return new ClaimsPrincipal(new ClaimsIdentity());
                //throw new Exception($"Ошибка при получении UserSession для пользователя с именем {newIdentity.Name}");
            }

            // -----------------------------------------------------------------------
            // Добавляем все индивидуальные Claims 
            // -----------------------------------------------------------------------
            if (_userSession.User.UserClaims != null)
            {
                newIdentity.AddClaims(_userSession.User.UserClaims.Select(r => new Claim(r.ClaimType, r.ClaimValue)));
            }
            // -----------------------------------------------------------------------

            // -----------------------------------------------------------------------
            // Добавляем все роли как Claims 
            // -----------------------------------------------------------------------
            if (_userSession.User.UsersRoles != null)
            {
                newIdentity.AddClaims(_userSession.User.UsersRoles.Select(ur => new Claim(ClaimTypes.Role, ur.Role.Name)));
            }
            // -----------------------------------------------------------------------

            // -----------------------------------------------------------------------
            // Если у пользователя взведена галка "ChangePassOnNextLogin" - создаем соответствующий Claim 
            // -----------------------------------------------------------------------
            if (_userSession.User.ChangePassOnNextLogin)
            {
                newIdentity.AddClaim(new Claim(nameof(Domain.Entities.Core.Security.User.ChangePassOnNextLogin), ""));
            }
            
            // Инициализируем UserCore текущим пользователем, в дальнейшем в этой сессии будет использоваться
            // именно этот экземпляр, поскольку UserCore зарегистрирован в DI как Scoped.  
            //_userCore.Initialize(login);
            
            // Create and return a new claims principal
            return new ClaimsPrincipal(newIdentity);
        }
    }
}

//foreach (UserClaim _claim in userSession.User.UserClaims)
//{
//    Claim claim = new Claim(_claim.ClaimType, _claim.ClaimValue);
//    newIdentity.AddClaim(claim);
//}
//foreach (UsersRoles _user_role in userSession.User.UsersRoles)
//{
//    if(_user_role.Role != null)
//    {
//        Claim claim = new Claim(ClaimTypes.Role, _user_role.Role.HumanName!);
//        newIdentity.AddClaim(claim);
//    }
//}
// Fetch the roles for the user and add the claims of the correct type so that roles can be recognized.
//var roles = await _roleProvider.GetUserRolesAsync(newIdentity.HumanName);
//newIdentity.AddClaims(roles.Select(r => new Claim(NavMenuRoleClaimType, r)));

// /// FOR TEST PURPOSE ONLY !!!
// newIdentity.AddClaim(new Claim(ClaimTypes.Role, "Administrators"));
// newIdentity.AddClaim(new Claim(ClaimTypes.Role, "Users"));

