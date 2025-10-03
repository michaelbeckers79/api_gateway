# Security Features Implementation

This document outlines the OWASP security best practices implemented in the API Gateway.

## OWASP Top 10 2021 Mitigations

### A01:2021 – Broken Access Control

**Implemented Mitigations:**
- ✅ Session validation middleware enforces authentication on protected routes
- ✅ Session binding to IP address and User-Agent for additional security
- ✅ Token revocation capability with `IsRevoked` flag
- ✅ Public/private route separation with whitelist approach
- ✅ Session timeouts: 30-minute idle timeout, 8-hour absolute timeout
- ✅ Automatic session cleanup via background service

**Code References:**
- `Middleware/SessionValidationMiddleware.cs` - Enforces authentication
- `Services/SessionTokenService.cs` - Session management and validation

### A02:2021 – Cryptographic Failures

**Implemented Mitigations:**
- ✅ ASP.NET Core Data Protection API for cookie encryption
- ✅ Cryptographically secure random number generation (256-bit tokens)
- ✅ HTTPS enforcement via `UseHttpsRedirection()`
- ✅ Secure storage of OAuth tokens (server-side only, never in browser)
- ✅ PKCE (Proof Key for Code Exchange) for OAuth authorization code flow

**Code References:**
- `Services/SessionTokenService.cs` - `GenerateSecureToken()` method
- `Services/OAuthAgentService.cs` - PKCE implementation
- `Program.cs` - Data Protection and HTTPS configuration

### A03:2021 – Injection

**Implemented Mitigations:**
- ✅ Entity Framework Core with parameterized queries (automatic)
- ✅ Input validation on controller endpoints
- ✅ Type-safe data models

**Code References:**
- `Data/ApiGatewayDbContext.cs` - EF Core context
- `Controllers/AuthController.cs` - Strongly-typed request/response DTOs

### A05:2021 – Security Misconfiguration

**Implemented Mitigations:**
- ✅ Secure cookie configuration:
  - `HttpOnly=true` - Prevents XSS cookie theft
  - `Secure=true` - HTTPS-only transmission
  - `SameSite=Strict` - CSRF protection
  - `__Host-` prefix - Additional CSRF protection (no Domain attribute)
- ✅ Security headers:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `X-XSS-Protection: 1; mode=block`
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Content-Security-Policy` with restrictive defaults
- ✅ CORS properly configured with explicit allowed origins
- ✅ No sensitive data in error messages

**Code References:**
- `Controllers/AuthController.cs` - Cookie configuration constants
- `Program.cs` - Security headers middleware

### A07:2021 – Identification and Authentication Failures

**Implemented Mitigations:**
- ✅ Strong session token generation (256-bit cryptographically random)
- ✅ Session timeouts (absolute and idle)
- ✅ PKCE implementation for OAuth (prevents authorization code interception)
- ✅ State parameter validation (CSRF protection for OAuth flow)
- ✅ Opaque session tokens (no user information leaked)
- ✅ Session revocation on logout
- ✅ Automatic cleanup of expired sessions

**Code References:**
- `Services/SessionTokenService.cs` - Session management
- `Services/OAuthAgentService.cs` - OAuth security (PKCE, state)
- `Controllers/AuthController.cs` - State validation in callback

### A08:2021 – Software and Data Integrity Failures

**Implemented Mitigations:**
- ✅ Data Protection API for cookie integrity
- ✅ State parameter validation ensures OAuth flow integrity
- ✅ Code verifier validation in token exchange (PKCE)
- ✅ Encrypted cookie content prevents tampering

**Code References:**
- `Controllers/AuthController.cs` - State and code verifier validation
- `Program.cs` - Data Protection configuration

## Cookie Security Implementation

### Cookie Names with __Host- Prefix

The `__Host-` prefix is a cookie security feature that:
1. Requires the `Secure` flag to be set (HTTPS only)
2. Prevents setting a `Domain` attribute
3. Sets `Path` to `/` (entire site)

This provides additional CSRF protection by ensuring cookies can't be overwritten by subdomains.

**Implementation:**
```csharp
private const string SessionCookieName = "__Host-Session";
private const string StateCookieName = "__Host-State";
private const string CodeVerifierCookieName = "__Host-CodeVerifier";
```

### Cookie Options Configuration

All cookies use secure configuration:
```csharp
var cookieOptions = new CookieOptions
{
    HttpOnly = true,        // Not accessible to JavaScript
    Secure = true,          // HTTPS only
    SameSite = SameSiteMode.Strict,  // CSRF protection
    MaxAge = TimeSpan.FromHours(8),  // Limited lifetime
    Path = "/",             // Site-wide
    IsEssential = true      // Required for functionality
};
```

## Token Handler Pattern

The Token Handler Pattern is implemented to keep OAuth tokens secure:

1. **Frontend never sees OAuth tokens**
   - Access tokens and refresh tokens are stored server-side
   - Only opaque session tokens are sent to the client
   
2. **Opaque Session Tokens**
   - 256-bit cryptographically random tokens
   - No embedded information
   - Stored in encrypted cookies
   
3. **Token Storage**
   - OAuth tokens stored in database
   - Associated with session token
   - Retrieved for API calls via middleware
   
4. **Token Lifecycle**
   - Created during OAuth callback
   - Validated on each request
   - Revoked on logout
   - Automatically cleaned up when expired

**Implementation Flow:**
```
1. User initiates login → OAuth agent generates auth request
2. User completes OAuth flow → Gateway receives authorization code
3. Gateway exchanges code for tokens → Stores in database
4. Gateway creates session token → Sends encrypted cookie to client
5. Client makes API call → Gateway validates session → Retrieves OAuth token
6. Gateway calls backend API → Uses stored OAuth access token
```

## PKCE (Proof Key for Code Exchange)

PKCE protects against authorization code interception attacks:

1. **Code Verifier Generation**
   - 256-bit cryptographically random string
   - Base64 URL-encoded
   
2. **Code Challenge Creation**
   - SHA256 hash of code verifier
   - Base64 URL-encoded
   - Sent in authorization request
   
3. **Code Verifier Validation**
   - Original verifier sent in token exchange
   - Authorization server validates hash matches
   
4. **Storage**
   - Code verifier stored in encrypted cookie during flow
   - Deleted after token exchange

**Implementation:**
```csharp
// Generate
var codeVerifier = GenerateCodeVerifier();
var codeChallenge = GenerateCodeChallenge(codeVerifier);

