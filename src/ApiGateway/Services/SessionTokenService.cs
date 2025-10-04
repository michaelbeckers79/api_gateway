using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using ApiGateway.Data;
using ApiGateway.Models;

namespace ApiGateway.Services;

public interface ISessionTokenService
{
    Task<string> CreateSessionAsync(string userId, string accessToken, string refreshToken, 
        string ipAddress, string userAgent);
    Task<SessionToken?> GetSessionAsync(string tokenId);
    Task<bool> ValidateSessionAsync(string tokenId);
    Task RevokeSessionAsync(string tokenId);
    Task CleanupExpiredSessionsAsync();
}

public class SessionTokenService : ISessionTokenService
{
    private readonly ApiGatewayDbContext _dbContext;
    private readonly IDataProtector _protector;
    private readonly ILogger<SessionTokenService> _logger;
    private readonly TimeSpan _sessionTimeout;
    private readonly TimeSpan _absoluteTimeout;

    public SessionTokenService(
        ApiGatewayDbContext dbContext,
        IDataProtectionProvider dataProtectionProvider,
        IConfiguration configuration,
        ILogger<SessionTokenService> logger)
    {
        _dbContext = dbContext;
        _protector = dataProtectionProvider.CreateProtector("SessionTokens");
        _logger = logger;
        
        // Read timeout configuration from appsettings, with defaults
        var idleTimeoutMinutes = configuration.GetValue<int?>("Session:IdleTimeoutMinutes") ?? 30;
        var absoluteTimeoutHours = configuration.GetValue<int?>("Session:AbsoluteTimeoutHours") ?? 8;
        
        _sessionTimeout = TimeSpan.FromMinutes(idleTimeoutMinutes);
        _absoluteTimeout = TimeSpan.FromHours(absoluteTimeoutHours);
    }

    public async Task<string> CreateSessionAsync(string userId, string accessToken, 
        string refreshToken, string ipAddress, string userAgent)
    {
        // userId is actually the username from JWT, need to look up or create user
        var username = userId;
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        
        if (user == null)
        {
            _logger.LogError("User not found for username: {Username}", username);
            throw new InvalidOperationException($"User not found: {username}");
        }

        if (!user.IsEnabled)
        {
            _logger.LogWarning("Attempted to create session for disabled user: {Username}", username);
            throw new UnauthorizedAccessException($"User is disabled: {username}");
        }

        // Generate cryptographically secure random token ID
        var tokenId = GenerateSecureToken();

        var session = new SessionToken
        {
            TokenId = tokenId,
            UserId = user.Id,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.Add(_absoluteTimeout),
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsRevoked = false
        };

        _dbContext.SessionTokens.Add(session);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created session token for user {Username} (ID: {UserId}) from IP {IpAddress}", 
            username, user.Id, ipAddress);

        return tokenId;
    }

    public async Task<SessionToken?> GetSessionAsync(string tokenId)
    {
        var session = await _dbContext.SessionTokens
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.TokenId == tokenId && !s.IsRevoked);

        if (session == null)
        {
            _logger.LogWarning("Session token {TokenId} not found", tokenId);
            return null;
        }

        // Check if user is enabled
        if (!session.User.IsEnabled)
        {
            _logger.LogWarning("Session token {TokenId} belongs to disabled user {Username}", 
                tokenId, session.User.Username);
            await RevokeSessionAsync(tokenId);
            return null;
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Session token {TokenId} has expired", tokenId);
            await RevokeSessionAsync(tokenId);
            return null;
        }

        // Check idle timeout
        if (session.LastAccessedAt.HasValue && 
            DateTime.UtcNow - session.LastAccessedAt.Value > _sessionTimeout)
        {
            _logger.LogWarning("Session token {TokenId} exceeded idle timeout", tokenId);
            await RevokeSessionAsync(tokenId);
            return null;
        }

        // Update last accessed time
        session.LastAccessedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return session;
    }

    public async Task<bool> ValidateSessionAsync(string tokenId)
    {
        var session = await GetSessionAsync(tokenId);
        return session != null;
    }

    public async Task RevokeSessionAsync(string tokenId)
    {
        var session = await _dbContext.SessionTokens
            .FirstOrDefaultAsync(s => s.TokenId == tokenId);

        if (session != null)
        {
            session.IsRevoked = true;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Revoked session token {TokenId}", tokenId);
        }
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        var expiredSessions = await _dbContext.SessionTokens
            .Where(s => s.ExpiresAt < DateTime.UtcNow || s.IsRevoked)
            .ToListAsync();

        _dbContext.SessionTokens.RemoveRange(expiredSessions);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
    }

    private static string GenerateSecureToken()
    {
        // Generate 32 bytes (256 bits) of cryptographically secure random data
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        
        // Convert to base64 URL-safe string
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
