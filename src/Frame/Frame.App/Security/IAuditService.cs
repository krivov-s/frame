using Frame.Domain.Entities.Core.Security;

namespace Frame.App.Security;

public interface IAuditService
{
    public Task WriteAsync(AuditRecord auditRecord);
}