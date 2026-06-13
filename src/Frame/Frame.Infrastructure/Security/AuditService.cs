using Frame.App.Security;
using Frame.Domain.Entities.Core.Security;
using Frame.Infrastructure.DBContext;

namespace Frame.Infrastructure.Security;

public class AuditService(NoSecurityDbContext ctx): IAuditService
{
    public async Task WriteAsync(AuditRecord auditRecord)
    {
        ctx.Add(auditRecord);
        await ctx.SaveChangesAsync();
    }
}