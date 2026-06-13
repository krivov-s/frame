using FluentAssertions;
using Frame.App.Cores;
using Frame.Shared;
using Frame.Domain.Entities.Core.Params;
using Frame.Domain.Params;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestUserCore  : IClassFixture<FrameTestFixtureExt>
    {
        private readonly FrameTestFixtureExt _fixture;

        public TestUserCore(FrameTestFixtureExt fixture)
        {            
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        }

        [Fact]
        public void CanGetLogin()
        {
            IUserCore? userCore = _fixture.GetUserCore();
            userCore.Should().NotBeNull();
            string login = userCore.CurrentUserLogin;
            login.Should().Be("testuser");
        }

        [Fact]
        public void CanGetUserParamList()
        {
            IUserCore? userCore = _fixture.GetUserCore();
            userCore.Should().NotBeNull();
            ParamList userParamList = userCore.UserParamList;
            userParamList.Should().NotBeNull();
        }

        [Fact]
        public async Task CanSaveUserParamListAsync()
        {
            IUserCore? userCore = _fixture.GetUserCore();
            userCore.Should().NotBeNull();

            ParamList userParamList = userCore.UserParamList;
            userParamList.Should().NotBeNull();
            dynamic paramList = userParamList;
            paramList.NewParam = "xxx";
        
            Result plSaved = await userCore.SaveUserProfileAsync();
            plSaved.IsError.Should().BeFalse();
            
            // Принудительно перелогиниваем пользователя, при этом перезачитывается сессия (и вместе с ней список параметров) из БД
            await _fixture.LoginTestuserAsync();
        
            ParamList userParamList_new = userCore.UserParamList;
            userParamList_new.Should().NotBeNull();
            dynamic paramList_new = userParamList_new;
            string newParamValue = paramList_new.NewParam;
            newParamValue.Should().Be("xxx");
        }
    }
}
