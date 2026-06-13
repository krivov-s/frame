using FluentAssertions;
using Frame.App.IEntityLoaders;
using Frame.Shared;
using Frame.Domain.Entities.Core.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Tests.TestCore;

[Collection("DisableParallelism")]
public class TestSecurityRepos(FrameTestFixtureExt fixture) : IClassFixture<FrameTestFixtureExt>
{
    private readonly FrameTestFixtureExt _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));

    [Fact]
    public async Task TestUserLoader()
    {
        IUserLoader? loader = _fixture.ServiceProvider.GetService<IUserLoader>();
        loader.Should().NotBeNull();
        Result<User?> result = await loader!.LoadByNameAsync("testuser");
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value!.Login.Should().Be("testuser");
        result = await loader.LoadByNameAsync("testuserXXX");
        result.IsError.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
