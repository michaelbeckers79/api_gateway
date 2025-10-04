# Upstream Token Management and Backend Authentication

This document describes the new features added to the API Gateway for managing upstream tokens and backend-initiated authentication.

## Overview

The API Gateway now supports:

1. **Database schema with snake_case columns** - All database columns use snake_case naming (e.g., `user_id`, `created_at`)
2. **Configurable session timeouts** - Idle and absolute timeouts can be set in appsettings.json
3. **Backend-initiated authentication** - Backend services can initiate OAuth flows
4. **Route security policies** - Define how each route is secured for upstream services
5. **Upstream token management** - Automatic token acquisition and renewal for upstream services
6. **Distributed cache support** - Session and token data cached in memory (configurable for Redis, etc.)
7. **Multiple authentication modes**: Client Credentials, Token Exchange, Self-Signed JWT

## Database Schema Changes

### Snake_case Column Names

All database columns now use snake_case naming convention:
- `UserId` → `user_id`
- `CreatedAt` → `created_at`
- `IsActive` → `is_active`

Table names remain in PascalCase for SQLite compatibility.

### New Tables

#### RoutePolicies
Defines security policies for routes:
```sql
CREATE TABLE RoutePolicies (
    id INTEGER PRIMARY KEY,
    route_id TEXT NOT NULL,
    security_type TEXT NOT NULL,  -- 'none', 'session', 'client_credentials', 'token_exchange', 'self_signed'
    token_endpoint TEXT,
    client_id TEXT,
    client_secret TEXT,
    scope TEXT,
    token_expiration_seconds INTEGER,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
```

#### UpstreamTokens
Stores tokens for upstream services:
```sql
CREATE TABLE UpstreamTokens (
    id INTEGER PRIMARY KEY,
    route_id TEXT NOT NULL,
    session_id INTEGER,  -- NULL for client_credentials
    access_token TEXT NOT NULL,
    refresh_token TEXT,
    expires_at TEXT NOT NULL,
    created_at TEXT NOT NULL,
    last_refreshed_at TEXT
);
```

## Configuration

### appsettings.json

```json
{
  "Session": {
    "IdleTimeoutMinutes": 30,
    "AbsoluteTimeoutHours": 8
  },
  "Jwt": {
    "UsernameClaim": "preferred_username",
    "Secret": "your-secret-key-min-32-chars",
    "Issuer": "api-gateway",
    "Audience": "api-gateway"
  },
  "OAuth": {
    "AuthorizationEndpoint": "https://auth.example.com/authorize",
    "TokenEndpoint": "https://auth.example.com/token",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "RedirectUri": "https://your-gateway.com/oauth/callback",
    "Scope": "openid profile email"
  }
}
```

## Security Types

### 1. None (`none`)
No authentication required for the upstream service.

### 2. Session (`session`)
Use the user's session token directly.

### 3. Client Credentials (`client_credentials`)
OAuth 2.0 Client Credentials flow - gateway authenticates as itself.

**Configuration:**
```sql
INSERT INTO RoutePolicies (route_id, security_type, token_endpoint, client_id, client_secret, scope, token_expiration_seconds, created_at, updated_at)
VALUES (
  'api-route',
  'client_credentials',
  'https://auth.example.com/token',
  'gateway-client-id',
  'gateway-client-secret',
  'api.read api.write',
  3600,
  datetime('now'),
  datetime('now')
);
```

### 4. Token Exchange (`token_exchange`)
RFC 8693 Token Exchange - exchange user's token for a downstream token.

**Configuration:**
```sql
INSERT INTO RoutePolicies (route_id, security_type, token_endpoint, client_id, client_secret, scope, token_expiration_seconds, created_at, updated_at)
VALUES (
  'api-route',
  'token_exchange',
  'https://auth.example.com/token',
  'api-gateway',
  'your-secret',
  'downstream-service',
  3600,
  datetime('now'),
  datetime('now')
);
```

### 5. Self-Signed JWT (`self_signed`)
Generate a JWT signed by the gateway based on the user's session.

**Configuration:**
```sql
INSERT INTO RoutePolicies (route_id, security_type, token_endpoint, client_id, client_secret, scope, token_expiration_seconds, created_at, updated_at)
VALUES (
  'api-route',
  'self_signed',
  NULL,
  NULL,
  NULL,
  NULL,
  3600,
  datetime('now'),
  datetime('now')
);
```

