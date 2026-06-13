namespace Frame.App.Security;

public class ChangedPropValue
{
    public string AttrName { get; set; } = "";
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
}
