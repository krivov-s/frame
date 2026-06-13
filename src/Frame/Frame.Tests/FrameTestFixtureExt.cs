namespace Frame.Tests;

/// <summary>
/// К базовому FrameTestFixture добавляется создание БД в конструкторе и асинхронная инициализация, внутри которой
/// осуществляется логин в систему пользователя testuser 
/// </summary>
public class FrameTestFixtureExt : FrameTestFixture, IAsyncLifetime
{
    public FrameTestFixtureExt()
    {
        CreateInitDatabase();
    }
   
    public Task InitializeAsync()
    {
        return LoginTestuserAsync();
    }

    public async Task RecreateDatabaseAsync()
    {
        CreateInitDatabase();
        await InitializeAsync();
        if (OnOneTimeSetUpAsync != null)
        {
            await OnOneTimeSetUpAsync();
        }
        await LoginTestuserAsync();
    }
    
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public Func<Task>? OnOneTimeSetUpAsync { get; set; }
    private bool _oneTimeSetupDone;
    public async Task OneTimeSetUpAsync()
    {
        if (OnOneTimeSetUpAsync != null && !_oneTimeSetupDone)
        {
            await OnOneTimeSetUpAsync();
            _oneTimeSetupDone = true;
        }
    }
    
}