## Backend-Initiated Authentication

### Endpoint: POST /oauth/backend/initiate

Allows backend services to initiate OAuth authentication flows.

**Request:**
```json
{
  "clientId": "api-gateway",
  "redirectUri": "https://your-gateway.com/oauth/callback"
}
```

**Response:**
```json
{
  "authorizationUrl": "https://auth.example.com/authorize?...",
  "state": "random-state-value"
}
```

The backend service can redirect users to `authorizationUrl` to complete authentication.

## Token Management

### Automatic Token Acquisition

When a request arrives for a route with a security policy:
1. Gateway checks distributed cache for token
2. If not found or expired, checks database
3. If not found or expired, acquires new token based on policy type
4. Stores token in both cache and database
5. Attaches token to upstream request

### Token Renewal

Tokens are automatically refreshed when they are within 5 minutes of expiration.

### Manual Token Refresh

```csharp
await _upstreamTokenService.RefreshUpstreamTokenAsync("route-id", sessionId);
```

## Distributed Cache

The gateway uses `IDistributedCache` for token storage:

**Default (Memory):**
```csharp
builder.Services.AddDistributedMemoryCache();
```

**Redis (Production):**
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

## Architecture

```
┌─────────────┐
│   Client    │
└─────┬───────┘
      │ 1. Request with session cookie
      ▼
┌─────────────────────────────────┐
│       API Gateway               │
│                                 │
│  ┌───────────────────────────┐ │
│  │ Session Validation        │ │
│  └───────────┬───────────────┘ │
│              │                  │
│  ┌───────────▼───────────────┐ │
│  │ Upstream Token Service    │ │
│  │ - Check cache             │ │
│  │ - Get/refresh token       │ │
│  │ - Apply policy            │ │
│  └───────────┬───────────────┘ │
└──────────────┼─────────────────┘
               │ 2. Request with upstream token
               ▼
┌─────────────────────────────────┐
│     Upstream Service            │
└─────────────────────────────────┘

Database:
- Sessions (persistent)
- Upstream tokens (persistent)

Cache:
- Session data (fast lookup)
- Upstream tokens (fast lookup)
```

## Usage Examples

### C# Service Usage

```csharp
public class MyService
{
    private readonly IUpstreamTokenService _upstreamTokenService;
    private readonly IHttpClientFactory _httpClientFactory;

    public async Task<string> CallUpstreamService(string routeId, int sessionId)
    {
        // Get or create upstream token
        var token = await _upstreamTokenService.GetOrCreateUpstreamTokenAsync(routeId, sessionId);
        
        if (string.IsNullOrEmpty(token))
        {
            throw new UnauthorizedAccessException("Failed to get upstream token");
        }

        // Make request to upstream service
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var response = await client.GetAsync("https://upstream-service.com/api/resource");
        return await response.Content.ReadAsStringAsync();
    }
}
```

## Security Considerations

1. **Token Storage**: Tokens are encrypted at rest in the database
2. **Cache Security**: Use Redis with authentication in production
3. **Token Lifetime**: Configure appropriate expiration times
4. **Secrets Management**: Store client secrets securely (Azure Key Vault, AWS Secrets Manager)
5. **Network Security**: Use HTTPS for all token exchanges
6. **Audit Logging**: Token acquisitions are logged with timestamps

## Migration Guide

### From Previous Version

1. Stop the gateway
2. Backup your database
3. Update the code
4. Run migrations: `dotnet ef database update`
5. Update appsettings.json with new configuration
6. Configure route policies in the database
7. Restart the gateway

The migration will:
- Add snake_case column names (existing data preserved)
- Create RoutePolicies table
- Create UpstreamTokens table
- Add security_policy column to RouteConfigs

## Troubleshooting

### Token Not Being Acquired

1. Check route policy exists in database
2. Verify token endpoint is reachable
3. Check client credentials are correct
4. Review logs for detailed error messages

### Cache Not Working

1. Verify IDistributedCache is registered
2. Check Redis connection (if using Redis)
3. Review cache expiration settings

### Session Timeout Issues

1. Check Session:IdleTimeoutMinutes in appsettings.json
2. Check Session:AbsoluteTimeoutHours in appsettings.json
3. Verify clock synchronization between servers
