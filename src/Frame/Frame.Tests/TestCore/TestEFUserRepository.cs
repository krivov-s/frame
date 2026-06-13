using FluentAssertions;
using Frame.Shared;
using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestEFUserRepository : IClassFixture<FrameTestFixtureExt>
    {
        private readonly FrameTestFixtureExt _fixture;
        private readonly IBaseRepository<User> _repository;
        public TestEFUserRepository(FrameTestFixtureExt fixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));

            _repository = _fixture.ServiceProvider.GetService<IBaseRepository<User>>() 
                                               ?? throw new NullReferenceException("Ошибка получения IBaseRepository<User> от ServiceProvider");
        }

        [Fact]
        public async Task GetUser_Ret_Success()
        {
            IUserRepository userRepository = (_repository as IUserRepository)!;
            userRepository.Should().NotBeNull("Полученный репозиторий должен быть IUserRepository");

            Result<List<User>> result = await _repository.GetAllAsync();

            result.IsError.Should().BeFalse();
            result.Value.Should().NotBeNull();
            result.Value!.Count.Should().BeGreaterThan(0);
            User user = result.Value[0];
            Result<User> result1 = await userRepository.GetUserAsync(user.Id);
            result1.IsError.Should().BeFalse();
            result1.Value.Should().NotBeNull();
            User user1 = result1.Value!;
            user1.Id.Should().Be(user.Id);
        }
    }
}
