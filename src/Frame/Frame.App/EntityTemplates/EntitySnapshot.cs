namespace Frame.App.EntityTemplates;

public sealed class EntitySnapshot
{
    public required string EntityType { get; init; }
    public required Dictionary<string, object?> Data { get; init; }
}
