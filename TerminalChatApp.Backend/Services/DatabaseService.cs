using Microsoft.EntityFrameworkCore;
using TerminalChatApp.Backend.Data;
using TerminalChatApp.Backend.Models;

namespace TerminalChatApp.Backend.Services;

/// <summary>
/// Service for managing database operations, migrations, and seeding
/// Useful for both In-Memory and PostgreSQL configurations
/// </summary>
public class DatabaseService
{
    private readonly ChatContext _context;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(ChatContext context, ILogger<DatabaseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Initialize database - handles both In-Memory and PostgreSQL
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            // Check if we're using In-Memory database
            if (_context.Database.IsInMemory())
            {
                _logger.LogInformation("Using In-Memory database - ensuring created");
                await _context.Database.EnsureCreatedAsync();
            }
            else
            {
                _logger.LogInformation("Using persistent database - applying migrations");
                
                // For PostgreSQL: Apply pending migrations
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation($"Applying {pendingMigrations.Count()} pending migrations");
                    await _context.Database.MigrateAsync();
                    _logger.LogInformation("Database migrations applied successfully");
                }
                else
                {
                    _logger.LogInformation("Database is up to date");
                }
            }
            
            // Seed data if empty
            await SeedDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    /// <summary>
    /// Seed initial data for development/testing
    /// </summary>
    public async Task SeedDataAsync()
    {
        try
        {
            if (!await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Seeding initial data");
                
                // Add test users
                var user1 = new User
                {
                    Username = "testuser1",
                    Email = "test1@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
                };
                
                var user2 = new User
                {
                    Username = "testuser2",
                    Email = "test2@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
                };

                _context.Users.AddRange(user1, user2);
                await _context.SaveChangesAsync();

                // Add test chat
                var chat = new Chat
                {
                    Name = "General Chat",
                    Type = ChatType.Group
                };

                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();

                // Add participants
                var participant1 = new ChatParticipant
                {
                    UserId = user1.Id,
                    ChatId = chat.Id,
                    IsAdmin = true
                };
                
                var participant2 = new ChatParticipant
                {
                    UserId = user2.Id,
                    ChatId = chat.Id,
                    IsAdmin = false
                };

                _context.ChatParticipants.AddRange(participant1, participant2);
                await _context.SaveChangesAsync();

                // Add welcome message
                var message = new Message
                {
                    ChatId = chat.Id,
                    SenderId = user1.Id,
                    Content = "Welcome to the Terminal Chat App!",
                    Type = MessageType.System
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Initial data seeding completed");
            }
            else
            {
                _logger.LogInformation("Database already contains data, skipping seeding");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed data");
            throw;
        }
    }

    /// <summary>
    /// Get database health information
    /// </summary>
    public async Task<DatabaseHealthInfo> GetDatabaseHealthAsync()
    {
        try
        {
            var healthInfo = new DatabaseHealthInfo();
            
            // Check if database can be connected to
            healthInfo.CanConnect = await _context.Database.CanConnectAsync();
            
            if (healthInfo.CanConnect)
            {
                healthInfo.UserCount = await _context.Users.CountAsync();
                healthInfo.ChatCount = await _context.Chats.CountAsync();
                healthInfo.MessageCount = await _context.Messages.CountAsync();
                healthInfo.InvitationCount = await _context.ChatInvitations.CountAsync();
                
                // Get database provider
                healthInfo.DatabaseProvider = _context.Database.ProviderName ?? "Unknown";
                healthInfo.IsInMemory = _context.Database.IsInMemory();
                
                // For PostgreSQL, get version info
                if (!healthInfo.IsInMemory)
                {
                    try
                    {
                        // This will work for PostgreSQL
                        var result = await _context.Database.ExecuteSqlRawAsync("SELECT version()");
                        // Note: ExecuteSqlRawAsync doesn't return query results directly
                        // In a real implementation, you'd use a proper query to get version
                        healthInfo.DatabaseVersion = "PostgreSQL (version query requires proper implementation)";
                    }
                    catch
                    {
                        healthInfo.DatabaseVersion = "Unknown";
                    }
                }
                else
                {
                    healthInfo.DatabaseVersion = "In-Memory";
                }
            }
            
            return healthInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database health information");
            return new DatabaseHealthInfo
            {
                CanConnect = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Clean up test data (useful for development)
    /// </summary>
    public async Task CleanTestDataAsync()
    {
        try
        {
            _logger.LogInformation("Cleaning test data");
            
            // Remove in reverse order due to foreign key constraints
            _context.ChatInvitations.RemoveRange(_context.ChatInvitations);
            _context.Messages.RemoveRange(_context.Messages);
            _context.ChatParticipants.RemoveRange(_context.ChatParticipants);
            _context.Chats.RemoveRange(_context.Chats);
            _context.Users.RemoveRange(_context.Users);
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Test data cleaned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean test data");
            throw;
        }
    }
}

/// <summary>
/// Database health information
/// </summary>
public class DatabaseHealthInfo
{
    public bool CanConnect { get; set; }
    public string DatabaseProvider { get; set; } = string.Empty;
    public bool IsInMemory { get; set; }
    public string DatabaseVersion { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public int ChatCount { get; set; }
    public int MessageCount { get; set; }
    public int InvitationCount { get; set; }
    public string? ErrorMessage { get; set; }
}