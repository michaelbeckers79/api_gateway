# API Gateway - BFF with YARP and OAuth Token Handler Pattern

A Backend-for-Frontend (BFF) API Gateway built with .NET Core 9, YARP (Yet Another Reverse Proxy), and Entity Framework Core. This gateway implements the OAuth token handler pattern with opaque session tokens stored in encrypted cookies, following OWASP security best practices.

## Features

### Core Functionality
- **YARP Reverse Proxy**: Database-configurable routing for backend services
- **OAuth Agent Pattern**: Handles OAuth 2.0 authorization flows with PKCE
- **Token Handler Pattern**: Secure opaque session tokens with encrypted cookies
- **Entity Framework Core**: SQLite database with snake_case columns for route and session management
- **OWASP Security Best Practices**: Comprehensive security headers and cookie configuration
- **Upstream Token Management**: Automatic token acquisition and renewal for backend services
- **Distributed Cache**: Session and token caching with memory or Redis support
- **Backend-Initiated Auth**: Support for backend services to initiate OAuth flows

### Security Features

#### Cookie Security (OWASP Best Practices)
- **HttpOnly**: Prevents XSS attacks by making cookies inaccessible to JavaScript
- **Secure**: Cookies only transmitted over HTTPS
- **SameSite=Strict**: Prevents CSRF attacks
- **__Host- Prefix**: Ensures Secure flag and no Domain attribute (additional CSRF protection)
- **Encrypted Content**: All cookie values encrypted using ASP.NET Core Data Protection

#### Session Management
- **Opaque Tokens**: Session tokens don't expose user information
- **Configurable Timeouts**: Idle timeout (default 30 min) and absolute timeout (default 8 hours) configurable in appsettings.json
- **Session Binding**: Tracks IP address and User-Agent
- **Automatic Cleanup**: Background service removes expired sessions

#### OAuth Security
- **PKCE (Proof Key for Code Exchange)**: Protection against authorization code interception
- **State Parameter**: CSRF protection for OAuth flow
- **Secure Token Storage**: Access tokens stored server-side, not in browser

#### Upstream Token Management
- **Multiple Auth Modes**: Client Credentials, Token Exchange, Self-Signed JWT
- **Automatic Token Renewal**: Tokens refreshed automatically before expiration
- **Route Security Policies**: Define authentication strategy per route
- **Distributed Cache**: Fast token lookup with configurable backend

## Architecture

```
┌─────────────────┐
│  SPA/Frontend   │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────────┐
│         API Gateway (BFF)               │
│  ┌───────────────────────────────────┐  │
│  │   Auth Controller                 │  │
│  │   - Login Start                   │  │
│  │   - Login End/Callback            │  │
│  │   - Is Logged In                  │  │
│  │   - Logout                        │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │   OAuth Agent Service             │  │
│  │   - Authorization Request Gen     │  │
│  │   - Token Exchange                │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │   Session Token Service           │  │
│  │   - Create/Validate Sessions      │  │
│  │   - Encrypted Cookie Management   │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │   YARP Reverse Proxy              │  │
│  │   - Database-backed Routes        │  │
│  │   - Dynamic Configuration         │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
         │
         ▼
┌─────────────────┐
│  Backend APIs   │
└─────────────────┘
```

## API Endpoints

### Authentication Endpoints

#### POST `/oauth/login/start`
Initiates the OAuth login flow.

**Request:**
```json
{
  "redirectUri": "https://localhost:5000/oauth/callback"
}
```

**Response:**
```json
{
  "authorizationUrl": "https://auth.example.com/authorize?...",
  "instructions": {
    "action": "redirect",
    "url": "https://auth.example.com/authorize?...",
    "method": "GET"
  }
}
```

#### GET `/oauth/callback`
OAuth callback endpoint (called by authorization server).

**Query Parameters:**
- `code`: Authorization code
- `state`: State parameter for CSRF protection
- `error`: Error code (if authorization failed)

#### POST `/oauth/login/end`
Alternative endpoint for SPAs to complete login.

**Request:**
```json
{
  "code": "authorization_code",
  "state": "state_value",
  "error": null
}
```

#### GET `/oauth/isloggedin`
Check if the current user is logged in.

