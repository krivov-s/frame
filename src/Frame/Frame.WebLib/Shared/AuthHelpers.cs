using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Frame.App.Security;
using Frame.Shared;
using MudBlazor;

namespace Frame.WebLib.Shared
{
    public static class AuthHelpers
    {
        public static async Task<bool> CheckAuthorized<TComponent>( IGetCurrentUserNameService? getCurrentUserService, 
                                                    ILogger<TComponent>? logger,
                                                    ISnackbar? snackBar = null,
                                                    NavigationManager? navigation = null,
                                                    string notAuthorizedPage = "login")
        {
            bool bResult = false;

            if (getCurrentUserService != null)
            {
                string strUserName = await getCurrentUserService.GetLoginAsync();
                if (strUserName.Length == 0)
                {
                    logger?.LogSecurity($"Попытка неавторизованного доступа к {typeof(TComponent).Name}", LogLevel.Warning);
                    snackBar?.Add("Вы не авторизованы на данной странице", Severity.Warning);

                    if (navigation != null && notAuthorizedPage.Length > 0)
                    {
                        navigation.NavigateTo(notAuthorizedPage);
                    }
                }
                else
                {
                    bResult = true;
                }
            }
            return bResult;
        }
    }

    //static public
    //    Func<RedirectContext<CookieAuthenticationOptions>, Task> ReplaceRedirector(
    //        HttpStatusCode statusCode,
    //        Func<RedirectContext<CookieAuthenticationOptions>, Task> existingRedirector) =>
    //            context =>
    //            {
    //                if (context.Request.Path.StartsWithSegments("/api"))
    //                {
    //                    context.Response.StatusCode = (int)statusCode;
    //                    return Task.CompletedTask;
    //                }
    //                return existingRedirector(context);
    //            };

    //public class DemandRoleAttribute : AuthorizeAttribute
    //{
    //    protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
    //    {
    //        if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
    //        {
    //            base.HandleUnauthorizedRequest(filterContext);
    //        }
    //        else
    //        {
    //            filterContext.Result = new NotAuthorizedResult();
    //        }
    //    }
    //}
}
