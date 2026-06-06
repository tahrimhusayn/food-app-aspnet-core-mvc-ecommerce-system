using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using Food_App.Models;

namespace Food_App.Attributes
{
    public class AuthorizeRoleAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _roles;

        public AuthorizeRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();

            if (!signInManager.IsSignedIn(context.HttpContext.User))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var user = await userManager.GetUserAsync(context.HttpContext.User);
            if (user == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var userRoles = await userManager.GetRolesAsync(user);
            if (!_roles.Any(role => userRoles.Contains(role)))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                return;
            }
        }
    }
}
