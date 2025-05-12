using System.Security.Claims;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[Authorize]
public class UsersController(IUserRepository userRepository, IMapper mapper, 
    IPhotoService photoService) : BaseApiController
{
    [HttpGet]
    // Should be called GetUser, but didn't notice the misspelling until too deep into development for now
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery]UserParams userParams) 
    {
        // Get the current user from the Claims
        userParams.CurrentUsername = User.GetUsername();

        // Retunr a list of all of the users
        var users = await userRepository.GetMembersAsync(userParams);

        // Add pagination to the list of users
        Response.AddPaginationHeader(users);

        // Return the users
        return Ok(users);
    }

    [HttpGet("{username}")] // /api/users/3
    public async Task<ActionResult<MemberDto>> GetUsers(string username) 
    {
        // Return a user based on their username
        var user = await userRepository.GetMemberAsnyc(username);

        // If no username is found return null
        if(user == null) return NotFound();

        // Retrun the user
        return user;
    }

    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto) 
    {
        // Get the user from the userRepository by getting the user by username of the current user
        var user = await userRepository.GetUserByUserNameAsync(User.GetUsername());

        // If null return a bad request saying could not find user
        if (user == null) return BadRequest("Could not find user");

        // Using IMapper to map the updated user to a memberUpdateDto
        mapper.Map(memberUpdateDto, user);

        // Save the changes, no content is returned
        if (await userRepository.SaveAllAsync()) return NoContent();

        // If an error occurs, return a bad request 
        return BadRequest("Failed to update the user");
    }

    [HttpPost("add-photo")]
    public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
    {
        // Get the user from the userRepository by getting the user by username of the current user
        var user = await userRepository.GetUserByUserNameAsync(User.GetUsername());

        // If null return a bad request saying could not find user
        if (user == null) return BadRequest("Cannot update user");

        // Save the result of user uploading a photo file
        var result = await photoService.AddPhotoAsync(file);

        // If there was an error return a bad request with the error message
        if (result.Error != null) return BadRequest(result.Error.Message);

        // Create a new photo object with the photo url and the id
        var photo = new Photo
        {
            Url = result.SecureUrl.AbsoluteUri,
            PublicId = result.PublicId
        };

        // If the user has no photos, set the uploaded photo as their main photo
        if (user.Photos.Count == 0) photo.IsMain = true;

        // Add the photo to the user's photos
        user.Photos.Add(photo);

        // If changes were saved, return the CreatedAtAction
        // This fetch the user and their photos
        // Get the values needed to build the URL
        // Map the data to the PhotoDto passing in the photo object
        if (await userRepository.SaveAllAsync()) 
            return CreatedAtAction(nameof(GetUsers), 
                new {username = user.UserName}, mapper.Map<PhotoDto>(photo));

        // Else return a bad request
        return BadRequest("Problem adding photo");
    }

    [HttpPut("set-main-photo/{photoId:int}")]
    public async Task<ActionResult> SetMainPhoto(int photoId)
    {
        // Get the user from the userRepository by getting the user by username of the current user
        var user = await userRepository.GetUserByUserNameAsync(User.GetUsername());

        // If null return a bad request saying could not find user
        if (user == null) return BadRequest("Could not find user");

        // Check to see if the passed in Id matches any photos in the user's photos
        var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

        // If null or photo is already their main photo, return a bad request
        if (photo == null || photo.IsMain) return BadRequest("Cannot use this as main photo");

        // Get the current user's photo by finding the photo set to IsMain (if any)
        var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);

        // If currentMain is not null, set its IsMain property to false        
        if (currentMain != null) currentMain.IsMain = false;
        
        // Set photo IsMain property to true
        photo.IsMain = true;

        // Save the changes in the userRepository
        if (await userRepository.SaveAllAsync()) return NoContent();

        // Return bad request if there was an error
        return BadRequest("Problem setting main photo");
    }

    [HttpDelete("delete-photo/{photoId:int}")]
    public async Task<ActionResult> DeletePhoto(int photoId)
    {
        // Get the user from the userRepository by getting the user by username of the current user
        var user = await userRepository.GetUserByUserNameAsync(User.GetUsername());

        // If null return a bad request saying could not find user
        if (user == null) return BadRequest("User not found");

        // Check to see if the passed in Id matches any photos in the user's photos
        var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

        // If null or photo is already their main photo, return a bad request
        if (photo == null || photo.IsMain) return BadRequest("This photo cannot be deleted");

        // If the PublicId is not null 
        if (photo.PublicId != null)
        {
            // Delete the photo from the user's photos by passing in the photo.PublicId
            var result = await photoService.DeletePhotoAsync(photo.PublicId);

            // Return an error if there was a problem deleting the photo
            if (result.Error != null) return BadRequest(result.Error.Message);
        }

        // Remove the photo from the user's photos
        user.Photos.Remove(photo);

        // Save the changes in the userRepository
        if (await userRepository.SaveAllAsync()) return Ok();

        // Return a bad request if deleting photo did not work
        return BadRequest("Problem deleting photo");
    }
}
