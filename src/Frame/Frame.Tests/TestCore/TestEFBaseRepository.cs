using FluentAssertions;
using Frame.App.IEntityRepositories;
using Frame.Shared;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestEFBaseRepository : IClassFixture<FrameTestFixtureExt>
    {
        private readonly FrameTestFixtureExt _fixture;
        private readonly IBaseRepository<User> _baseRepository;

        public TestEFBaseRepository(FrameTestFixtureExt fixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            
            _baseRepository = _fixture.CreateNewRepository<User>();
        }

        [Fact]
        public async Task Add_New_Ret_Success()
        {
            IBaseRepository<User> repository = _fixture.CreateNewRepository<User>();

            User user = new() { Login = "xxx", FIO = "Ivanov" };
            user.SetPassword("XXX");
            Result result = await repository.AddAsync(user);

            result.IsError.Should().BeFalse(result.ErrorResult);
            result = await repository.ObjectStorage.SaveChangesAsync();
            result.IsError.Should().BeFalse(result.ErrorResult);
            user.Id.Should().BePositive();
        }

        [Fact]
        public async Task Add_New_Validation_Failed()
        {
            IBaseRepository<User> repository = _fixture.CreateNewRepository<User>();

            User user = new() { Login = "", NormalizedLogin = "XXX", FIO = "Ivanov" };
            user.SetPassword("XXX");
            Result result = await repository.AddAsync(user);

             result.IsError.Should().BeTrue();
        }

        [Fact]
        public async Task GetById_Ret_Success()
        {
            IBaseRepository<User> repositoryUser = _fixture.CreateNewRepository<User>();

            const int idExisting = 1;
            Result<User> result = await repositoryUser.GetByIdAsync(idExisting);

            result.IsError.Should().BeFalse();
            result.Value.Should().NotBeNull();
            result.Value!.Id.Should().Be(idExisting);
        }

        [Fact]
        public async Task GetById_Ret_Failed()
        {
            IBaseRepository<User> repositoryUser = _fixture.CreateNewRepository<User>();

            const int idNotExisting = 0;
            Result<User> result = await repositoryUser.GetByIdAsync(idNotExisting);

            result.IsError.Should().BeFalse();
            result.Value.Should().BeNull();
        }

        [Fact]
        public async Task Update_New_Ret_Failed()
        {
            IBaseRepository<User> repositoryUser = _fixture.CreateNewRepository<User>();

            User user = new() { Login = "yyy", NormalizedLogin = "YYY", FIO = "Ivanov" };
            user.SetPassword("XXX");

            Result result = await repositoryUser.UpdateAsync(user);
            result.IsError.Should().BeTrue();

            user.Id = 1;
            result = await repositoryUser.UpdateAsync(user);
            result.IsError.Should().BeTrue();
        }

        [Fact]
        public async Task Update_FromAnotherContext_Ret_Failed()
        {
            IBaseRepository<User> repositoryUser = _fixture.CreateNewRepository<User>();
            Result<User> resUser = await repositoryUser.GetByIdAsync(1);
            resUser.IsError.Should().BeFalse();
            User? user = resUser.Value;
            user.Should().NotBeNull();

            Result result = await _baseRepository.UpdateAsync(user!);
            result.IsError.Should().BeTrue();
        }

        [Fact]
        public async Task Update_FromSameContext_Validate_Failed()
        {
            IBaseRepository<User> repositoryUser = _fixture.CreateNewRepository<User>();

            User? user = await repositoryUser.GetByIdAsync(1);
            user.Should().NotBeNull();
        }

        [Fact]
        public async Task Update_FromSameContext_Ret_Success()
        {
            IBaseRepository<User> repositoryUser = _fixture.CreateNewRepository<User>();

            User? user = await repositoryUser.GetByIdAsync(1);
            //User? user = await _appDbContext.Set<User>().Where(u => u.Id == 1).SingleOrDefaultAsync();
            user.Should().NotBeNull();

            const string sNewFIO = "СИДОРОВ";
            user!.FIO = sNewFIO;

            Result result = await repositoryUser.UpdateAsync(user);
            result.IsError.Should().BeFalse();

            Result<User> result2 = await repositoryUser.GetByIdAsync(user.Id);
            result2.IsError.Should().BeFalse();
            result2.Value.Should().NotBeNull();
            result2.Value!.FIO.Should().Be(sNewFIO);
        }

        [Fact]
        public async Task Update_FromSameContext_Through_List_Ret_Failed()
        {
            IBaseRepository<User> repositoryUser = _fixture.CreateNewRepository<User>();

            List<User> list;

            //IObjectStorage objectStorage = Services.GetService<IObjectStorage>()!;
            //objectStorage.Should().NotBeNull();

            Result<List<User>> r1 = await repositoryUser.GetAllAsync();
            r1.IsError.Should().BeFalse();
            r1.Value.Should().NotBeNull();
            list = r1.Value!;

            //User? user = await _appDbContext.Set<User>().Where(u => u.Id == 1).SingleOrDefaultAsync();
            User user = list[0];
            user.Should().NotBeNull();

            const string sNewFIO = "СИДОРОВ";
            user.FIO = sNewFIO;

            Result result = await repositoryUser.UpdateAsync(user);
            result.IsError.Should().BeFalse(result.ErrorResult);

            Result resSave = await repositoryUser.ObjectStorage.SaveChangesAsync();
            resSave.IsError.Should().BeFalse();

            Result<User> result2 = await repositoryUser.GetByIdAsync(user.Id);
            result2.IsError.Should().BeFalse();
            result2.Value.Should().NotBeNull();
            result2.Value!.FIO.Should().Be(sNewFIO);
        }

        [Fact]
        public async Task Delete_ById_NotTracked_Failed()
        {
            IBaseRepository<User> repositoryUser = _fixture.CreateNewRepository<User>();

            User user = new() { Id = 1, Login = "XXX" };
            Result result = await repositoryUser.DeleteAsync(user);
            result.IsError.Should().BeTrue();
        }
    }
}