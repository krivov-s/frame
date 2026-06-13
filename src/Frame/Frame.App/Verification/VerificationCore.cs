using Frame.Shared;

namespace Frame.App.Verification;

public class VerificationCore : IVerificationCore
{
    private readonly object _lock = new();
    private readonly Dictionary<string, VerificationTypeMetadata> _metadata = [];
    
    /// <summary>
    /// <inheritdoc/> 
    /// </summary>
    public void RegisterEntityType(string entityType, bool isRoot, string pathToRoot)
    {
        Result<VerificationTypeMetadata> res = GetMetadata(entityType);
        if (res.IsError) return;
        VerificationTypeMetadata meta = res.Value ?? 
                                        new VerificationTypeMetadata()
                                        {
                                            EntityType =  entityType, IsRoot = isRoot
                                        };
        
        meta.Paths.Add(new RootPath()
        {
            RootPath = pathToRoot,
        });
    }
    
    /// <summary>
    /// <inheritdoc/> 
    /// </summary>
    public void RegisterFileDocumentRoot(string entityType)
    {
    }

    /// <summary>
    /// <inheritdoc/> 
    /// </summary>
    public Result<VerificationTypeMetadata> GetMetadata(string entityType)
    {
        lock (_lock)
        {
            VerificationTypeMetadata? meta = _metadata.GetValueOrDefault(entityType);
            if (meta == null)
            {
                string msg = $"Метаданные верификации для типа {entityType} не зарегистрированы";
                return Result<VerificationTypeMetadata>.SuccessWithMessage(meta, msg);
            }
            return Result<VerificationTypeMetadata>.Success(meta);
        }
    }
    
}