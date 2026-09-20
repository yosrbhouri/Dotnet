using DeliveryApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DeliveryApp.Filters
{
    public class ProfilePictureFilter : IAsyncActionFilter
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfilePictureFilter(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(context.HttpContext.User);
                if (user != null && context.Controller is Controller controller)
                {
                    controller.ViewBag.CurrentUserProfilePic = user.ProfilePicturePath;
                    controller.ViewBag.CurrentUserFullName = user.FullName;
                    controller.ViewBag.CurrentUserType = user.TypeUtilisateur switch
                    {
                        UserType.Client => "Client",
                        UserType.Livreur => "Livreur",
                        _ => "Utilisateur"
                    };
                }
            }

            await next();
        }
    }
}
