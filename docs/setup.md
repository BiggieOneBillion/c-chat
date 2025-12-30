# Getting Started

Follow these instructions to get the `c-chat` backend up and running on your local machine.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/) (optional for production-like testing)
- A terminal of your choice

## Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/yourusername/c-chat.git
   cd c-chat
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Configure Environment**:
   Update `appsettings.json` or `appsettings.Development.json` with your JWT secret and database connection string.

   ```json
   {
     "Jwt": {
       "SecretKey": "your-very-secure-secret-key-at-least-32-chars",
       "Issuer": "TerminalChatApp",
       "Audience": "TerminalChatApp"
     },
     "ConnectionStrings": {
       "PostgreSQL": "Host=localhost;Database=terminalchat;Username=postgres;Password=yourpassword"
     }
   }
   ```

4. **Build the project**:
   ```bash
   dotnet build
   ```

5. **Run the server**:
   ```bash
   dotnet run
   ```

The server will start at `http://localhost:5000`. You can access the Swagger UI at `http://localhost:5000/swagger` for interactive API testing.

## Database Modes

The application supports two database modes, controlled in `Program.cs`:

- **In-Memory**: Used by default for quick development and testing. Data is lost when the server stops.
- **PostgreSQL**: Used for development with persistence or production. Ensure the connection string is valid and `context.Database.MigrateAsync()` is enabled in `Program.cs`.

## Seed Data

On startup, the system automatically seeds two test users:
- **testuser1** / `password123`
- **testuser2** / `password123`
- A **General Chat** group containing both users.
