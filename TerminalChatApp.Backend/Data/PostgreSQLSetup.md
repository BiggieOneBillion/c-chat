# PostgreSQL Setup for Terminal Chat App

## Prerequisites

1. **Install PostgreSQL** (if not already installed):
   ```bash
   # macOS with Homebrew
   brew install postgresql
   brew services start postgresql
   
   # Ubuntu/Debian
   sudo apt-get install postgresql postgresql-contrib
   
   # Windows
   # Download from https://www.postgresql.org/download/windows/
   ```

2. **Create Database and User**:
   ```sql
   -- Connect to PostgreSQL as superuser
   sudo -u postgres psql
   
   -- Create database
   CREATE DATABASE terminalchatapp;
   
   -- Create user with password
   CREATE USER terminalchat_user WITH ENCRYPTED PASSWORD 'secure_password_123';
   
   -- Grant privileges
   GRANT ALL PRIVILEGES ON DATABASE terminalchatapp TO terminalchat_user;
   
   -- Grant schema privileges (PostgreSQL 15+)
   \c terminalchatapp
   GRANT ALL ON SCHEMA public TO terminalchat_user;
   
   -- Exit
   \q
   ```

## Configuration Options

### Option 1: Simple Connection (Default PostgreSQL User)
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=terminalchatapp;Username=postgres;Password=your_password_here"
}
```

### Option 2: Dedicated User (Recommended for Production)
```json
"ConnectionStrings": {
  "PostgreSQL": "Host=localhost;Port=5432;Database=terminalchatapp;Username=terminalchat_user;Password=secure_password_123;Include Error Detail=true"
}
```

### Option 3: Environment Variables (Most Secure)
```json
"ConnectionStrings": {
  "PostgreSQL": "Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};Include Error Detail=true"
}
```

## Migration Commands

```bash
# Add initial migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update

# Add new migration after model changes
dotnet ef migrations add AddNewFeature

# Remove last migration (if not applied to database)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script
```

## Connection String Parameters

- **Host**: PostgreSQL server address (localhost for local development)
- **Port**: PostgreSQL port (default: 5432)
- **Database**: Database name
- **Username**: Database user
- **Password**: User password
- **Include Error Detail**: Shows detailed error messages (development only)
- **SSL Mode**: SSL connection mode (Disable, Require, Prefer)
- **Connection Timeout**: Connection timeout in seconds
- **Command Timeout**: Command execution timeout in seconds

## Production Considerations

1. **SSL/TLS**: Always use SSL in production
   ```
   "PostgreSQL": "Host=prod-server;Database=terminalchatapp;Username=app_user;Password=secure_pass;SSL Mode=Require"
   ```

2. **Connection Pooling**: Configure appropriate pool sizes
   ```
   "PostgreSQL": "Host=server;Database=db;Username=user;Password=pass;Maximum Pool Size=100;Minimum Pool Size=5"
   ```

3. **Security**: Use environment variables or Azure Key Vault for connection strings
4. **Monitoring**: Enable logging and monitoring for database connections
5. **Backup**: Set up automated backups for production databases

## Troubleshooting

### Common Issues:
1. **Connection Refused**: Check if PostgreSQL service is running
2. **Authentication Failed**: Verify username/password and pg_hba.conf settings
3. **Database Does Not Exist**: Create the database first
4. **Permission Denied**: Grant proper privileges to the user
5. **SSL Issues**: Configure SSL settings appropriately

### Useful Commands:
```bash
# Check PostgreSQL status
brew services list | grep postgres  # macOS
sudo systemctl status postgresql    # Linux

# Connect to database
psql -h localhost -U terminalchat_user -d terminalchatapp

# List databases
\l

# List tables
\dt

# Describe table
\d table_name
```