**Response:**
```json
{
  "isLoggedIn": true,
  "userId": "user-123"
}
```

#### POST `/oauth/logout`
Logout and revoke the current session.

**Response:**
```json
{
  "success": true,
  "message": "Logout successful"
}
```

#### POST `/oauth/backend/initiate` (New)
Initiate OAuth flow from backend service.

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

### Health Check Endpoint

#### GET `/health`
Returns the health status of the API Gateway.

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=apigateway.db"
  },
  "Session": {
    "IdleTimeoutMinutes": 30,
    "AbsoluteTimeoutHours": 8
  },
  "OAuth": {
    "AuthorizationEndpoint": "https://auth.example.com/authorize",
    "TokenEndpoint": "https://auth.example.com/token",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "RedirectUri": "https://localhost:5000/oauth/callback",
    "Scope": "openid profile email"
  },
  "Jwt": {
    "UsernameClaim": "preferred_username",
    "Secret": "your-secret-key-min-32-chars",
    "Issuer": "api-gateway",
    "Audience": "api-gateway"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:4200"
    ]
  }
}
```

### Database Configuration

The gateway uses SQLite by default with **snake_case column naming**. Routes and clusters are stored in the database and can be modified at runtime.

**RouteConfig Table:**
- route_id: Unique identifier for the route
- cluster_id: Reference to the cluster
- match: Path pattern (e.g., `/api/{**catch-all}`)
- order: Route priority
- is_active: Enable/disable route
- security_policy: Security type reference (optional)

**ClusterConfig Table:**
- cluster_id: Unique identifier for the cluster
- destination_address: Backend service URL
- is_active: Enable/disable cluster

**SessionToken Table:**
- token_id: Opaque session token
- user_id: User identifier
- access_token: OAuth access token (stored server-side)
- refresh_token: OAuth refresh token (stored server-side)
- expires_at: Absolute expiration time
- last_accessed_at: Last activity time (for idle timeout)
- is_revoked: Revocation flag

**RoutePolicies Table (New):**
- route_id: Reference to route
- security_type: Authentication method (none, session, client_credentials, token_exchange, self_signed)
- token_endpoint: OAuth token endpoint (for client_credentials and token_exchange)
- client_id: Client ID for OAuth flows
- client_secret: Client secret for OAuth flows
- scope: OAuth scopes
- token_expiration_seconds: Token lifetime

**UpstreamTokens Table (New):**
- route_id: Reference to route
- session_id: Reference to session (nullable for client_credentials)
- access_token: Token for upstream service
- refresh_token: Refresh token (optional)
- expires_at: Token expiration time
- last_refreshed_at: Last refresh timestamp

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- SQLite (included)

### Running the Gateway

1. **Clone the repository**
   ```bash
   git clone https://github.com/michaelbeckers79/api_gateway.git
   cd api_gateway
   ```

2. **Configure OAuth settings**
   
   Edit `src/ApiGateway/appsettings.json` with your OAuth provider details.

3. **Run the application**
   ```bash
   cd src/ApiGateway
   dotnet run
   ```

4. **Access the API**
   
   The gateway will be available at `https://localhost:5001` (or the port specified in your launch settings).

### Database Migrations

The database is automatically created and migrated on application startup. To manually manage migrations:

