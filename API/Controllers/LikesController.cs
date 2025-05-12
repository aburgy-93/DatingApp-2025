using System;
using API.DTOs;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class LikesController(ILikesRepository likesRepository) : BaseApiController
{
    [HttpPost("{targetUserId:int}")]
    // Toggles a user adding a like on another user
    // Takes in the id of the target user being liked
    public async Task<ActionResult> ToggleLike(int targetUserId)
    {
        // Get the id of the user being liked
        var sourceUserId = User.GetUserId();

        // Check to make sure you cannot like yourself (absolutely nothing wrong with liking yourself though!)
        if(sourceUserId == targetUserId) return BadRequest("You cannot like yourself");

        // Check the likesRepository to see if current user has already liked target user
        var existingLike = await likesRepository.GetUserLike(sourceUserId, targetUserId);

        // If return null, then create a new UserLike with the currentUserId(sourceUserId) and targetUserId 
        if (existingLike == null) 
        {
            var like = new UserLike
            {
                SourceUserId = sourceUserId,
                TargetUserId = targetUserId
            };

            // Add the like to the likesRepository
            likesRepository.AddLike(like);
        }
        else 
        {
            // Otherwise, delete the existing like from the likesRepository
            likesRepository.DeleteLike(existingLike);
        }

        // If changes were made to the likesRepository, save the changes, return ok
        if (await likesRepository.SaveChanges()) return Ok();

        // If changes could not be saved, return Bad Request
        return BadRequest("Failed to update like");
    }

    [HttpGet("list")]
    // Returns a list of the current user's likes
    public async Task<ActionResult<IEnumerable<int>>> GetCurrentUserLikeIds()
    {
        return Ok(await likesRepository.GetCurrentUserLikeIds(User.GetUserId()));
    }

    [HttpGet]
    // Based on a query string predicate we can return an array of MemberDtos
    // Ex: return a list of MemberDtos of who the current user likes, who like the current user, or mutual likes
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUserLikes([FromQuery]LikesParams likesParams)
    {
        // On the likesParams set the UserId as the current user's ID
        likesParams.UserId = User.GetUserId();

        // Get a list of users based on the preticate from the query string
        var users = await likesRepository.GetUserLikes(likesParams);

        // Add pagination to the list of passed in users and return the users
        Response.AddPaginationHeader(users);
        return Ok(users);
    }
}
