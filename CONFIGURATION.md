# Configuration Guide

This guide explains how to configure the API Gateway for different environments and use cases.

## Table of Contents

1. [Basic Configuration](#basic-configuration)
2. [OAuth Provider Setup](#oauth-provider-setup)
3. [Route Configuration](#route-configuration)
4. [Database Configuration](#database-configuration)
5. [CORS Configuration](#cors-configuration)
6. [Production Configuration](#production-configuration)

## Basic Configuration

The gateway is configured via `appsettings.json` and `appsettings.{Environment}.json` files.

### Minimal Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=apigateway.db"
  },
  "OAuth": {
    "Issuer": "https://your-auth-server.com",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "RedirectUri": "https://your-gateway.com/oauth/callback",
    "Scope": "openid profile email"
  }
}
```

## OAuth Provider Setup

The gateway supports any OpenID Connect provider through automatic discovery. You only need to configure the issuer URL - all endpoints (authorization, token, JWKS, etc.) are automatically discovered from the provider's well-known configuration endpoint.

### Microsoft Azure AD / Entra ID

1. Register an application in Azure Portal
2. Configure redirect URI: `https://your-gateway.com/oauth/callback`
3. Add API permissions
4. Create a client secret

```json
{
  "OAuth": {
    "Issuer": "https://login.microsoftonline.com/{tenant-id}/v2.0",
    "ClientId": "{application-id}",
    "ClientSecret": "{client-secret}",
    "RedirectUri": "https://your-gateway.com/oauth/callback",
    "Scope": "openid profile email"
  }
}
```

### Auth0

1. Create an application in Auth0 Dashboard
2. Configure allowed callback URLs
3. Get client credentials

```json
{
  "OAuth": {
    "Issuer": "https://{your-domain}.auth0.com",
    "ClientId": "{client-id}",
    "ClientSecret": "{client-secret}",
    "RedirectUri": "https://your-gateway.com/oauth/callback",
    "Scope": "openid profile email"
  }
}
```

### Okta

1. Create an application in Okta Admin Console
2. Choose "Web Application" type
3. Configure redirect URIs

```json
{
  "OAuth": {
    "Issuer": "https://{your-domain}.okta.com/oauth2/default",
    "ClientId": "{client-id}",
    "ClientSecret": "{client-secret}",
    "RedirectUri": "https://your-gateway.com/oauth/callback",
    "Scope": "openid profile email"
  }
}
```

### Google

1. Create OAuth 2.0 credentials in Google Cloud Console
2. Add authorized redirect URI
3. Get client ID and secret

```json
{
  "OAuth": {
    "Issuer": "https://accounts.google.com",
    "ClientId": "{client-id}.apps.googleusercontent.com",
    "ClientSecret": "{client-secret}",
    "RedirectUri": "https://your-gateway.com/oauth/callback",
    "Scope": "openid profile email"
  }
}
```

### Keycloak

1. Create a client in Keycloak Admin Console
2. Set Access Type to "confidential"
3. Configure valid redirect URIs

```json
{
  "OAuth": {
    "Issuer": "https://{keycloak-host}/realms/{realm}",
    "ClientId": "{client-id}",
    "ClientSecret": "{client-secret}",
    "RedirectUri": "https://your-gateway.com/oauth/callback",
    "Scope": "openid profile email"
  }
}
```

## OpenID Connect Authentication

The API Gateway uses OpenID Connect (OIDC) for authentication, which extends OAuth 2.0 with identity features. The implementation uses PKCE (Proof Key for Code Exchange) for enhanced security.

### Keycloak Configuration for OpenID Connect

#### 1. Create a Confidential Client in Keycloak

1. Log into Keycloak Admin Console
2. Navigate to **Clients** → **Create**
3. Configure the client:
   - **Client ID**: `api-gateway`
   - **Client Protocol**: `openid-connect`
   - **Access Type**: `confidential`
   - **Standard Flow Enabled**: `ON`
   - **Direct Access Grants Enabled**: `ON` (for backend token exchange)
   - **Service Accounts Enabled**: `ON` (for client credentials)
   - **Valid Redirect URIs**: 
     - `https://your-gateway.com/oauth/callback`
     - `http://localhost:5261/oauth/callback` (for development)
   - **Web Origins**: `https://your-gateway.com`

4. Save the client and note the **Client Secret** from the **Credentials** tab

#### 2. Configure Token Exchange (Optional)

To support token exchange for upstream services:

1. In Keycloak, go to **Realm Settings** → **Tokens**
2. Enable **Token Exchange**
3. Create a separate client for each upstream service that needs token exchange
4. Configure the token exchange policies in the **Service Account Roles** tab

#### 3. API Gateway Configuration

Update `appsettings.json`:

```json
{
  "OAuth": {
    "Issuer": "https://keycloak.example.com/realms/myrealm",
    "ClientId": "api-gateway",
    "ClientSecret": "your-client-secret-from-keycloak",
    "RedirectUri": "https://your-gateway.com/oauth/callback",
    "Scope": "openid profile email"
  },
  "Session": {
    "IdleTimeoutMinutes": 30,
    "AbsoluteTimeoutHours": 8
  },
  "Jwt": {
    "UsernameClaim": "preferred_username",
    "Secret": "change-this-to-a-secure-secret-key-at-least-32-characters-long",
    "Issuer": "api-gateway",
    "Audience": "api-gateway"
  }
}
```

**Important Configuration Notes:**

- **OAuth:Issuer**: The OIDC issuer URL. This is the **only required endpoint** configuration. The gateway automatically discovers all other endpoints (authorization, token, JWKS, etc.) from the well-known OIDC configuration at `{Issuer}/.well-known/openid-configuration`. For Keycloak, this is typically `https://keycloak.example.com/realms/{realm-name}`.
- **Automatic Endpoint Discovery**: The gateway automatically fetches:
  - Authorization endpoint for initiating login flows
  - Token endpoint for exchanging authorization codes
  - JWKS endpoint for validating token signatures
  - Other OIDC metadata as needed
- **Well-Known Discovery**: The gateway automatically fetches JWKS (JSON Web Key Set) and other OIDC metadata from the well-known endpoint for secure token validation.
- **Token Validation**: ID tokens are validated according to OWASP guidelines, including:
  - Signature verification using JWKS
  - Issuer validation
  - Audience validation
  - Expiration time validation
  - Nonce validation (replay protection)
  - Algorithm verification (RS256 or stronger)

#### 4. Token Validation Security

The API Gateway implements comprehensive OIDC token validation following OWASP security guidelines:

1. **Automatic OIDC Discovery**: Fetches configuration from `{Issuer}/.well-known/openid-configuration`
2. **Dynamic Endpoint Resolution**: All OAuth/OIDC endpoints (authorization, token, JWKS) are automatically discovered from the well-known endpoint
3. **JWKS-based Signature Validation**: Validates ID token signatures using public keys from the JWKS endpoint
4. **Issuer Validation**: Ensures tokens are issued by the configured identity provider
5. **Audience Validation**: Verifies tokens are intended for this gateway (client ID)
6. **Lifetime Validation**: Checks token expiration with configurable clock skew (5 minutes)
7. **Nonce Validation**: Validates nonce claim in ID token to prevent replay attacks
8. **Algorithm Verification**: Ensures only secure algorithms (RS256, RS384, RS512) are accepted

**Security Features:**
- Configuration cached for 24 hours with automatic refresh
- All validation follows OWASP Authentication Cheat Sheet guidelines
- Invalid tokens are rejected with detailed logging for security monitoring
- Reduced configuration surface area (only issuer URL needed)

#### 5. Configuring Route Security Policies

Routes can be configured with different security policies for upstream services:

**Security Types**:
- `none`: No authentication required
- `session`: Use the user's session token
- `client_credentials`: Use OAuth client credentials flow
- `token_exchange`: Exchange the user's token for an upstream token
- `self_signed`: Generate a self-signed JWT based on the session

**Database Configuration**:

```sql
-- Note: Table names are in PascalCase, but column names use snake_case
-- Add a route policy
INSERT INTO RoutePolicies (route_id, security_type, token_endpoint, client_id, client_secret, scope, token_expiration_seconds, created_at, updated_at)
VALUES (
  'api-route',
  'client_credentials',
  'https://keycloak.example.com/realms/myrealm/protocol/openid-connect/token',
  'backend-service',
  'backend-service-secret',
  'api.read api.write',
  3600,
  datetime('now'),
  datetime('now')
);

-- For token exchange
INSERT INTO RoutePolicies (route_id, security_type, token_endpoint, client_id, client_secret, scope, token_expiration_seconds, created_at, updated_at)
VALUES (
  'secure-api-route',
  'token_exchange',
  'https://keycloak.example.com/realms/myrealm/protocol/openid-connect/token',
  'api-gateway',
  'your-client-secret',
  'downstream-service',
  3600,
  datetime('now'),
  datetime('now')
);

-- For self-signed tokens (no external OAuth needed)
INSERT INTO RoutePolicies (route_id, security_type, token_endpoint, client_id, client_secret, scope, token_expiration_seconds, created_at, updated_at)
VALUES (
  'internal-api-route',
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

**Token Management**:
- Upstream tokens are automatically cached in distributed cache (default: in-memory)
- Tokens are automatically refreshed when they are about to expire (5 minutes before expiration)
- Session information is stored in the database for persistence
- Tokens can be manually refreshed using the `IUpstreamTokenService`

**Example Backend Usage**:

```csharp
// In your backend service
var upstreamToken = await _upstreamTokenService.GetOrCreateUpstreamTokenAsync("api-route", sessionId);

// Use the token to call the upstream service
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", upstreamToken);
var response = await httpClient.GetAsync("https://upstream-service.com/api/resource");
```

## Route Configuration

Routes are stored in the database and can be managed dynamically.

### Adding Routes via Database

Connect to the SQLite database and insert route configurations:

```sql
-- Add a new cluster (backend service)
INSERT INTO ClusterConfigs (ClusterId, DestinationAddress, IsActive, CreatedAt, UpdatedAt)
VALUES ('user-service', 'https://api.example.com/users', 1, datetime('now'), datetime('now'));

-- Add a route to the cluster
INSERT INTO RouteConfigs (RouteId, ClusterId, Match, "Order", IsActive, CreatedAt, UpdatedAt)
VALUES ('user-route', 'user-service', '/users/{**catch-all}', 1, 1, datetime('now'), datetime('now'));
```

### Route Patterns

YARP supports various route patterns:

**Exact Match:**
```sql
Match = '/api/users'
```

**Prefix Match:**
```sql
Match = '/api/{**catch-all}'
```

**Path Parameters:**
```sql
Match = '/api/users/{id}'
```

**Query Parameters:**
Routes can be further filtered by query parameters using YARP's query match features (requires additional configuration).

### Route Priority

Routes are evaluated in order. Lower `Order` values have higher priority:

```sql
-- High priority (evaluated first)
INSERT INTO RouteConfigs (RouteId, ClusterId, Match, "Order", IsActive, CreatedAt, UpdatedAt)
VALUES ('specific-route', 'service-a', '/api/users/special', 1, 1, datetime('now'), datetime('now'));

-- Lower priority (evaluated after)
INSERT INTO RouteConfigs (RouteId, ClusterId, Match, "Order", IsActive, CreatedAt, UpdatedAt)
VALUES ('general-route', 'service-b', '/api/{**catch-all}', 100, 1, datetime('now'), datetime('now'));
```

### Managing Routes Programmatically

You can create an admin API to manage routes:

```csharp
[ApiController]
[Route("admin/routes")]
public class RouteManagementController : ControllerBase
{
    private readonly ApiGatewayDbContext _dbContext;
    private readonly DatabaseProxyConfigProvider _proxyConfigProvider;

    [HttpPost]
    public async Task<IActionResult> CreateRoute([FromBody] RouteConfigDto dto)
    {
        var route = new RouteConfig
        {
            RouteId = dto.RouteId,
            ClusterId = dto.ClusterId,
            Match = dto.Match,
            Order = dto.Order,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.RouteConfigs.Add(route);
        await _dbContext.SaveChangesAsync();

        // Reload YARP configuration
        _proxyConfigProvider.Reload();

        return Ok(route);
    }
}
```

## Database Configuration

### SQLite (Default - Development)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=apigateway.db"
  }
}
```

### PostgreSQL (Recommended for Production)

1. Install NuGet package:
   ```bash
   dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
   ```

2. Update `Program.cs`:
   ```csharp
   builder.Services.AddDbContext<ApiGatewayDbContext>(options =>
       options.UseNpgsql(
           builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

3. Configure connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=apigateway;Username=user;Password=password;SSL Mode=Require"
     }
   }
   ```

### SQL Server

1. Install NuGet package:
   ```bash
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   ```

2. Update `Program.cs`:
   ```csharp
   builder.Services.AddDbContext<ApiGatewayDbContext>(options =>
       options.UseSqlServer(
           builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

3. Configure connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=ApiGateway;User Id=user;Password=password;Encrypt=true;TrustServerCertificate=false"
     }
   }
   ```

### MySQL/MariaDB

1. Install NuGet package:
   ```bash
   dotnet add package Pomelo.EntityFrameworkCore.MySql
   ```

2. Update `Program.cs`:
   ```csharp
   builder.Services.AddDbContext<ApiGatewayDbContext>(options =>
       options.UseMySql(
           builder.Configuration.GetConnectionString("DefaultConnection"),
           ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));
   ```

3. Configure connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=apigateway;User=user;Password=password;SslMode=Required"
     }
   }
   ```

## CORS Configuration

Configure allowed origins for your frontend applications:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://app.example.com",
      "https://admin.example.com",
      "http://localhost:3000"
    ]
  }
}
```

### Environment-Specific CORS

**appsettings.Development.json:**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:4200",
      "http://localhost:8080"
    ]
  }
}
```

**appsettings.Production.json:**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://app.example.com",
      "https://www.example.com"
    ]
  }
}
```