```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

## OWASP Security Best Practices Implemented

### A01:2021 – Broken Access Control
- Session validation middleware enforces authentication
- Session binding to IP and User-Agent
- Token revocation capability

### A02:2021 – Cryptographic Failures
- ASP.NET Core Data Protection for cookie encryption
- Secure token generation using cryptographically secure random number generator
- HTTPS enforced for all traffic

### A03:2021 – Injection
- Entity Framework Core parameterized queries
- Input validation on all endpoints

### A05:2021 – Security Misconfiguration
- Secure cookie configuration (HttpOnly, Secure, SameSite)
- Security headers (X-Content-Type-Options, X-Frame-Options, etc.)
- CORS properly configured

### A07:2021 – Identification and Authentication Failures
- Strong session token generation (256-bit random tokens)
- Session timeouts (absolute and idle)
- PKCE implementation for OAuth
- State parameter for CSRF protection

### A08:2021 – Software and Data Integrity Failures
- Data Protection API for cookie integrity
- State validation for OAuth flow

## Development

### Project Structure

```
api_gateway/
├── src/
│   └── ApiGateway/
│       ├── Controllers/        # API controllers
│       │   └── AuthController.cs
│       ├── Data/              # Database context
│       │   └── ApiGatewayDbContext.cs
│       ├── Middleware/        # Custom middleware
│       │   └── SessionValidationMiddleware.cs
│       ├── Models/            # Entity models
│       │   ├── RouteConfig.cs
│       │   ├── ClusterConfig.cs
│       │   └── SessionToken.cs
│       ├── Services/          # Business logic
│       │   ├── OAuthAgentService.cs
│       │   ├── SessionTokenService.cs
│       │   ├── DatabaseProxyConfigProvider.cs
│       │   └── SessionCleanupBackgroundService.cs
│       ├── Migrations/        # EF Core migrations
│       ├── Program.cs         # Application entry point
│       └── appsettings.json   # Configuration
└── ApiGateway.sln            # Solution file
```

### Adding Routes Dynamically

Routes can be added to the database at runtime:

```sql
INSERT INTO RouteConfigs (RouteId, ClusterId, Match, Order, IsActive, CreatedAt, UpdatedAt)
VALUES ('my-new-route', 'backend-api', '/myapi/{**catch-all}', 1, 1, datetime('now'), datetime('now'));
```

Then reload the proxy configuration by restarting the application or implementing a reload endpoint.

## Testing

### Testing the Login Flow

1. Call `/oauth/login/start` to get the authorization URL
2. Navigate to the authorization URL
3. Complete authentication with your OAuth provider
4. Get redirected to `/oauth/callback`
5. Session cookie is set automatically
6. Call `/oauth/isloggedin` to verify the session

### Testing with cURL

```bash
# Start login
curl -X POST https://localhost:5001/oauth/login/start \
  -H "Content-Type: application/json" \
  -d '{"redirectUri": "https://localhost:5001/oauth/callback"}'

# Check login status
curl -X GET https://localhost:5001/oauth/isloggedin \
  -b cookies.txt

# Logout
curl -X POST https://localhost:5001/oauth/logout \
  -b cookies.txt
```

## Production Considerations

### Security
1. **Use HTTPS**: Always use HTTPS in production
2. **Key Management**: Configure Data Protection to persist keys in a secure location (Azure Key Vault, AWS KMS, etc.)
3. **Database Security**: Use a production database (PostgreSQL, SQL Server) with proper access controls
4. **Secrets Management**: Use environment variables or secret management services for sensitive configuration
5. **Rate Limiting**: Implement rate limiting on authentication endpoints
6. **Monitoring**: Set up logging and monitoring for security events

### Performance
1. **Caching**: Implement caching for route configuration
2. **Connection Pooling**: Configure database connection pooling
3. **Load Balancing**: Deploy multiple instances behind a load balancer

### Reliability
1. **Health Checks**: Configure proper health checks
2. **Graceful Shutdown**: Ensure sessions are persisted during restart
3. **Database Backups**: Regular backups of session and configuration data

## Contributing

Contributions are welcome! Please follow these guidelines:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License.

## Documentation

- **[CONFIGURATION.md](CONFIGURATION.md)** - Complete configuration guide including OAuth provider setup and backend-initiated authentication with Keycloak
- **[UPSTREAM_TOKENS.md](UPSTREAM_TOKENS.md)** - Detailed documentation on upstream token management, security policies, and usage examples
- **[IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)** - Architecture overview and implementation details
- **[SECURITY.md](SECURITY.md)** - Security features and OWASP best practices
- **[ADMIN_API.md](ADMIN_API.md)** - Admin API endpoints for managing routes, clusters, and users

## References

- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [OWASP Cheat Sheet Series](https://cheatsheetseries.owasp.org/)
- [OAuth 2.0 Security Best Current Practice](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics)
- [Token Handler Pattern](https://curity.io/resources/learn/token-handler-overview/)
- [RFC 8693 - OAuth 2.0 Token Exchange](https://datatracker.ietf.org/doc/html/rfc8693)
