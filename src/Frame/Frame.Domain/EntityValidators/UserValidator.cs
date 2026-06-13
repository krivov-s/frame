using FluentValidation;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Domain.EntityValidators
{
    public class UserValidator : BaseEntityValidator<User>
    {
        public UserValidator()
        {
            RuleFor(x => x).Custom((user, _) => { user.NormalizedLogin = user.Login.Trim().ToUpper(); });
        }
    }
}
