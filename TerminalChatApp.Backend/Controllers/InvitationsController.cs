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
public class InvitationsController : ControllerBase
{
    private readonly ChatContext _context;
    private readonly IHubContext<ChatHub> _hubContext;

    public InvitationsController(ChatContext context, IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
    }

    [HttpPost("send")]
    public async Task<ActionResult<InvitationDto>> SendInvitation(SendInvitationRequest request)
    {
        try
        {
            var senderId = GetCurrentUserId();
            var sender = await _context.Users.FindAsync(senderId);
            if (sender == null) return BadRequest("Sender not found");

            // Find receiver by username
            var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.ReceiverUsername);
            if (receiver == null) return BadRequest("User not found");

            if (receiver.Id == senderId) return BadRequest("Cannot invite yourself");

            // Check for existing invitation
            var existingInvitation = await _context.ChatInvitations
                .FirstOrDefaultAsync(i => i.SenderId == senderId && i.ReceiverId == receiver.Id 
                    && i.Status == InvitationStatus.Pending && i.Type == request.Type);

            if (existingInvitation != null)
                return BadRequest("Invitation already sent to this user");

            // For DirectMessage, check if chat already exists
            if (request.Type == InvitationType.DirectMessage)
            {
                var existingDM = await _context.ChatParticipants
                    .Where(cp => cp.UserId == senderId || cp.UserId == receiver.Id)
                    .Include(cp => cp.Chat)
                    .Where(cp => cp.Chat.Type == ChatType.Private)
                    .GroupBy(cp => cp.ChatId)
                    .Where(g => g.Count() == 2)
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync();

                if (existingDM != null)
                    return BadRequest("Direct message chat already exists with this user");
            }

            // Create invitation
            var invitation = new ChatInvitation
            {
                SenderId = senderId,
                ReceiverId = receiver.Id,
                ChatId = request.ChatId,
                Type = request.Type,
                Message = request.Message,
                ChatName = request.ChatName,
                ExpiresAt = DateTime.UtcNow.AddDays(7) // Expire in 7 days
            };

            _context.ChatInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            // Create DTO for response
            var invitationDto = new InvitationDto
            {
                Id = invitation.Id,
                SenderId = sender.Id,
                SenderUsername = sender.Username,
                ReceiverId = receiver.Id,
                ReceiverUsername = receiver.Username,
                ChatId = invitation.ChatId,
                ChatName = invitation.ChatName,
                Type = invitation.Type,
                Status = invitation.Status,
                Message = invitation.Message,
                CreatedAt = invitation.CreatedAt,
                ExpiresAt = invitation.ExpiresAt
            };

            // Send real-time notification to receiver
            await _hubContext.Clients.User(receiver.Id).SendAsync("InvitationReceived", invitationDto);
            
            // Also send a general notification with sound/visual cue
            var notificationMessage = invitation.Type == InvitationType.GroupChat 
                ? $"New group chat invitation: {invitation.ChatName}"
                : $"New direct message invitation from {sender.Username}";
            
            await _hubContext.Clients.User(receiver.Id).SendAsync("ShowNotification", notificationMessage);

            return Ok(invitationDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to send invitation: {ex.Message}");
        }
    }

