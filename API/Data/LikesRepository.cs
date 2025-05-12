using System;
using API.DTOs;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class LikesRepository(DataContext context, IMapper mapper) : ILikesRepository
{
    public void AddLike(UserLike like)
    {
        // Add a like to the DbSet<UserLike> table 
        context.Likes.Add(like);
    }

    public void DeleteLike(UserLike like)
    {
        // Remove a like to the DbSet<UserLike> table 
        context.Likes.Remove(like);
    }

    public async Task<IEnumerable<int>> GetCurrentUserLikeIds(int currentUserId)
    {
        // Return the list of who the current user liked
        return await context.Likes
            .Where(x => x.SourceUserId == currentUserId)
            .Select(x => x.TargetUserId)
            .ToListAsync();
    }

    public async Task<UserLike?> GetUserLike(int sourceUserId, int targetUserId)
    {
        // Retrieves a single UserLike record where the Id of the user who iniated the like and the Id of the user who was liked
        return await context.Likes.FindAsync(sourceUserId, targetUserId);
    }

    // Returns a paginiated list of users based on liked condition
    public async Task<PagedList<MemberDto>> GetUserLikes(LikesParams likesParams)
    {
        // Building the query to be executed later
        var likes = context.Likes.AsQueryable();

        // Declare the variable that will hold the final query that will return a MemberDto
        IQueryable<MemberDto> query;


        // Based on the predicate in the likesParam from the url
        switch (likesParams.Predicate)
        {
            case "liked":
            // Filter likes where the current user is the source (liker)
                query = likes
                    .Where(x => x.SourceUserId == likesParams.UserId)
                // From those likes, select the user who were liked (the targets)
                    .Select(x => x.TargetUser)
                // Convert each target user to a MemberDto using AutoMapper
                    .ProjectTo<MemberDto>(mapper.ConfigurationProvider);
                break;

            // Filter likes where current user is the target
            case "likedBy":
                 query = likes
                    .Where(x => x.TargetUserId == likesParams.UserId)
                // Select the users who liked the current user
                    .Select(x => x.SourceUser)
                // Convert those SourceUsers into MemberDto Objects
                    .ProjectTo<MemberDto>(mapper.ConfigurationProvider);
                break;
            default:
                // returns list of current user's like ids
                var likeIds = await GetCurrentUserLikeIds(likesParams.UserId);

                // Sees if likedIds list contains the SourceUserId
                // For each one that it does contain, select ane return that SourceUser
                // But only if the targetUserId matches the userId giving us mutal likes
                query = likes
                    .Where(x => x.TargetUserId == likesParams.UserId && likeIds.Contains(x.SourceUserId))
                    .Select(x => x.SourceUser)
                    .ProjectTo<MemberDto>(mapper.ConfigurationProvider);
                break;
        }
        return await PagedList<MemberDto>.CreatAsync(query, likesParams.pageNumber, likesParams.PageSize);
    }

    public async Task<bool> SaveChanges()
    {
        return await context.SaveChangesAsync() > 0;
    }
}
