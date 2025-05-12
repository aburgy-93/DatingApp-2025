using System;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class MessageRepository(DataContext context, IMapper mapper) : IMessageRepository
{
    // Add a message to the context (DB)
    public void AddMessage(Message message)
    {
        context.Messages.Add(message);
    }

    // Delete a message from the context (DB)
    public void DeleteMessage(Message message)
    {
        context.Messages.Remove(message);
    }

    // Get a message from the context by id
    public async Task<Message?> GetMessage(int id)
    {
        return await context.Messages.FindAsync(id);
    }

    // Return a list of MessageDtos for a user
    public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
    {
        // set up the query, get the messages sent by current user in descending order
        var query = context.Messages
            .OrderByDescending(x => x.MessageSent)
            .AsQueryable();

        // Set up the query based on the messageParams sent
        query = messageParams.Container switch
        {
            // Show messages where the recipeint usernmae is equal to the username in the message params and the recipient hasn't deleted it
            "Inbox" => query.Where(x => x.Recipient.UserName == messageParams.Username 
                && x.RecipientDeleted == false),
            
            // Show messages where the sender unsername is equal to the username in the message params and the sender hasn't deleted it
            "Outbox" => query.Where(x => x.Sender.UserName == messageParams.Username 
                && x.SenderDeleted == false),
            
            // Default case, show messages where the recipient name is equal to the message params username and the dateRead is null
            // and the recipient hasn't deleted it
            _ => query.Where(x => x.Recipient.UserName == messageParams.Username 
                && x.DateRead == null && x.RecipientDeleted == false)
        };

        // Project the returned messages to a MessageDto using the mapper
        var messages = query.ProjectTo<MessageDto>(mapper.ConfigurationProvider);

        // Return a list of messages with the messages, selected page number and page size based on the params in the query
        return await PagedList<MessageDto>
            .CreatAsync(messages, messageParams.pageNumber, messageParams.PageSize);
    }

    // Get a message thread between two users
    public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername)
    {
        // Step 1) Getting the Sender and Recipient users and their photos 
        var messages = await context.Messages
            .Include(x => x.Sender).ThenInclude(x => x.Photos)
            .Include(x => x.Recipient).ThenInclude(x => x.Photos)
            .Where(x => 
            // Include the messages between two users (current user and recipient) that have not been deleted
                x.RecipientUsername == currentUsername 
                    && x.RecipientDeleted == false 
                    && x.SenderUsername == recipientUsername ||
                x.SenderUsername == currentUsername 
                    && x.SenderDeleted == false 
                    && x.RecipientUsername == recipientUsername
            )
            // Order by message sent and put them into a list
            .OrderBy(x => x.MessageSent)
            .ToListAsync();

        // Check for unread messages
        var unreadMessages = messages.Where(x => x.DateRead == null && 
            x.RecipientUsername == currentUsername).ToList();

        // If there are, set DateRead
        if (unreadMessages.Count != 0) 
        {
            unreadMessages.ForEach(x => x.DateRead = DateTime.UtcNow);
            await context.SaveChangesAsync();
        }

        // Return messages
        return mapper.Map<IEnumerable<MessageDto>>(messages);
    }

    // Save the changes in the context if there are any
    public async Task<bool> SaveAllAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }
}