## Production Configuration

### appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Yarp": "Warning",
      "ApiGateway": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "${DATABASE_CONNECTION_STRING}"
  },
  "OAuth": {
    "Issuer": "${OAUTH_ISSUER}",
    "ClientId": "${OAUTH_CLIENT_ID}",
    "ClientSecret": "${OAUTH_CLIENT_SECRET}",
    "RedirectUri": "https://gateway.example.com/oauth/callback",
    "Scope": "openid profile email"
  },
  "Cors": {
    "AllowedOrigins": [
      "https://app.example.com"
    ]
  }
}
```

### Environment Variables

Set sensitive configuration via environment variables:

**Linux/macOS:**
```bash
export DATABASE_CONNECTION_STRING="Host=prod-db;Database=apigateway;..."
export OAUTH_ISSUER="https://auth.example.com"
export OAUTH_CLIENT_SECRET="your-secret"
export ASPNETCORE_ENVIRONMENT="Production"
```

**Windows:**
```powershell
$env:DATABASE_CONNECTION_STRING="Host=prod-db;Database=apigateway;..."
$env:OAUTH_ISSUER="https://auth.example.com"
$env:OAUTH_CLIENT_SECRET="your-secret"
$env:ASPNETCORE_ENVIRONMENT="Production"
```

**Docker:**
```dockerfile
ENV DATABASE_CONNECTION_STRING="Host=prod-db;Database=apigateway;..."
ENV OAUTH_ISSUER="https://auth.example.com"
ENV OAUTH_CLIENT_SECRET="your-secret"
ENV ASPNETCORE_ENVIRONMENT="Production"
```

### Kubernetes Configuration

**ConfigMap (non-sensitive):**
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: apigateway-config
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  OAuth__Issuer: "https://auth.example.com"
  OAuth__RedirectUri: "https://gateway.example.com/oauth/callback"
```

