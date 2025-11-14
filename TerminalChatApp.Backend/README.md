# Terminal Chat App Backend

.NET Web API backend for the Terminal Chat App with real-time messaging capabilities.

## Features

- **RESTful API**: User authentication, chat management, and messaging endpoints
- **SignalR Hub**: Real-time messaging, typing indicators, and user presence
- **JWT Authentication**: Secure token-based authentication
- **In-Memory Database**: Entity Framework with in-memory storage for development
- **CORS Support**: Configured for frontend communication

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - User login
- `POST /api/auth/logout` - User logout

### Chats
- `GET /api/chats` - Get user's chats
- `POST /api/chats` - Create new chat
- `GET /api/chats/{id}/messages` - Get chat messages
- `POST /api/chats/{id}/messages` - Send message

### SignalR Hub
- **Hub URL**: `/chathub`
- **Methods**: `SendMessage`, `JoinChat`, `LeaveChat`, `SetTyping`
- **Events**: `ReceiveMessage`, `UserTyping`, `UserOnlineStatus`

## Quick Start

```bash
# Install dependencies
dotnet restore

# Build the project
dotnet build

# Run the server
dotnet run
```

The server will start on `http://localhost:5000`.

## Test Users

The application seeds test data on startup:
- **User 1**: `testuser1` / `password123`
- **User 2**: `testuser2` / `password123`

## Configuration

Update `appsettings.json` to configure:
- JWT settings (SecretKey, Issuer, Audience)
- Database connection (currently using in-memory)
- Logging levels

## Development

### Technologies Used
- .NET 9
- Entity Framework Core (In-Memory)
- SignalR
- JWT Bearer Authentication
- BCrypt for password hashing
- Swagger/OpenAPI documentation

### Project Structure
```
├── Controllers/        # API controllers
├── Models/            # Data models and DTOs
├── Services/          # Business logic services
├── Data/              # Entity Framework context
├── Hubs/              # SignalR hubs
└── Program.cs         # Application configuration
```

### Database Models
- **User**: Authentication and user profile
- **Chat**: Chat rooms (private/group)
- **ChatParticipant**: User-chat relationships
- **Message**: Chat messages with timestamps

The application uses Entity Framework's in-memory database for development. For production, configure a persistent database in `Program.cs`.