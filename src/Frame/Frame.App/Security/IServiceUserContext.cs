namespace Frame.App.Security;

public interface IServiceUserContext
{
    public Task<bool> UseServiceUserNameAsync();
}