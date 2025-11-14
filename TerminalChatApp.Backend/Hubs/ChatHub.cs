using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TerminalChatApp.Backend.Data;
using TerminalChatApp.Backend.Models;
using TerminalChatApp.Backend.Services;
using System.Security.Claims;

namespace TerminalChatApp.Backend.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ChatContext _context;
    private readonly JwtService _jwtService;
    private static readonly Dictionary<string, string> _userConnections = new();
    private static readonly Dictionary<string, HashSet<string>> _chatGroups = new();

    public ChatHub(ChatContext context, JwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            // Store connection mapping
            _userConnections[userId] = Context.ConnectionId;
            
            // Update user online status
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Join user to their chat groups
            var userChats = await _context.ChatParticipants
                .Where(cp => cp.UserId == userId)
                .Select(cp => cp.ChatId)
                .ToListAsync();

            foreach (var chatId in userChats)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
                
                // Track chat group membership
                if (!_chatGroups.ContainsKey(chatId))
                    _chatGroups[chatId] = new HashSet<string>();
                _chatGroups[chatId].Add(userId);
            }

            // Notify other users of online status
            await NotifyUserOnlineStatus(userId, true);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            // Remove connection mapping
            _userConnections.Remove(userId);

            // Update user offline status
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = false;
                user.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Remove from chat groups
            foreach (var chatGroup in _chatGroups.Values)
            {
                chatGroup.Remove(userId);
            }

            // Notify other users of offline status
            await NotifyUserOnlineStatus(userId, false);
        }

        await base.OnDisconnectedAsync(exception);
    }
    
    // New invitation-related hub methods
    public async Task SendInvitationNotification(string receiverId, object invitationData)
    {
        try
        {
            await Clients.User(receiverId).SendAsync("InvitationReceived", invitationData);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to send invitation notification: {ex.Message}");
        }
    }
    
    public async Task NotifyInvitationResponse(string senderId, string receiverUsername, string response, object invitationData)
    {
        try
        {
            var eventName = response == "Accepted" ? "InvitationAccepted" : "InvitationDeclined";
            await Clients.User(senderId).SendAsync(eventName, receiverUsername, invitationData);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to send invitation response notification: {ex.Message}");
        }
    }
    
    public async Task NotifyNewChatMember(string chatId, string newMemberUsername)
    {
        try
        {
            // Get all participants in the chat
            var participantIds = await _context.ChatParticipants
                .Where(cp => cp.ChatId == chatId)
                .Select(cp => cp.UserId)
                .ToListAsync();
            
            // Notify all participants about the new member
            foreach (var participantId in participantIds)
            {
                await Clients.User(participantId).SendAsync("UserJoinedChat", newMemberUsername, chatId);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to notify new chat member: {ex.Message}");
        }
    }

    public async Task SendMessage(string chatId, string message)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return;

            // Verify user is participant in this chat
            var isParticipant = await _context.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

            if (!isParticipant)
                return;

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return;

            // Create and save message
            var messageEntity = new Message
            {
                ChatId = chatId,
                SenderId = userId,
                Content = message,
                Type = MessageType.Text
            };

            _context.Messages.Add(messageEntity);

            // Update chat last activity
            var chat = await _context.Chats.FindAsync(chatId);
            if (chat != null)
            {
                chat.LastActivity = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Send message to all users in the chat
            await Clients.Group($"chat_{chatId}").SendAsync("ReceiveMessage", 
                user.Username, message, messageEntity.Timestamp);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to send message: {ex.Message}");
        }
    }

    public async Task JoinChat(string chatId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return;

            // Verify user is participant in this chat
            var isParticipant = await _context.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

            if (!isParticipant)
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
            
            // Track chat group membership
            if (!_chatGroups.ContainsKey(chatId))
                _chatGroups[chatId] = new HashSet<string>();
            _chatGroups[chatId].Add(userId);

            // Notify other users in chat
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                await Clients.GroupExcept($"chat_{chatId}", Context.ConnectionId)
                    .SendAsync("UserJoinedChat", user.Username, chatId);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to join chat: {ex.Message}");
        }
    }

    public async Task LeaveChat(string chatId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");
            
            // Remove from chat group tracking
            if (_chatGroups.ContainsKey(chatId))
            {
                _chatGroups[chatId].Remove(userId);
                if (_chatGroups[chatId].Count == 0)
                    _chatGroups.Remove(chatId);
            }

            // Notify other users in chat
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                await Clients.Group($"chat_{chatId}")
                    .SendAsync("UserLeftChat", user.Username, chatId);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to leave chat: {ex.Message}");
        }
    }

    public async Task SetTyping(string chatId, bool isTyping)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return;

            // Verify user is participant in this chat
            var isParticipant = await _context.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

            if (!isParticipant)
                return;

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                // Notify other users in the chat about typing status
                await Clients.GroupExcept($"chat_{chatId}", Context.ConnectionId)
                    .SendAsync("UserTyping", user.Username, isTyping);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to set typing status: {ex.Message}");
        }
    }

    private string GetCurrentUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    private async Task NotifyUserOnlineStatus(string userId, bool isOnline)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return;

            // Get all chats the user is in
            var userChatIds = await _context.ChatParticipants
                .Where(cp => cp.UserId == userId)
                .Select(cp => cp.ChatId)
                .ToListAsync();

            // Notify users in all those chats
            foreach (var chatId in userChatIds)
            {
                await Clients.GroupExcept($"chat_{chatId}", Context.ConnectionId)
                    .SendAsync("UserOnlineStatus", user.Username, isOnline);
            }
        }
        catch
        {
            // Ignore errors in notification
        }
    }
}