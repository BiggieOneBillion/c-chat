using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TerminalChatApp.Backend.Data;
using TerminalChatApp.Backend.Hubs;
using TerminalChatApp.Backend.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Configure Entity Framework with In-Memory Database (Development)
// builder.Services.AddDbContext<ChatContext>(options =>
//     options.UseInMemoryDatabase("TerminalChatApp"));

// ALTERNATIVE: PostgreSQL Database Configuration (Production)
// Uncomment the block below and comment out the in-memory configuration above to use PostgreSQL

builder.Services.AddDbContext<ChatContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSQL") 
        ?? throw new InvalidOperationException("Connection string 'PostgreSQL' not found.");
    
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly("TerminalChatApp.Backend");
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    });
    
    // Enable detailed errors in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});


// Add JWT Service
builder.Services.AddScoped<JwtService>();

// Add Database Service (handles both In-Memory and PostgreSQL)
builder.Services.AddScoped<DatabaseService>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? "your-256-bit-secret-key-here-make-it-long-enough";
var issuer = jwtSettings["Issuer"] ?? "TerminalChatApp";
var audience = jwtSettings["Audience"] ?? "TerminalChatApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Configure JWT for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
    
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:5000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add API Explorer for Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS
app.UseCors("AllowAll");

// Don't use HTTPS redirection for development
// app.UseHttpsRedirection();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map SignalR Hub
app.MapHub<ChatHub>("/chathub");

// Initialize database (works for both In-Memory and PostgreSQL)
using (var scope = app.Services.CreateScope())
{
    var databaseService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
    await databaseService.InitializeDatabaseAsync();
    
    // ALTERNATIVE: Manual database initialization (if not using DatabaseService)
    
    var context = scope.ServiceProvider.GetRequiredService<ChatContext>();
    
    // For PostgreSQL: Apply migrations automatically
    await context.Database.MigrateAsync();
    
    // For In-Memory: Ensure database is created
    // await context.Database.EnsureCreatedAsync();
    
    await SeedDatabase(context);
    
}

app.Run();

// Seed some initial data for testing
async Task SeedDatabase(ChatContext context)
{
    if (!context.Users.Any())
    {
        // Add test users
        var user1 = new TerminalChatApp.Backend.Models.User
        {
            Username = "testuser1",
            Email = "test1@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        
        var user2 = new TerminalChatApp.Backend.Models.User
        {
            Username = "testuser2",
            Email = "test2@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };

        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        // Add test chat
        var chat = new TerminalChatApp.Backend.Models.Chat
        {
            Name = "General Chat",
            Type = TerminalChatApp.Backend.Models.ChatType.Group
        };

        context.Chats.Add(chat);
        await context.SaveChangesAsync();

        // Add participants
        var participant1 = new TerminalChatApp.Backend.Models.ChatParticipant
        {
            UserId = user1.Id,
            ChatId = chat.Id,
            IsAdmin = true
        };
        
        var participant2 = new TerminalChatApp.Backend.Models.ChatParticipant
        {
            UserId = user2.Id,
            ChatId = chat.Id,
            IsAdmin = false
        };

        context.ChatParticipants.AddRange(participant1, participant2);
        await context.SaveChangesAsync();

        // Add test message
        var message = new TerminalChatApp.Backend.Models.Message
        {
            ChatId = chat.Id,
            SenderId = user1.Id,
            Content = "Welcome to the Terminal Chat App!",
            Type = TerminalChatApp.Backend.Models.MessageType.System
        };

        context.Messages.Add(message);
        await context.SaveChangesAsync();
    }
}
