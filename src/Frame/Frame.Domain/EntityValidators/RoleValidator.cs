using FluentValidation;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Domain.EntityValidators
{
    public class RoleValidator : BaseEntityValidator<Role>
    {
        public RoleValidator()
        {
            RuleFor(x => x).Custom((role, _) => { role.NormalizedRoleName = role.Name.Trim().ToUpper(); });
        }
    }
}
