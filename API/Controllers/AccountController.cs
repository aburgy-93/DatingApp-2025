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
        if (await UserExists(registerDto.Username)) return BadRequest("Username is taken");

        // using var hmac = new HMACSHA512();

        var user = mapper.Map<AppUser>(registerDto);

        user.UserName = registerDto.Username.ToLower();
        // user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
        // user.PasswordSalt = hmac.Key;

        // Add user to the context/DB
        // context.Users.Add(user);

        var result = await userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded) return BadRequest(result.Errors);

        // Await the saved changes
        // await context.SaveChangesAsync();

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

        if (user == null || user.UserName == null) return Unauthorized("Invalid username");

        // Checking the password
        var result = await userManager.CheckPasswordAsync(user, loginDto.Password);

        if (!result) return Unauthorized();

        // // Use the key (PasswordSalt)
        // using var hmac = new HMACSHA512(user.PasswordSalt);

        // // Compute the hash
        // var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

        // // Compare the computed hash with the hash in the DB
        // for (int i = 0; i < computedHash.Length; i++)
        // {
        //     if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
        // }

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
