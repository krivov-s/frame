using System.Security.Principal;
using Frame.App.Security;

namespace Frame.Tests.Shared
{
    public class TestCurrentUserService(string strLogin = "testuser") : IGetCurrentUserNameService
    {
        private readonly string _login = strLogin;

        public Task<IIdentity?> GetIdentityAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetLoginAsync()
        {
            return Task.FromResult(_login);
        }

        public string GetLogin()
        {
            return _login;
        }
        
    }
}
