# PostgreSQL Production Deployment Guide

This guide covers deploying the Terminal Chat App with PostgreSQL in production environments.

## Pre-Deployment Checklist

### 1. Environment Variables Setup

Create secure environment variables for production:

```bash
# Database connection
export TERMINALCHAT_DB_HOST="your-postgres-server.com"
export TERMINALCHAT_DB_PORT="5432"
export TERMINALCHAT_DB_NAME="terminalchatapp_prod"
export TERMINALCHAT_DB_USER="terminalchat_prod_user"
export TERMINALCHAT_DB_PASSWORD="your_super_secure_password_here"

# JWT Configuration
export TERMINALCHAT_JWT_KEY="your-256-bit-secret-key-here"
export TERMINALCHAT_JWT_ISSUER="https://your-domain.com"
export TERMINALCHAT_JWT_AUDIENCE="terminalchatapp"

# CORS Settings
export TERMINALCHAT_CORS_ORIGINS="https://your-frontend-domain.com,https://your-app-domain.com"
```

### 2. Update appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "ConnectionStrings": {
    "PostgreSQL": "Host=${TERMINALCHAT_DB_HOST};Port=${TERMINALCHAT_DB_PORT};Database=${TERMINALCHAT_DB_NAME};Username=${TERMINALCHAT_DB_USER};Password=${TERMINALCHAT_DB_PASSWORD};SSL Mode=Require;Trust Server Certificate=false;Include Error Detail=false"
  },
  "JwtSettings": {
    "Key": "${TERMINALCHAT_JWT_KEY}",
    "Issuer": "${TERMINALCHAT_JWT_ISSUER}",
    "Audience": "${TERMINALCHAT_JWT_AUDIENCE}",
    "ExpiryInHours": 24
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://*:5000"
      },
      "Https": {
        "Url": "https://*:5001"
      }
    }
  }
}
```

## PostgreSQL Server Setup

### 1. Production Database Creation

```sql
-- Connect as postgres superuser
CREATE USER terminalchat_prod_user WITH ENCRYPTED PASSWORD 'your_super_secure_password_here';

-- Create production database
CREATE DATABASE terminalchatapp_prod 
    WITH OWNER terminalchat_prod_user
    ENCODING 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8'
    TEMPLATE template0;

-- Grant necessary privileges
GRANT ALL PRIVILEGES ON DATABASE terminalchatapp_prod TO terminalchat_prod_user;
GRANT CREATE ON SCHEMA public TO terminalchat_prod_user;
GRANT USAGE ON SCHEMA public TO terminalchat_prod_user;
```

### 2. Database Security Configuration

```sql
-- Create read-only user for monitoring
CREATE USER terminalchat_readonly WITH ENCRYPTED PASSWORD 'readonly_password_here';
GRANT CONNECT ON DATABASE terminalchatapp_prod TO terminalchat_readonly;
GRANT USAGE ON SCHEMA public TO terminalchat_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO terminalchat_readonly;
GRANT SELECT ON ALL SEQUENCES IN SCHEMA public TO terminalchat_readonly;

-- Set default privileges for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO terminalchat_readonly;
```

### 3. PostgreSQL Configuration (postgresql.conf)

```ini
# Connection settings
listen_addresses = '*'
port = 5432
max_connections = 200

# Memory settings
shared_buffers = 256MB
work_mem = 4MB
maintenance_work_mem = 64MB

# Write-ahead logging
wal_level = replica
max_wal_size = 1GB
min_wal_size = 80MB

# Query planner
random_page_cost = 1.1
effective_cache_size = 1GB

# Logging
log_statement = 'mod'
log_min_duration_statement = 1000
log_connections = on
log_disconnections = on
log_line_prefix = '%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h '

# Security
ssl = on
ssl_cert_file = 'server.crt'
ssl_key_file = 'server.key'
password_encryption = scram-sha-256
```

## Application Deployment

### 1. Activate PostgreSQL in Program.cs

Uncomment the PostgreSQL configuration block in `Program.cs`:

```csharp
// UNCOMMENT THIS BLOCK FOR POSTGRESQL
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
    
    // Only enable detailed errors in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});

// Comment out the in-memory database
// builder.Services.AddDbContext<ChatContext>(options => options.UseInMemoryDatabase("ChatApp"));
```

### 2. Database Migration Commands

```bash
# Navigate to backend directory
cd TerminalChatApp.Backend

# Add initial migration (if not exists)
dotnet ef migrations add InitialCreate

# Update database to latest migration
dotnet ef database update --configuration Release

