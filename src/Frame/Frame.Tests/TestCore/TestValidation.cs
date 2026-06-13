using FluentAssertions;
using FluentValidation.Results;
using Frame.App.IEntityRepositories;
using Frame.Shared;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.EntityValidators;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestValidation : IClassFixture<FrameTestFixtureExt>
    {
        private readonly FrameTestFixtureExt _fixture;

        public TestValidation(FrameTestFixtureExt fixture)
        {            
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        }

        [Fact]
        public void TestFieldValidation()
        {
            User user = new();
            UserValidator validator = new();
            ValidationResult result = validator.Validate(user);
            result.IsValid.Should().BeFalse();
            result.Errors.Count.Should().Be(3);
            user.Login = "testuser";
            user.NormalizedLogin = "TESTUSER";
            user.SetPassword("111");
            result = validator.Validate(user);
            result.IsValid.Should().BeTrue();
            result.Errors.Count.Should().Be(0);
        }

        [Fact]
        public async Task TestBaseRepositoryValidationAsync()
        {
            IObjectStorage? storage = _fixture.ServiceProvider.GetService<IObjectStorage>();
            storage.Should().NotBeNull();

            // Когда мы запрашиваем репозиторий у IObjectStorage Validator для него устанавливается 
            // самим ObjectStorage путем обращения к фабрике валидаторов.
            Result<IBaseRepository<User>> resRepository = storage!.GetBaseRepository<User>();
            resRepository.IsError.Should().BeFalse();
            resRepository.Value.Should().NotBeNull();
            IUserRepository? userRepository = (resRepository.Value as IUserRepository);
            userRepository.Should().NotBeNull("Полученный репозиторий должен быть IUserRepository");

            IBaseRepository<User> repoWithValidator = resRepository.Value!;
            
            // Когда мы запрашиваем репозиторий у IObjectStorage Validator для него устанавливается 
            // также из DI, поэтому он будет предоставлен только в случае его предварительной регистрации!
            // !!! Обращения к фабрике валидаторов не происходит !!! поэтому де-факто валидатор отсутствует.
            IBaseRepository<User>? repoNoValidator = _fixture.ServiceProvider.GetService<IBaseRepository<User>>();
            repoNoValidator.Should().NotBeNull();
            
            User user = new();
            
            Result result = await repoWithValidator.AddAsync(user);
            result.IsError.Should().BeTrue("Репозиторий от ObjectStorage: должна быть ошибка о том, что не заполнены необходимые параметры пользователя");

            result = await repoNoValidator!.AddAsync(user);
            result.IsError.Should().BeFalse("Репозиторий от DI: Validator должен отсутствовать, проверки не выполняются");
            
            user.Login = "testuser";
            user.NormalizedLogin = "TESTUSER";
            user.SetPassword("111");
            
            result = await repoWithValidator.AddAsync(user);
            result.IsError.Should().BeFalse();
        }

        [Fact]
        public async Task TestObjectStorageValidationAsync()
        {
            IObjectStorage? storage = _fixture.ServiceProvider.GetService<IObjectStorage>();
            storage.Should().NotBeNull();
            
            User user = new();
            
            Result result = storage!.Add<User>(user);
            result.IsError.Should().BeFalse("Здесь д.б. пофиг на корректность, т.к. валидация выполняется при физическом сохранении.");
            
            Result resWithError = await storage.SaveChangesAsync();
            resWithError.IsError.Should().BeTrue("Должен был сработать валидатор и выдать ошибку");

            user.Login = "testuser_new";
            user.NormalizedLogin = "TESTUSER_NEW";
            user.SetPassword("111");
            
            Result resNoError = await storage.SaveChangesAsync();
            resNoError.IsError.Should().BeFalse("Валидатор должен быть убедиться в отсутствии ошибок и разрешить запись");
            result.IsError.Should().BeFalse();
        }
    }
}
