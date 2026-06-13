using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Principal;
using Frame.App.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Frame.WebLib.Security
{
    public class GetCurrentUserNameService : IGetCurrentUserNameService
    {
       private readonly AuthenticationStateProvider _authenticationStateProvider;
       private readonly IHttpContextAccessor _httpContextAccessor;
       private readonly ILogger _logger;
       
       private string _currentUserName = "";
       
       public GetCurrentUserNameService(AuthenticationStateProvider authenticationStateProvider, 
                                        IHttpContextAccessor? httpContextAccessor,
                                        ILogger<GetCurrentUserNameService>? logger)
        {
            _authenticationStateProvider = 
                authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));
            _httpContextAccessor = 
                httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetLoginAsync()
        {
            if (_currentUserName == "")
            {
                AuthenticationState state = await _authenticationStateProvider.GetAuthenticationStateAsync();
                if (state.User is { Identity.IsAuthenticated: true })
                {
                    _currentUserName = state.User.Identity.Name!;
                }
            }
            return _currentUserName;
        }

        public async Task<IIdentity?> GetIdentityAsync()
        {
            IIdentity? identity = null;
            AuthenticationState state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            if ( state.User != null) 
            {
                identity = state.User.Identity!;
            }
            return identity;
        }

        public string GetLogin()
        {
            if (_currentUserName == "")
            {
                if (_httpContextAccessor.HttpContext != null)
                {
                    ClaimsPrincipal user = _httpContextAccessor.HttpContext.User;
                    Claim? claim = user.FindFirst(ClaimTypes.Name);
                    if (claim != null)
                    {
                        _currentUserName = claim.Value;
                    }
                }
            }
            
            return _currentUserName;
        }
    }
}
