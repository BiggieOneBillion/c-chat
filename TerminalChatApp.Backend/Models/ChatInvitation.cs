using System.ComponentModel.DataAnnotations;

namespace TerminalChatApp.Backend.Models;

public class ChatInvitation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string SenderId { get; set; } = string.Empty;
    
    [Required]
    public string ReceiverId { get; set; } = string.Empty;
    
    public string? ChatId { get; set; } // Null for new chats to be created
    
    [Required]
    public InvitationType Type { get; set; }
    
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    
    [StringLength(500)]
    public string? Message { get; set; }
    
    [StringLength(100)]
    public string? ChatName { get; set; } // For group chat invitations
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    
    // Navigation properties
    public User Sender { get; set; } = null!;
    public User Receiver { get; set; } = null!;
    public Chat? Chat { get; set; }
}

public class CreateGroupChatRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public List<string> MemberUsernames { get; set; } = new();
    
    [StringLength(500)]
    public string? InvitationMessage { get; set; }
}

public class CreateDirectMessageRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Message { get; set; }
}

public class SendInvitationRequest
{
    [Required]
    public string ReceiverUsername { get; set; } = string.Empty;
    
    public string? ChatId { get; set; }
    
    [Required]
    public InvitationType Type { get; set; }
    
    [StringLength(500)]
    public string? Message { get; set; }
    
    [StringLength(100)]
    public string? ChatName { get; set; } // For new group chats
}

public class RespondToInvitationRequest
{
    [Required]
    public string InvitationId { get; set; } = string.Empty;
    
    [Required]
    public InvitationStatus Response { get; set; } // Accepted or Declined
}

public class InvitationListResponse
{
    public bool Success { get; set; }
    public List<InvitationDto> Invitations { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class InvitationDto
{
    public string Id { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderUsername { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string ReceiverUsername { get; set; } = string.Empty;
    public string? ChatId { get; set; }
    public string? ChatName { get; set; }
    public InvitationType Type { get; set; }
    public InvitationStatus Status { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class UserSearchResponse
{
    public bool Success { get; set; }
    public List<UserDto> Users { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public enum InvitationType
{
    GroupChat,
    DirectMessage
}

public enum InvitationStatus
{
    Pending,
    Accepted,
    Declined,
    Expired,
    Cancelled
}