    [HttpGet("received")]
    public async Task<ActionResult<InvitationListResponse>> GetReceivedInvitations([FromQuery] InvitationStatus? status = null)
    {
        try
        {
            var userId = GetCurrentUserId();

            var query = _context.ChatInvitations
                .Include(i => i.Sender)
                .Include(i => i.Receiver)
                .Include(i => i.Chat)
                .Where(i => i.ReceiverId == userId);

            if (status.HasValue)
                query = query.Where(i => i.Status == status.Value);

            var invitations = await query
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var invitationDtos = invitations.Select(i => new InvitationDto
            {
                Id = i.Id,
                SenderId = i.SenderId,
                SenderUsername = i.Sender.Username,
                ReceiverId = i.ReceiverId,
                ReceiverUsername = i.Receiver.Username,
                ChatId = i.ChatId,
                ChatName = i.ChatName ?? i.Chat?.Name,
                Type = i.Type,
                Status = i.Status,
                Message = i.Message,
                CreatedAt = i.CreatedAt,
                RespondedAt = i.RespondedAt,
                ExpiresAt = i.ExpiresAt
            }).ToList();

            return Ok(new InvitationListResponse
            {
                Success = true,
                Invitations = invitationDtos
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new InvitationListResponse
            {
                Success = false,
                Message = $"Failed to get invitations: {ex.Message}"
            });
        }
    }

    [HttpGet("sent")]
    public async Task<ActionResult<InvitationListResponse>> GetSentInvitations([FromQuery] InvitationStatus? status = null)
    {
        try
        {
            var userId = GetCurrentUserId();

            var query = _context.ChatInvitations
                .Include(i => i.Sender)
                .Include(i => i.Receiver)
                .Include(i => i.Chat)
                .Where(i => i.SenderId == userId);

            if (status.HasValue)
                query = query.Where(i => i.Status == status.Value);

            var invitations = await query
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var invitationDtos = invitations.Select(i => new InvitationDto
            {
                Id = i.Id,
                SenderId = i.SenderId,
                SenderUsername = i.Sender.Username,
                ReceiverId = i.ReceiverId,
                ReceiverUsername = i.Receiver.Username,
                ChatId = i.ChatId,
                ChatName = i.ChatName ?? i.Chat?.Name,
                Type = i.Type,
                Status = i.Status,
                Message = i.Message,
                CreatedAt = i.CreatedAt,
                RespondedAt = i.RespondedAt,
                ExpiresAt = i.ExpiresAt
            }).ToList();

            return Ok(new InvitationListResponse
            {
                Success = true,
                Invitations = invitationDtos
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new InvitationListResponse
            {
                Success = false,
                Message = $"Failed to get sent invitations: {ex.Message}"
            });
        }
    }

    [HttpPut("{id}/respond")]
    public async Task<ActionResult<InvitationDto>> RespondToInvitation(string id, RespondToInvitationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            var invitation = await _context.ChatInvitations
                .Include(i => i.Sender)
                .Include(i => i.Receiver)
                .Include(i => i.Chat)
                .FirstOrDefaultAsync(i => i.Id == id && i.ReceiverId == userId);

            if (invitation == null)
                return NotFound("Invitation not found");

            if (invitation.Status != InvitationStatus.Pending)
                return BadRequest("Invitation already responded to");

            if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt < DateTime.UtcNow)
                return BadRequest("Invitation has expired");

            // Update invitation status
            invitation.Status = request.Response;
            invitation.RespondedAt = DateTime.UtcNow;

            Chat? chat = null;

            if (request.Response == InvitationStatus.Accepted)
            {
                // Create or join chat
                if (invitation.Type == InvitationType.DirectMessage)
                {
                    chat = await CreateDirectMessageChat(invitation);
                }
                else if (invitation.ChatId != null)
                {
                    chat = await JoinExistingGroupChat(invitation);
                }
                else
                {
                    chat = await CreateGroupChat(invitation);
                }

                invitation.ChatId = chat?.Id;
            }

            await _context.SaveChangesAsync();

            // Create response DTO
            var invitationDto = new InvitationDto
            {
                Id = invitation.Id,
                SenderId = invitation.SenderId,
                SenderUsername = invitation.Sender.Username,
                ReceiverId = invitation.ReceiverId,
                ReceiverUsername = invitation.Receiver.Username,
                ChatId = invitation.ChatId,
                ChatName = invitation.ChatName ?? invitation.Chat?.Name,
                Type = invitation.Type,
                Status = invitation.Status,
                Message = invitation.Message,
                CreatedAt = invitation.CreatedAt,
                RespondedAt = invitation.RespondedAt,
                ExpiresAt = invitation.ExpiresAt
            };

            // Send real-time notification to sender
            var responseType = request.Response == InvitationStatus.Accepted ? "InvitationAccepted" : "InvitationDeclined";
            await _hubContext.Clients.User(invitation.SenderId).SendAsync(responseType, invitationDto);
            
            // Send user-friendly notification
            var responseMessage = request.Response == InvitationStatus.Accepted 
                ? $"🎉 {invitation.Receiver.Username} accepted your invitation!"
                : $"❌ {invitation.Receiver.Username} declined your invitation";
            
            await _hubContext.Clients.User(invitation.SenderId).SendAsync("ShowNotification", responseMessage);

            if (chat != null && request.Response == InvitationStatus.Accepted)
            {
                // Notify all chat participants about new member
                var participantIds = await _context.ChatParticipants
                    .Where(cp => cp.ChatId == chat.Id)
                    .Select(cp => cp.UserId)
                    .ToListAsync();

                foreach (var participantId in participantIds)
                {
                    await _hubContext.Clients.User(participantId).SendAsync("UserJoinedChat", invitation.Receiver.Username, chat.Id);
                }
            }

            return Ok(invitationDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to respond to invitation: {ex.Message}");
        }
    }

    private async Task<Chat> CreateDirectMessageChat(ChatInvitation invitation)
    {
        var chat = new Chat
        {
            Name = $"{invitation.Sender.Username}, {invitation.Receiver.Username}",
            Type = ChatType.Private
        };

        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();

        // Add both users as participants
        var participants = new[]
        {
            new ChatParticipant { UserId = invitation.SenderId, ChatId = chat.Id, IsAdmin = false },
            new ChatParticipant { UserId = invitation.ReceiverId, ChatId = chat.Id, IsAdmin = false }
        };

        _context.ChatParticipants.AddRange(participants);
        await _context.SaveChangesAsync();

        return chat;
    }

    private async Task<Chat?> JoinExistingGroupChat(ChatInvitation invitation)
    {
        var chat = await _context.Chats.FindAsync(invitation.ChatId);
        if (chat == null) return null;

        // Check if user is already a participant
        var existingParticipant = await _context.ChatParticipants
            .FirstOrDefaultAsync(cp => cp.ChatId == chat.Id && cp.UserId == invitation.ReceiverId);

        if (existingParticipant == null)
        {
            var participant = new ChatParticipant
            {
                UserId = invitation.ReceiverId,
                ChatId = chat.Id,
                IsAdmin = false
            };

            _context.ChatParticipants.Add(participant);
        }

        return chat;
    }

    private async Task<Chat> CreateGroupChat(ChatInvitation invitation)
    {
        var chat = new Chat
        {
            Name = invitation.ChatName ?? "New Group Chat",
            Type = ChatType.Group
        };

        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();

        // Add sender as admin and receiver as member
        var participants = new[]
        {
            new ChatParticipant { UserId = invitation.SenderId, ChatId = chat.Id, IsAdmin = true },
            new ChatParticipant { UserId = invitation.ReceiverId, ChatId = chat.Id, IsAdmin = false }
        };

        _context.ChatParticipants.AddRange(participants);
        await _context.SaveChangesAsync();

        return chat;
    }
}