**Secret (sensitive):**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: apigateway-secrets
type: Opaque
stringData:
  DATABASE_CONNECTION_STRING: "Host=prod-db;Database=apigateway;..."
  OAuth__ClientSecret: "your-secret"
```

### Azure App Service

Configure in Azure Portal under Configuration > Application Settings:

| Name | Value |
|------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | `Server=...` |
| `OAuth__ClientSecret` | `***` |

Or use Azure Key Vault references:
```
@Microsoft.KeyVault(VaultName=my-vault;SecretName=oauth-client-secret)
```

### AWS Elastic Beanstalk

Configure in `.ebextensions/environment.config`:

```yaml
option_settings:
  - namespace: aws:elasticbeanstalk:application:environment
    option_name: ASPNETCORE_ENVIRONMENT
    value: Production
  - namespace: aws:elasticbeanstalk:application:environment
    option_name: OAuth__ClientSecret
    value: your-secret
```

Or use AWS Systems Manager Parameter Store:
```csharp
var clientSecret = await ssm.GetParameterAsync(new GetParameterRequest
{
    Name = "/apigateway/oauth/client-secret",
    WithDecryption = true
});
```

## Logging Configuration

### Console Logging (Development)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Yarp": "Information",
      "ApiGateway.Services": "Debug"
    }
  }
}
```

