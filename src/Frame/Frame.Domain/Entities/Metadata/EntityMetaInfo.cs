namespace Frame.Domain.Entities.Metadata;

public class EntityMetaInfo
{
    public required string HumanName { get; set; } = "";
    public required Type DotNetType { get; set; }
    public List<FieldMetaInfo> Fields { get; set; } = [];
}