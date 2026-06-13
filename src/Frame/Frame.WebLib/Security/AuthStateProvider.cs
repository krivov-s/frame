using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Frame.WebLib.Security
{
    public class AuthStateProvider(IHttpContextAccessor httpContextAccessor) : AuthenticationStateProvider
    {
        private ClaimsPrincipal? _currentUser;

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_currentUser != null)
            {
                return Task.FromResult(new AuthenticationState(_currentUser));
            }
            else
            {
                if(httpContextAccessor != null && httpContextAccessor.HttpContext != null)
                {
                    var identity = httpContextAccessor.HttpContext.User.Identity as ClaimsIdentity;
                    return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity ?? new ClaimsIdentity())));
                }
                else
                {
                    return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
                }
            }
        }

        public void NotifyAuthenticationStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        
        public Task SetUserAsync(ClaimsPrincipal user)
        {
            _currentUser = user;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            return Task.CompletedTask;
        } 
    }
}
