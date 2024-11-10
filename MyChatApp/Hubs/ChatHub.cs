using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using MyChatApp.Services;
using MyChatApp.Data;
using Microsoft.Extensions.Logging;

namespace MyChatApp.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IGroupService _groupService;
        private readonly ChatDbContext _context;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IMessageService messageService, IGroupService groupService, ChatDbContext context, ILogger<ChatHub> logger)
        {
            _messageService = messageService;
            _groupService = groupService;
            _context = context;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"User {Context.UserIdentifier} connected with connection ID: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"User {Context.UserIdentifier} disconnected.");
            await base.OnDisconnectedAsync(exception);
        }

        // Send a private message
        public async Task SendMessageToUser(string recipientId, string messageContent, string? fileUrl = null)
        {
            try
            {
                var message = await _messageService.SendMessageToUser(Context.UserIdentifier, recipientId, messageContent, fileUrl);
                await Clients.User(recipientId).SendAsync("ReceiveMessage", message);
                await Clients.Caller.SendAsync("MessageSent", message); // Confirmation to sender
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to user.");
                await Clients.Caller.SendAsync("Error", "Failed to send message.");
            }
        }

        // Send a group message
        public async Task SendMessageToGroup(Guid groupId, string messageContent, string? fileUrl = null)
        {
            try
            {
                var message = await _messageService.SendMessageToGroup(Context.UserIdentifier, groupId, messageContent, fileUrl);
                await Clients.Group(groupId.ToString()).SendAsync("ReceiveGroupMessage", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to group.");
                await Clients.Caller.SendAsync("Error", "Failed to send message to group.");
            }
        }

        // Add current user to a group
        public async Task AddUserToGroup(Guid groupId)
        {
            var userId = Context.UserIdentifier;
            var success = await _groupService.AddUserToGroup(userId, groupId);
            if (success)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
                await Clients.Group(groupId.ToString()).SendAsync("UserJoinedTheGroup", userId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Could not add the user to the group.");
            }
        }

        // Remove current user from a group
        public async Task RemoveUserFromGroup(Guid groupId)
        {
            var userId = Context.UserIdentifier;
            var success = await _groupService.RemoveUserFromGroup(userId, groupId);
            if (success)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId.ToString());
                await Clients.Group(groupId.ToString()).SendAsync("UserLeftTheGroup", userId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Could not remove the user from the group.");
            }
        }

        // Admin adds a user to a group
        public async Task AdminAddUserToGroup(Guid groupId, string userId)
        {
            try
            {
                var success = await _groupService.AddUserToGroup(userId, groupId);
                if (success)
                {
                    await Clients.Group(groupId.ToString()).SendAsync("UserAdded", userId);
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Could not add the user to the group.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user to group.");
                await Clients.Caller.SendAsync("Error", "Could not add the user.");
            }
        }

        // Admin removes a user from the group
        public async Task AdminRemoveUserFromGroup(Guid groupId, string userId)
        {
            try
            {
                var success = await _groupService.RemoveUserFromGroup(userId, groupId);
                if (success)
                {
                    await Clients.Group(groupId.ToString()).SendAsync("UserRemovedByAdmin", userId);
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Could not remove the user from the group.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user from group.");
                await Clients.Caller.SendAsync("Error", "Could not remove the user.");
            }
        }

        // Delete a message and notify group members of deletion
        public async Task DeleteMessage(Guid messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
            {
                await Clients.Caller.SendAsync("Error", "Message not found.");
                return;
            }

            try
            {
                var success = await _messageService.DeleteMessage(Context.UserIdentifier, messageId);
                if (success)
                {
                    if (message.GroupId.HasValue)
                    {
                        await Clients.Group(message.GroupId.ToString()).SendAsync("MessageDeleted", messageId);
                    }
                    else
                    {
                        await Clients.User(message.RecipientId).SendAsync("MessageDeleted", messageId);
                    }
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Could not delete the message.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message.");
                await Clients.Caller.SendAsync("Error", "Failed to delete message.");
            }
        }
    }
}