# Generate SQL script for deployment (alternative approach)
dotnet ef migrations script --output ../deploy/migration.sql --configuration Release
```

### 3. Docker Production Setup

Create `docker-compose.prod.yml`:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: terminalchatapp_prod
      POSTGRES_USER: terminalchat_prod_user
      POSTGRES_PASSWORD: ${TERMINALCHAT_DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
    ports:
      - "5432:5432"
    restart: unless-stopped
    networks:
      - terminalchat_network

  backend:
    build: 
      context: .
      dockerfile: Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: https://+:443;http://+:80
      ASPNETCORE_Kestrel__Certificates__Default__Password: ${CERT_PASSWORD}
      ASPNETCORE_Kestrel__Certificates__Default__Path: /app/certs/aspnetapp.pfx
    ports:
      - "80:80"
      - "443:443"
    depends_on:
      - postgres
    volumes:
      - ./certs:/app/certs:ro
    restart: unless-stopped
    networks:
      - terminalchat_network

volumes:
  postgres_data:

networks:
  terminalchat_network:
    driver: bridge
```

### 4. SSL Certificate Setup

```bash
# Generate development certificate (for testing)
dotnet dev-certs https -ep aspnetapp.pfx -p your_cert_password

# For production, use Let's Encrypt or your certificate authority
# Place certificates in ./certs/ directory
```

## Monitoring and Maintenance

### 1. Health Check Endpoints

Uncomment the `HealthController.cs` to enable health monitoring:

- `GET /api/health` - Overall application health
- `GET /api/health/database` - Database-specific health check

### 2. Database Backup Strategy

```bash
#!/bin/bash
# backup.sh - Daily backup script

BACKUP_DIR="/var/backups/terminalchatapp"
DATE=$(date +%Y%m%d_%H%M%S)
DB_NAME="terminalchatapp_prod"
DB_USER="terminalchat_prod_user"

# Create backup directory
mkdir -p $BACKUP_DIR

# Create backup
pg_dump -h localhost -U $DB_USER -d $DB_NAME -f $BACKUP_DIR/terminalchat_$DATE.sql

# Compress backup
gzip $BACKUP_DIR/terminalchat_$DATE.sql

# Remove backups older than 30 days
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete

echo "Backup completed: terminalchat_$DATE.sql.gz"
```

### 3. Log Monitoring

```bash
# View application logs
docker logs terminalchat-backend

# View PostgreSQL logs
docker logs terminalchat-postgres

# Follow logs in real-time
docker logs -f terminalchat-backend
```

## Performance Tuning

### 1. Database Indexes

```sql
-- Add indexes for better performance (already included in migrations)
CREATE INDEX IF NOT EXISTS idx_messages_chat_timestamp ON "Messages"("ChatId", "Timestamp" DESC);
CREATE INDEX IF NOT EXISTS idx_chatparticipants_user ON "ChatParticipants"("UserId");
CREATE INDEX IF NOT EXISTS idx_invitations_receiver_status ON "ChatInvitations"("ReceiverId", "Status");
```

### 2. Connection Pool Configuration

In `Program.cs`, configure connection pooling:

```csharp
builder.Services.AddDbContext<ChatContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10));
    });
}, ServiceLifetime.Scoped); // Use scoped lifetime for better performance
```

## Security Considerations

### 1. Database Access Control

```sql
-- Revoke public access
REVOKE ALL ON SCHEMA public FROM public;
REVOKE ALL ON DATABASE terminalchatapp_prod FROM public;

-- Only allow connections from application servers
-- Edit pg_hba.conf
host terminalchatapp_prod terminalchat_prod_user 10.0.0.0/8 scram-sha-256
```

### 2. Network Security

- Use SSL/TLS for all database connections
- Implement IP whitelisting for database access
- Use VPN or private networks for database communication
- Regular security updates for PostgreSQL

### 3. Application Security

- Use strong, unique passwords for all database users
- Rotate JWT signing keys regularly
- Implement rate limiting
- Use HTTPS only in production
- Regular dependency updates

## Troubleshooting

### Common Issues

1. **Connection Timeout**
   - Check firewall settings
   - Verify PostgreSQL is listening on correct interface
   - Check connection string format

2. **Migration Failures**
   - Verify database user has sufficient privileges
   - Check for conflicting data
   - Review migration logs

3. **Performance Issues**
   - Monitor database connections
   - Check query performance with `EXPLAIN ANALYZE`
   - Review PostgreSQL logs for slow queries

### Useful Commands

```bash
# Check database connection
psql -h hostname -U username -d database_name

# Monitor active connections
SELECT * FROM pg_stat_activity WHERE datname = 'terminalchatapp_prod';

# Check database size
SELECT pg_size_pretty(pg_database_size('terminalchatapp_prod'));

# Analyze query performance
EXPLAIN ANALYZE SELECT * FROM "Messages" WHERE "ChatId" = 1 ORDER BY "Timestamp" DESC LIMIT 50;
```