### Structured Logging (Production)

Install Serilog:
```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
```

Configure in `Program.cs`:
```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
```

Configure in `appsettings.json`:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/apigateway-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

## Health Checks

The gateway includes a basic health check at `/health`. Configure advanced health checks:

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApiGatewayDbContext>()
    .AddUrlGroup(new Uri("https://backend-api.com/health"), "Backend API")
    .AddCheck("OAuth Server", () =>
    {
        // Check if OAuth server is reachable
        return HealthCheckResult.Healthy();
    });

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        }));
    }
});
```

## Performance Tuning

### Connection Pooling

```csharp
builder.Services.AddDbContext<ApiGatewayDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MaxBatchSize(100);
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(3);
    }));
```

### Response Caching

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### Output Caching (.NET 9)

```csharp
builder.Services.AddOutputCache();
app.UseOutputCache();

app.MapGet("/oauth/isloggedin", ...)
   .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(10)));
```

## Troubleshooting

### Enable Detailed Errors

```json
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### Database Connection Issues

Test connection:
```bash
dotnet ef database update --verbose
```

### OAuth Issues

Enable OAuth logging:
```json
{
  "Logging": {
    "LogLevel": {
      "ApiGateway.Services.OAuthAgentService": "Debug"
    }
  }
}
```

### YARP Issues

Enable YARP logging:
```json
{
  "Logging": {
    "LogLevel": {
      "Yarp": "Debug"
    }
  }
}
```

## Next Steps

- Review [SECURITY.md](SECURITY.md) for security best practices
- Set up monitoring and alerting
- Configure backups
- Implement rate limiting
- Add custom authentication policies
- Extend with additional middleware
