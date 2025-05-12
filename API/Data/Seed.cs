using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class Seed
{
    public static async Task SeedUsers(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        // Check to see if any users already exist
        // If users are in the DB, no need to reseed
        if (await userManager.Users.AnyAsync()) return;

        // Get seed data asynchronously from the UserSeedData.json file
        var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");

        // Create new options, in this case making property namecase insensitive
        var options = new JsonSerializerOptions{PropertyNameCaseInsensitive = true};

        // Create a list of AppUser from the JSON file
        var users = JsonSerializer.Deserialize<List<AppUser>>(userData, options);

        // Check for users being null
        if (users == null) return;

        // Create a list of roles
        var roles = new List<AppRole>
        {
            new() {Name = "Member"},
            new() {Name = "Admin"},
            new() {Name = "Moderator"},
        };

        // For each role in roles create the role in the roleManager, passing in the role
        foreach (var role in roles)
        {
            await roleManager.CreateAsync(role);
        }

        // For each user, set the username to lowercase
        // In this demo, the passwords are the same for simplicity
        // Each member is then assinged a default role of Member
        foreach (var user in users)
        {
            // creating a complex password for seed data
            user.UserName = user.UserName!.ToLower();
            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, "Member");
        }

        // Create a new AppUser called admin with their properties set
        var admin = new AppUser
        {
            UserName = "admin",
            KnownAs = "Admin",
            Gender = "",
            City = "",
            Country = ""
        };

        // Create the admin profile, add it to the userManager with the admin object, and "super strong" password
        await userManager.CreateAsync(admin, "Pa$$w0rd");

        // Add the roles to the admin profile
        await userManager.AddToRolesAsync(admin, ["Admin", "Moderator"]);

        // Do not need anymore because above calls the async functionality
        // Keeping for historic information
        // await context.SaveChangesAsync();
    }
}
