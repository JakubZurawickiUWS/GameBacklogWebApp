using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using GameBacklogWebApp.Data;
using System.Threading.Tasks;

namespace GameBacklogWebApp.Filters
{
    public class UserCurrencyFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var user = httpContext.User;
            if (user.Identity.IsAuthenticated)
            {
                var userManager = httpContext.RequestServices.GetService<UserManager<IdentityUser>>();
                var db = httpContext.RequestServices.GetService<GameBacklogWebAppContext>();
                var userId = userManager.GetUserId(user);
                var wallet = await db.UserWallets.FindAsync(userId);

                (context.Controller as Microsoft.AspNetCore.Mvc.Controller).ViewData["UserCurrency"] = wallet?.Currency ?? 0;
            }

            await next();
        }
    }
}
