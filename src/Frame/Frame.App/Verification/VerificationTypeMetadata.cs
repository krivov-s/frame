namespace Frame.App.Verification;

public sealed class VerificationTypeMetadata
{
    public string EntityType { get; init; } = "";
    public bool IsRoot { get; init; }
    public List<RootPath> Paths { get; init; } = [];
}

public sealed class RootPath
{
    public string RootType { get; init; } = "";
    public IReadOnlyList<string> NavigationChain { get; init; } = [];
}
