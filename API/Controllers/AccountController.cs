using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(UserManager<AppUser> userManager, ITokenService tokenService, 
    IMapper mapper) : BaseApiController
{
    [HttpPost("register")] // account/register
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        // Checking if the entered username is already taken
        if (await UserExists(registerDto.Username)) return BadRequest("Username is taken");


        // Creating the new user by mapping the registerDto data into an AppUser
        var user = mapper.Map<AppUser>(registerDto);

        // Setting the username to all lower case
        user.UserName = registerDto.Username.ToLower();

        // Create the user with the password from the registerDto
        var result = await userManager.CreateAsync(user, registerDto.Password);

        // Check to see if result succeeded, else return a bad request with the error(s)
        if (!result.Succeeded) return BadRequest(result.Errors);

        // Retrun the UserDto with the entered data for the new user
        return new UserDto
        {
            Username = user.UserName,
            Token = await tokenService.CreateToken(user),
            KnownAs = user.KnownAs,
            Gender = user.Gender
        };
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        // Get user if exists, or return null 
        var user = await userManager.Users
            .Include(p => p.Photos)
                .FirstOrDefaultAsync(x => 
                    x.NormalizedUserName == loginDto.Username.ToUpper());

        // If user or user.UserName is null, return unauthorized
        if (user == null || user.UserName == null) return Unauthorized("Invalid username");

        // Checking the password, passing in the user and the password from the DTO
        var result = await userManager.CheckPasswordAsync(user, loginDto.Password);

        // If not result, return unauthorized
        if (!result) return Unauthorized();

        // If successful, reutrn the UserDto with current user data
        return new UserDto
        {
            Username = user.UserName,
            KnownAs = user.KnownAs,
            Token = await tokenService.CreateToken(user),
            Gender = user.Gender,
            PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url
        };
    }

    private async Task<bool> UserExists(string username)
    {
        return await userManager.Users.AnyAsync(x => x.NormalizedUserName == username.ToUpper()); // Bob!= bob
    }
}
