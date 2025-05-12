using System;
using API.Entities;
using API.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

// Extending the IdentityDbContext giving all of ASP.NET Core Identity's database tables.
// Cusomizing it to use my own AppUser and AppRole entities with int as primary key type.
public class DataContext(DbContextOptions options) : IdentityDbContext<AppUser, 
    AppRole, int, IdentityUserClaim<int>, AppUserRole, IdentityUserLogin<int>, 
    IdentityRoleClaim<int>, IdentityUserToken<int>>(options)
{
    // public DbSet<AppUser> Users { get; set; }

    // Custom entities. EF Core will create tables for these and track them
    public DbSet<UserLike> Likes { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Configuring the relationships between entities
        base.OnModelCreating(builder);

        // One AppUser can have many roles via UserRoles
        // Each AppUserRole links to one user via UserId
        // Sets up the many-to-many relationship between users and roles using AppUserRole as the join entity
        builder.Entity<AppUser>()
            .HasMany(ur => ur.UserRoles)
            .WithOne(u => u.User)
            .HasForeignKey(ur => ur.UserId)
            .IsRequired();

        // One AppRole can have many users via UserRoles
        // Each AppUserRole links to one role via RoleId
        // Completes the other half of the many-to-many setup for users and roles
          builder.Entity<AppRole>()
            .HasMany(ur => ur.UserRoles)
            .WithOne(u => u.Role)
            .HasForeignKey(ur => ur.RoleId)
            .IsRequired();

        // Creates a composite key using both user IDs
        // Ensures a user can like another only once
        builder.Entity<UserLike>()
            .HasKey(k => new {k.SourceUserId, k.TargetUserId});

        // SourceUser is the user who liked someone
        // LikedUser is the list of users they liked
        // If the source user is deleted, their likes are also deleted
        builder.Entity<UserLike>()
            .HasOne(s => s.SourceUser)
            .WithMany(l => l.LikedUsers)
            .HasForeignKey(s => s.SourceUserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // TargetUser is the user who was liked
        // LikedByUsers is the list of users who liked them
        // If the target user is deleted, their "liked-by" entries are also deleted
        builder.Entity<UserLike>()
            .HasOne(s => s.TargetUser)
            .WithMany(l => l.LikedByUsers)
            .HasForeignKey(s => s.TargetUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Each message has one recipient
        // A user can recieve many messages
        // If the recipient user is deleted, don't delete messages
        builder.Entity<Message>()
            .HasOne(x => x.Recipient)
            .WithMany(x => x.MessagesReceived)
            .OnDelete(DeleteBehavior.Restrict);

        // Each message has one sender
        // A user can send many messages
        // If a sender is deleted, don't delete messages
        // Prevents cascade deletes from wiping out large amounts of messages unintentionally
        builder.Entity<Message>()
            .HasOne(x => x.Sender)
            .WithMany(x => x.MessagesSent)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
