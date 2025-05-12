using System;
using System.Security.Claims;

namespace API.Extensions;

public static class ClaimsPrincipleExtensions
{
    // The ClaimsPrinciple is an object that represents the currenclty authenticated user in ASP.NET Core
    public static string GetUsername(this ClaimsPrincipal user)
    {
        // This method extracts the username from the token
        // ClaimTypes.Name holds the username in the JWT claims
        var username = user.FindFirstValue(ClaimTypes.Name) 
            ?? throw new Exception("Cannot get username from token");
        return username;
    }

       public static int GetUserId(this ClaimsPrincipal user)
    {
        // Similar functionality as above, but extracts the user ID and parses it to an int
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? throw new Exception("Cannot get username from token"));
        return userId;
    }
}
