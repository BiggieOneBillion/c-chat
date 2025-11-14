using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerminalChatApp.Backend.Data;
using TerminalChatApp.Backend.Models;
using TerminalChatApp.Backend.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace TerminalChatApp.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatsController : ControllerBase
{
    private readonly ChatContext _context;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatsController(ChatContext context, IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
    }

    [HttpGet]
    public async Task<ActionResult<ChatListResponse>> GetUserChats()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var chats = await _context.ChatParticipants
                .Where(cp => cp.UserId == userId)
                .Include(cp => cp.Chat)
                    .ThenInclude(c => c.Participants)
                        .ThenInclude(p => p.User)
                .Include(cp => cp.Chat)
                    .ThenInclude(c => c.Messages.OrderByDescending(m => m.Timestamp).Take(1))
                        .ThenInclude(m => m.Sender)
                .Select(cp => cp.Chat)
                .ToListAsync();

            var chatDtos = chats.Select(chat => new ChatDto
            {
                Id = chat.Id,
                Name = chat.Name,
                Type = chat.Type,
                ParticipantIds = chat.Participants.Select(p => p.UserId).ToList(),
                Participants = chat.Participants.Select(p => new UserDto
                {
                    Id = p.User.Id,
                    Username = p.User.Username,
                    Email = p.User.Email,
                    IsOnline = p.User.IsOnline,
                    LastSeen = p.User.LastSeen
                }).ToList(),
                CreatedAt = chat.CreatedAt,
                LastActivity = chat.LastActivity,
                LastMessage = chat.Messages.FirstOrDefault() != null ? new MessageDto
                {
                    Id = chat.Messages.First().Id,
                    ChatId = chat.Messages.First().ChatId,
                    SenderId = chat.Messages.First().SenderId,
                    SenderUsername = chat.Messages.First().Sender.Username,
                    Content = chat.Messages.First().Content,
                    Timestamp = chat.Messages.First().Timestamp,
                    Type = chat.Messages.First().Type
                } : null
            }).ToList();

            return Ok(new ChatListResponse
            {
                Success = true,
                Chats = chatDtos
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ChatListResponse
            {
                Success = false,
                Message = $"Failed to get chats: {ex.Message}"
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ChatDto>> CreateChat(CreateChatRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null)
                return BadRequest("User not found");

            // Create chat
            var chat = new Chat
            {
                Name = request.Name,
                Type = request.Type
            };

            _context.Chats.Add(chat);

            // Add current user as admin participant
            var adminParticipant = new ChatParticipant
            {
                UserId = currentUserId,
                ChatId = chat.Id,
                IsAdmin = true
            };
            _context.ChatParticipants.Add(adminParticipant);

            // Add other participants
            foreach (var username in request.ParticipantUsernames)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null && user.Id != currentUserId)
                {
                    var participant = new ChatParticipant
                    {
                        UserId = user.Id,
                        ChatId = chat.Id,
                        IsAdmin = false
                    };
                    _context.ChatParticipants.Add(participant);
                }
            }

            await _context.SaveChangesAsync();

            // Return created chat
            var createdChat = await _context.Chats
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == chat.Id);

            if (createdChat == null)
                return StatusCode(500, "Failed to create chat");

            var chatDto = new ChatDto
            {
                Id = createdChat.Id,
                Name = createdChat.Name,
                Type = createdChat.Type,
                ParticipantIds = createdChat.Participants.Select(p => p.UserId).ToList(),
                Participants = createdChat.Participants.Select(p => new UserDto
                {
                    Id = p.User.Id,
                    Username = p.User.Username,
                    Email = p.User.Email,
                    IsOnline = p.User.IsOnline,
                    LastSeen = p.User.LastSeen
                }).ToList(),
                CreatedAt = createdChat.CreatedAt,
                LastActivity = createdChat.LastActivity
            };

            return CreatedAtAction(nameof(GetUserChats), new { id = chat.Id }, chatDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to create chat: {ex.Message}");
        }
    }

    [HttpGet("{chatId}/messages")]
    public async Task<ActionResult<MessagesResponse>> GetChatMessages(string chatId, [FromQuery] int limit = 50)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Check if user is participant in this chat
            var isParticipant = await _context.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

            if (!isParticipant)
                return Forbid("You are not a participant in this chat");

            var messages = await _context.Messages
                .Where(m => m.ChatId == chatId)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToListAsync();

            var messageDtos = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                ChatId = m.ChatId,
                SenderId = m.SenderId,
                SenderUsername = m.Sender.Username,
                Content = m.Content,
                Timestamp = m.Timestamp,
                Type = m.Type
            }).OrderBy(m => m.Timestamp).ToList();

            return Ok(new MessagesResponse
            {
                Success = true,
                Messages = messageDtos
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new MessagesResponse
            {
                Success = false,
                Message = $"Failed to get messages: {ex.Message}"
            });
        }
    }

    [HttpPost("{chatId}/messages")]
    public async Task<ActionResult<MessageDto>> SendMessage(string chatId, [FromBody] SendMessageRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Check if user is participant in this chat
            var isParticipant = await _context.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

            if (!isParticipant)
                return Forbid("You are not a participant in this chat");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return BadRequest("User not found");

            // Create message
            var message = new Message
            {
                ChatId = chatId,
                SenderId = userId,
                Content = request.Content,
                Type = MessageType.Text
            };

            _context.Messages.Add(message);

            // Update chat last activity
            var chat = await _context.Chats.FindAsync(chatId);
            if (chat != null)
            {
                chat.LastActivity = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var messageDto = new MessageDto
            {
                Id = message.Id,
                ChatId = message.ChatId,
                SenderId = message.SenderId,
                SenderUsername = user.Username,
                Content = message.Content,
                Timestamp = message.Timestamp,
                Type = message.Type
            };

            return Ok(messageDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to send message: {ex.Message}");
        }
    }
    
    [HttpGet("users/search")]
    public async Task<ActionResult<UserSearchResponse>> SearchUsers([FromQuery] string query, [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return BadRequest("Search query must be at least 2 characters long");

            var currentUserId = GetCurrentUserId();

            var users = await _context.Users
                .Where(u => u.Id != currentUserId && u.Username.Contains(query))
                .Take(limit)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    IsOnline = u.IsOnline,
                    LastSeen = u.LastSeen
                })
                .ToListAsync();

            return Ok(new UserSearchResponse
            {
                Success = true,
                Users = users
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new UserSearchResponse
            {
                Success = false,
                Message = $"Failed to search users: {ex.Message}"
            });
        }
    }
    
    [HttpPost("create-group")]
    public async Task<ActionResult<ChatDto>> CreateGroupChatWithInvitations(CreateGroupChatRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null)
                return BadRequest("User not found");

            // Create the group chat
            var chat = new Chat
            {
                Name = request.Name,
                Type = ChatType.Group
            };

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            // Add current user as admin
            var adminParticipant = new ChatParticipant
            {
                UserId = currentUserId,
                ChatId = chat.Id,
                IsAdmin = true
            };
            _context.ChatParticipants.Add(adminParticipant);
            await _context.SaveChangesAsync();

            // Send invitations to other users
            var invitations = new List<ChatInvitation>();
            foreach (var username in request.MemberUsernames)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null && user.Id != currentUserId)
                {
                    var invitation = new ChatInvitation
                    {
                        SenderId = currentUserId,
                        ReceiverId = user.Id,
                        ChatId = chat.Id,
                        Type = InvitationType.GroupChat,
                        Message = request.InvitationMessage ?? $"{currentUser.Username} invited you to join '{chat.Name}'",
                        ChatName = chat.Name,
                        ExpiresAt = DateTime.UtcNow.AddDays(7)
                    };
                    
                    invitations.Add(invitation);
                }
            }

            if (invitations.Any())
            {
                _context.ChatInvitations.AddRange(invitations);
                await _context.SaveChangesAsync();

                // Send real-time notifications to all invited users
                foreach (var inv in invitations)
                {
                    var invitationDto = new InvitationDto
                    {
                        Id = inv.Id,
                        SenderId = inv.SenderId,
                        SenderUsername = currentUser.Username,
                        ReceiverId = inv.ReceiverId,
                        ChatId = inv.ChatId,
                        ChatName = inv.ChatName,
                        Type = inv.Type,
                        Status = inv.Status,
                        Message = inv.Message,
                        CreatedAt = inv.CreatedAt,
                        ExpiresAt = inv.ExpiresAt
                    };
                    
                    await _hubContext.Clients.User(inv.ReceiverId).SendAsync("InvitationReceived", invitationDto);
                    await _hubContext.Clients.User(inv.ReceiverId).SendAsync("ShowNotification", 
                        $"🎉 {currentUser.Username} invited you to join '{chat.Name}'!");
                }
            }

            // Return the created chat
            var createdChat = await _context.Chats
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == chat.Id);

            var chatDto = new ChatDto
            {
                Id = createdChat!.Id,
                Name = createdChat.Name,
                Type = createdChat.Type,
                ParticipantIds = createdChat.Participants.Select(p => p.UserId).ToList(),
                Participants = createdChat.Participants.Select(p => new UserDto
                {
                    Id = p.User.Id,
                    Username = p.User.Username,
                    Email = p.User.Email,
                    IsOnline = p.User.IsOnline,
                    LastSeen = p.User.LastSeen
                }).ToList(),
                CreatedAt = createdChat.CreatedAt,
                LastActivity = createdChat.LastActivity
            };

            return CreatedAtAction(nameof(GetUserChats), chatDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to create group chat: {ex.Message}");
        }
    }
}

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
}