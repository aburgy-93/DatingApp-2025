using System;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Helpers;

public class LogUserActivity : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Execute the action and get the result
        var resultContext = await next();

        // If the user identity returned from the context is not authenticated, return
        if (context.HttpContext.User.Identity?.IsAuthenticated != true) return;

        // Get the userId from the authenticated user claims
        var userId = resultContext.HttpContext.User.GetUserId();

        // Get the repository (IUserRepository)
        var repo = resultContext.HttpContext.RequestServices.GetRequiredService<IUserRepository>();

        // return the user from the repo with the userId
        var user = await repo.GetUserByIdAsync(userId);

        // Check if null
        if (user == null) return;

        // Set the LastActive property to now
        user.LastActive = DateTime.UtcNow;

        // Save changes in the repo
        await repo.SaveAllAsync();
    }
}
