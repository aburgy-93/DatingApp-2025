using System;

namespace API.SignalR;

public class PresenceTracker
{
    private static readonly Dictionary<string, List<string>> OnlineUsers = [];

    public Task UserConnected(string username, string connectionId)
    {
        // Locking dictionary while updating it, nothing can update when being updated
        lock (OnlineUsers) 
        {
            // If OnlineUsers dictionary contains the username, add the connectionId
            if (OnlineUsers.ContainsKey(username))
            {
                OnlineUsers[username].Add(connectionId);
            }
            // If OnlineUserd dictionary does not contain username, add username and connectionId
            else
            {
                OnlineUsers.Add(username, [connectionId]);
            }
        }

        return Task.CompletedTask;
    }

    public Task UserDisconnected(string username, string connectionId) 
    {
        // Locking dictionary while updating it, nothing can update when being updated
        lock (OnlineUsers)

        {
            // If dictionary does not contain the username, return the completed task
            if (!OnlineUsers.ContainsKey(username)) return Task.CompletedTask;

            // If they are in the dictionary, remove the connectionId
            OnlineUsers[username].Remove(connectionId);

            // If OnlineUsers with the key of username is == 0, reomve the username key from dictionary
            if (OnlineUsers[username].Count == 0)
            {
                OnlineUsers.Remove(username);
            }
        }

        return Task.CompletedTask;
    }

    public Task<string[]> GetOnlineUsers()
    {
        string[] onlineUsers;
        // Locking dictionary while updating it, nothing can update when being updated
        lock (OnlineUsers)
        {
            // Create a list of usernames of users online
            onlineUsers = OnlineUsers.OrderBy(k => k.Key).Select(k => k.Key).ToArray();
        }

        // Return the list of onlineUsers
        return Task.FromResult(onlineUsers);
    }
}
