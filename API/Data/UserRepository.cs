using System;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class UserRepository(DataContext context, IMapper mapper) : IUserRepository
{
    // Get a member from the context
    public async Task<MemberDto?> GetMemberAsnyc(string username)
    {
        // Return a user from the context where x.UserName is equal to the passed in username
        return await context.Users
            .Where(x=> x.UserName == username)
            .ProjectTo<MemberDto>(mapper.ConfigurationProvider)
            .SingleOrDefaultAsync();
    }

    // Return a list of MemberDtos
    public async Task<PagedList<MemberDto>> GetMembersAsync(UserParams userParams)
    {
        // Set up the query
        var query = context.Users.AsQueryable();

        // return the users that are not the current user
        query = query.Where(x => x.UserName != userParams.CurrentUsername);

        // return the users with a gender specified
        if(userParams.Gender != null) {
            query = query.Where(x => x.Gender == userParams.Gender);
        }

        // Setting up the min and max age
        var minDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-userParams.MaxAge-1));
        var maxDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-userParams.MinAge));

        // Retruning users who's DOB is greater or == to the minDob and less than or == to maxDob
        query = query.Where(x => x.DateOfBirth >= minDob && x.DateOfBirth <= maxDob);

        // Order the users either by created date or when last active
        query = userParams.OrderBy switch
        {
            "created" => query.OrderByDescending(x => x.Created),
            _ => query.OrderByDescending(x => x.LastActive)
        };

        // Return a paged list of MemberDtos 
        return await PagedList<MemberDto>.CreatAsync(query.ProjectTo<MemberDto>
            (mapper.ConfigurationProvider), userParams.pageNumber, userParams.PageSize);
            
    }

    // Return a user by id
    public async Task<AppUser?> GetUserByIdAsync(int id)
    {
        return await context.Users.FindAsync(id);
    }

    // Get a user by username
    public async Task<AppUser?> GetUserByUserNameAsync(string username)
    {
        // When returning the user, include their photos
       return await context.Users
       .Include(x => x.Photos)
       .SingleOrDefaultAsync(x => x.UserName == username);
    }

    // Return a collection of AppUser
    public async Task<IEnumerable<AppUser>> GetUsersAsync()
    {
        // Return the users with their photos
        return await context.Users
        .Include(x => x.Photos)
        .ToListAsync();
    }

    // If any changes were made to the context, save all
    public async Task<bool> SaveAllAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }

    // Update a user
    public void Update(AppUser user)
    {
        // Update the user
        context.Entry(user).State = EntityState.Modified;
    }
}