// Store securely
var encryptedCodeVerifier = _protector.Protect(codeVerifier);
Response.Cookies.Append(CodeVerifierCookieName, encryptedCodeVerifier, cookieOptions);

// Use in token exchange
var codeVerifier = _protector.Unprotect(encryptedCodeVerifier);
await _oauthAgent.ExchangeCodeForTokensAsync(code, codeVerifier);
```

## Session Management

### Timeout Strategy

**Absolute Timeout (8 hours):**
- Maximum session lifetime regardless of activity
- Prevents indefinite sessions
- Stored as `ExpiresAt` timestamp

**Idle Timeout (30 minutes):**
- Tracks last activity via `LastAccessedAt`
- Session invalidated after 30 minutes of inactivity
- Updated on each validated request

**Implementation:**
```csharp
// Check absolute timeout
if (session.ExpiresAt < DateTime.UtcNow)
{
    await RevokeSessionAsync(tokenId);
    return null;
}

// Check idle timeout
if (session.LastAccessedAt.HasValue && 
    DateTime.UtcNow - session.LastAccessedAt.Value > _sessionTimeout)
{
    await RevokeSessionAsync(tokenId);
    return null;
}
```

### Session Binding

Sessions are bound to client characteristics:
- IP Address
- User-Agent

This provides defense-in-depth against session hijacking. If an attacker steals a session cookie, the binding can help detect anomalies.

**Note:** IP binding should be used carefully in production due to:
- Legitimate IP changes (mobile networks, VPNs)
- Load balancers and proxies
- IPv6 address rotation

Consider implementing soft binding (log anomalies) rather than hard enforcement.

## Background Services

### Session Cleanup Service

Runs hourly to remove:
- Expired sessions (`ExpiresAt < now`)
- Revoked sessions (`IsRevoked = true`)

**Benefits:**
- Reduces database size
- Prevents token leakage from old sessions
- Improves query performance

**Implementation:**
```csharp
public class SessionCleanupBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_cleanupInterval, stoppingToken);
            await sessionTokenService.CleanupExpiredSessionsAsync();
        }
    }
}
```

## Production Considerations

### Key Management

In production, configure Data Protection to persist keys securely:

**Azure:**
```csharp
services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(new Uri("<blob-storage-uri>"))
    .ProtectKeysWithAzureKeyVault(new Uri("<key-vault-key-uri>"), credential);
```

**AWS:**
```csharp
services.AddDataProtection()
    .PersistKeysToAWSSystemsManager("<parameter-name>")
    .ProtectKeysWithAwsKms("<kms-key-id>");
```

**File System (shared across instances):**
```csharp
services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"\\shared-network-path\keys"))
    .ProtectKeysWithDpapi();
```

### Rate Limiting

Implement rate limiting on authentication endpoints to prevent:
- Brute force attacks
- Denial of service

**Recommended:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", config =>
    {
        config.PermitLimit = 5;
        config.Window = TimeSpan.FromMinutes(1);
    });
});

// Apply to auth endpoints
app.MapPost("/oauth/login/start", ...)
   .RequireRateLimiting("auth");
```

### Database Security

In production:
1. Use a production database (PostgreSQL, SQL Server, MySQL)
2. Enable SSL/TLS for database connections
3. Use connection string encryption
4. Implement database access controls and least privilege
5. Regular backups with encryption at rest
6. Monitor for anomalous queries

### Monitoring and Alerting

Implement monitoring for:
- Failed authentication attempts
- Session creation/revocation rates
- Token validation failures
- Suspicious session activities (IP/UA changes)
- OAuth errors

### Secrets Management

Never commit secrets to source control:
- Use environment variables
- Use secret management services (Azure Key Vault, AWS Secrets Manager)
- Rotate secrets regularly
- Use different secrets per environment

## Testing Security

### Security Testing Checklist

- [ ] Verify HTTPS enforcement
- [ ] Test cookie attributes (HttpOnly, Secure, SameSite)
- [ ] Verify security headers present
- [ ] Test session timeout behavior
- [ ] Verify state parameter validation
- [ ] Test PKCE flow
- [ ] Verify session revocation
- [ ] Test unauthorized access attempts
- [ ] Check for sensitive data in logs/errors
- [ ] Verify CORS configuration
- [ ] Test XSS protections
- [ ] Verify CSRF protections

### Automated Security Scanning

Consider integrating:
- OWASP ZAP for dynamic security testing
- SonarQube for static code analysis
- Snyk for dependency vulnerability scanning
- GitHub Security Scanning (Dependabot, CodeQL)

## References

- [OWASP Cheat Sheet Series](https://cheatsheetseries.owasp.org/)
- [OAuth 2.0 Security Best Current Practice](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics)
- [OWASP Session Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html)
- [Token Handler Pattern](https://curity.io/resources/learn/token-handler-overview/)
- [PKCE RFC 7636](https://datatracker.ietf.org/doc/html/rfc7636)
- [Cookie Prefixes](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Set-Cookie#cookie_prefixes)
