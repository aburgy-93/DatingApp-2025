using System;
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
public class MessagesController(IMessageRepository messageRepository, 
    IUserRepository userRepository, IMapper mapper) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
    {
        // Gets the user from the System.Security.Claims.ClaimsPrincipal for user associated with the executing action
        var username = User.GetUsername();

        // Prevents user sending themselves a message by checking that the username
        // of the current user is not the same as the RecipientUsername in the
        // createMessageDto
        if (username == createMessageDto.RecipientUsername.ToLower()) 
            return BadRequest("You cannot message yourself");

        // From the userRepository get the username of the sender by passing in the username
        var sender = await userRepository.GetUserByUserNameAsync(username);

        // From the userRepository get the username of the recipient from the createMessageDto.RecipientUsername
        var recipient = await userRepository.GetUserByUserNameAsync(createMessageDto.RecipientUsername);

        // If recipient is null or sender is null or sender username is null or recipient username is null, return a bad request
        if (recipient == null || sender == null || sender.UserName == null || 
            recipient.UserName == null) return BadRequest("Cannot send message at this time");

        // Create the message with the data
        var message = new Message
        {
            Sender = sender,
            Recipient = recipient,
            SenderUsername = sender.UserName,
            RecipientUsername = recipient.UserName,
            Content = createMessageDto.Content
        };

        // From the messageRepository call the AddMessage method, passing in the message
        messageRepository.AddMessage(message);

        // Save the message and return ok with the message mapped to the MessageDto
        if (await messageRepository.SaveAllAsync()) return Ok(mapper.Map<MessageDto>(message));
        
        // Else return a bad reqeust
        return BadRequest("Failed to save message");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser(
        [FromQuery]MessageParams messageParams)
        // Depending on the params from the query we can go to differnt inboxes
    {
        // Gets the user from the System.Security.Claims.ClaimsPrincipal for user associated with the executing action
        // Set the messageParams. username to the username of the current user
        messageParams.Username = User.GetUsername();

        // For the current user get the messages for the user, pass in the params
        // The params can go to different mailboxes like Outbox/Inbox
        var messages = await messageRepository.GetMessagesForUser(messageParams);

        // Add pagination to the returned list of messages.
        Response.AddPaginationHeader(messages);

        // return the messages
        return messages;
    }

    [HttpGet("thread/{username}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMeassageThread(string username)
    {
        // Gets the user from the System.Security.Claims.ClaimsPrincipal for user associated with the executing action
        var currentUsername = User.GetUsername();

        // Returns an array of messages between the current user and the recipient whoes username we pass in
        return Ok(await messageRepository.GetMessageThread(currentUsername, username));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMessage(int id) {
        // Gets the user from the System.Security.Claims.ClaimsPrincipal for user associated with the executing action
        var username = User.GetUsername();

        // Get the message by id from the messageRepository
        var message = await messageRepository.GetMessage(id);

        // If the message is null, return a bad request
        if (message == null) return BadRequest("Cannot delete this message");

        // Check to see if the senderUsername of the message does not equal the username of the current user
        // And make sure the recipeintUsername of the message does not equal the username of the current user
        // Checks to make sure that the current user is either the sender or the recipient of the message.
        // Ensures that only users involved in the message can access or manipulate it. 
        if (message.SenderUsername != username && 
            message.RecipientUsername != username) return Forbid();

        // Soft-delete the message for the sender, if the current user is the sender
        if (message.SenderUsername == username) message.SenderDeleted = true;

        // Soft-delete the message for the recipient, if the current user is the recipient
        if (message.RecipientUsername == username) message.RecipientDeleted = true;

        // If both sender and recipient delete the message, delete the message entierly from the messageRepository
        if (message is {SenderDeleted: true, RecipientDeleted: true}) {
            messageRepository.DeleteMessage(message);
        }

        // Save the changes in the messageRepository, return Ok
        if (await messageRepository.SaveAllAsync()) return Ok();

        // Else return a BadRequest
        return BadRequest("Problem deleting the message");
    }
}
