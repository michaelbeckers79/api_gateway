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
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _absoluteTimeout = TimeSpan.FromHours(8);

    public SessionTokenService(
        ApiGatewayDbContext dbContext,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<SessionTokenService> logger)
    {
        _dbContext = dbContext;
        _protector = dataProtectionProvider.CreateProtector("SessionTokens");
        _logger = logger;
    }

    public async Task<string> CreateSessionAsync(string userId, string accessToken, 
        string refreshToken, string ipAddress, string userAgent)
    {
        // Generate cryptographically secure random token ID
        var tokenId = GenerateSecureToken();

        var session = new SessionToken
        {
            TokenId = tokenId,
            UserId = userId,
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

        _logger.LogInformation("Created session token for user {UserId} from IP {IpAddress}", 
            userId, ipAddress);

        return tokenId;
    }

    public async Task<SessionToken?> GetSessionAsync(string tokenId)
    {
        var session = await _dbContext.SessionTokens
            .FirstOrDefaultAsync(s => s.TokenId == tokenId && !s.IsRevoked);

        if (session == null)
        {
            _logger.LogWarning("Session token {TokenId} not found", tokenId);
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
