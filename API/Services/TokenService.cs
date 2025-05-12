using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace API.Services;

// Config lets us read from appsettings.json, userManager allows us to retrieve user roles
// Implements ITokneService enforcing the CreatToken method
public class TokenService(IConfiguration config, UserManager<AppUser> userManager) : ITokenService
{
    // Generates a signed JWT toke for a given AppUser
    public async Task<string> CreateToken(AppUser user)
    {
        // Get and validate the TokenKey from the appsettings.json
        // Throws an excaption if cannot access it or typo
        var tokenKey = config["TokenKey"] ?? 
            throw new Exception("Cannot access tokenKey from appsettings");
        
        // Validates that the key is atleast 64 characters since using HMAC SHA512 and it needs a long key
        if(tokenKey.Length < 64) throw new Exception("Your tokenKey needs to be longer");

        // Create a security key by converting the token key into a byte array
        // Wrap it in a SymmeticSecurityKey which is usded to sign the JWT
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));

        // If user.Usernmae is null, throw exception
        if (user.UserName == null) throw new Exception("No username for user");

        // Set the claims of the token
        var claims = new List<Claim>
        {
            // Set the user id to a string
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),

            // Store the UserName
            new(ClaimTypes.Name, user.UserName)
        };

        // Add a roles claim by retrieving the roles for the user
        var roles = await userManager.GetRolesAsync(user);

        // Adding roles to user
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Tells the token handler to sign the token using HMAC SHA512
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        // Build the JWT 
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // The list of claims (user infor, roles)
            Subject = new ClaimsIdentity(claims),

            // Sets the token to expire in 7 days, can customize
            Expires = DateTime.UtcNow.AddDays(7),

            // The credentials used to sign the token 
            SigningCredentials = creds
        };

        // Used to create the serialized token
        var tokenHandler = new JwtSecurityTokenHandler();

        // Creates the acutal token
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Serializes the token into a string
        return tokenHandler.WriteToken(token);
    }
}
