using System;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace API.Controllers;

public class AdminController(UserManager<AppUser> userManager) : BaseApiController
{
    [Authorize(Policy = "RequireAdminRole")]
    [HttpGet("users-with-roles")]
    public async Task<ActionResult> GetUsersWithRoles()
    {
        var users = await userManager.Users
            .OrderBy(x => x.UserName)
            .Select(x => new {
                x.Id,
                Username = x.UserName,
                Roles = x.UserRoles.Select(r => r.Role.Name).ToList()
            }).ToListAsync();
        
        return Ok(users);
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("edit-roles/{username}")]
    public async Task<ActionResult> EditRoles(string username, string roles)
    {
        // Making sure a role is selected
        if (string.IsNullOrEmpty(roles)) return BadRequest("You must select at least one role");

        // Get the selected roles, split by a ",", then put the roles into an array
        var selectedRoles = roles.Split(",").ToArray();

        // Get the user
        var user = await userManager.FindByNameAsync(username);

        // Check to make sure we have a user
        if (user == null) return BadRequest("User not found");

        // Get the current roles of the user
        var userRoles = await userManager.GetRolesAsync(user);

        // Update the roles based on the passed in roles and roles user is already in
        var result = await userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

        // If the result is unsuccessful, return bad request
        if (!result.Succeeded) return BadRequest("Failed to add to roles");

        // Remove the roles from the user except for the selected ones
        result = await userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

        // Return a bad request if result fails
        if (!result.Succeeded) return BadRequest("Failed to remove from roles");

        // Return the user with the new roles
        return Ok(await userManager.GetRolesAsync(user));
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpGet("photos-to-moderate")]
    public ActionResult GetPhotosForModeration()
    {
        return Ok("Only Admins or moderators can see this");
    }
}
