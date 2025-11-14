using System.ComponentModel.DataAnnotations;

namespace TerminalChatApp.Backend.Models;

public class Chat
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public ChatType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public List<ChatParticipant> Participants { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}

public class ChatParticipant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsAdmin { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Chat Chat { get; set; } = null!;
}

public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ChatId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public MessageType Type { get; set; } = MessageType.Text;
    
    // Navigation properties
    public Chat Chat { get; set; } = null!;
    public User Sender { get; set; } = null!;
}

public class CreateChatRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public ChatType Type { get; set; }
    public List<string> ParticipantUsernames { get; set; } = new();
}

public class ChatListResponse
{
    public bool Success { get; set; }
    public List<ChatDto> Chats { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class MessagesResponse
{
    public bool Success { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class ChatDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ChatType Type { get; set; }
    public List<string> ParticipantIds { get; set; } = new();
    public List<UserDto> Participants { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public MessageDto? LastMessage { get; set; }
}

public class MessageDto
{
    public string Id { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderUsername { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public MessageType Type { get; set; }
}

public enum ChatType
{
    Private,
    Group
}

public enum MessageType
{
    Text,
    System,
    Notification
}