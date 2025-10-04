using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ApiGateway.Data;
using ApiGateway.Models;

namespace ApiGateway.Services;

public interface IUpstreamTokenService
{
    Task<string?> GetOrCreateUpstreamTokenAsync(string routeId, int? sessionId = null);
    Task RefreshUpstreamTokenAsync(string routeId, int? sessionId = null);
}

public class UpstreamTokenService : IUpstreamTokenService
{
    private readonly ApiGatewayDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UpstreamTokenService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;

    public UpstreamTokenService(
        ApiGatewayDbContext dbContext,
        IDistributedCache cache,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IJwtService jwtService,
        ILogger<UpstreamTokenService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<string?> GetOrCreateUpstreamTokenAsync(string routeId, int? sessionId = null)
    {
        // Try to get from cache first
        var cacheKey = GetCacheKey(routeId, sessionId);
        var cachedToken = await _cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cachedToken))
        {
            var tokenInfo = JsonSerializer.Deserialize<CachedTokenInfo>(cachedToken);
            if (tokenInfo != null && tokenInfo.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
            {
                return tokenInfo.AccessToken;
            }
        }

        // Get policy for the route
        var policy = await _dbContext.RoutePolicies
            .FirstOrDefaultAsync(p => p.RouteId == routeId);

        if (policy == null)
        {
            _logger.LogWarning("No security policy found for route {RouteId}", routeId);
            return null;
        }

        // Check if token exists in database and is still valid
        var upstreamToken = await _dbContext.UpstreamTokens
            .FirstOrDefaultAsync(t => t.RouteId == routeId && t.SessionId == sessionId);

        if (upstreamToken != null && upstreamToken.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
        {
            // Cache and return
            await CacheTokenAsync(cacheKey, upstreamToken.AccessToken, upstreamToken.ExpiresAt);
            return upstreamToken.AccessToken;
        }

        // Need to get a new token based on policy
        string? newToken = null;
        DateTime expiresAt = DateTime.UtcNow;

        switch (policy.SecurityType.ToLowerInvariant())
        {
            case "client_credentials":
                (newToken, expiresAt) = await GetClientCredentialsTokenAsync(policy);
                break;
            case "token_exchange":
                if (sessionId.HasValue)
                {
                    (newToken, expiresAt) = await GetTokenExchangeTokenAsync(policy, sessionId.Value);
                }
                break;
            case "self_signed":
                if (sessionId.HasValue)
                {
                    (newToken, expiresAt) = await GenerateSelfSignedTokenAsync(policy, sessionId.Value);
                }
                break;
            default:
                _logger.LogWarning("Unknown security type {SecurityType} for route {RouteId}", 
                    policy.SecurityType, routeId);
                return null;
        }

        if (string.IsNullOrEmpty(newToken))
        {
            return null;
        }

        // Store in database
        if (upstreamToken == null)
        {
            upstreamToken = new UpstreamToken
            {
                RouteId = routeId,
                SessionId = sessionId,
                AccessToken = newToken,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.UpstreamTokens.Add(upstreamToken);
        }
        else
        {
            upstreamToken.AccessToken = newToken;
            upstreamToken.ExpiresAt = expiresAt;
            upstreamToken.LastRefreshedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        // Cache the token
        await CacheTokenAsync(cacheKey, newToken, expiresAt);

        return newToken;
    }

    public async Task RefreshUpstreamTokenAsync(string routeId, int? sessionId = null)
    {
        var cacheKey = GetCacheKey(routeId, sessionId);
        await _cache.RemoveAsync(cacheKey);
        
        var upstreamToken = await _dbContext.UpstreamTokens
            .FirstOrDefaultAsync(t => t.RouteId == routeId && t.SessionId == sessionId);
        
        if (upstreamToken != null)
        {
            _dbContext.UpstreamTokens.Remove(upstreamToken);
            await _dbContext.SaveChangesAsync();
        }

        await GetOrCreateUpstreamTokenAsync(routeId, sessionId);
    }

    private async Task<(string? token, DateTime expiresAt)> GetClientCredentialsTokenAsync(RoutePolicy policy)
    {
        if (string.IsNullOrEmpty(policy.TokenEndpoint) || 
            string.IsNullOrEmpty(policy.ClientId) || 
            string.IsNullOrEmpty(policy.ClientSecret))
        {
            _logger.LogError("Client credentials configuration incomplete for route {RouteId}", policy.RouteId);
            return (null, DateTime.UtcNow);
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var requestData = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = policy.ClientId,
                ["client_secret"] = policy.ClientSecret
            };

            if (!string.IsNullOrEmpty(policy.Scope))
            {
                requestData["scope"] = policy.Scope;
            }

            var content = new FormUrlEncodedContent(requestData);
            var response = await httpClient.PostAsync(policy.TokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get client credentials token: {Error}", error);
                return (null, DateTime.UtcNow);
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse == null)
            {
                return (null, DateTime.UtcNow);
            }

            var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in);
            return (tokenResponse.access_token, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting client credentials token for route {RouteId}", policy.RouteId);
            return (null, DateTime.UtcNow);
        }
    }

    private async Task<(string? token, DateTime expiresAt)> GetTokenExchangeTokenAsync(RoutePolicy policy, int sessionId)
    {
        if (string.IsNullOrEmpty(policy.TokenEndpoint))
        {
            _logger.LogError("Token endpoint not configured for route {RouteId}", policy.RouteId);
            return (null, DateTime.UtcNow);
        }

        try
        {
            var session = await _dbContext.SessionTokens.FindAsync(sessionId);
            if (session == null)
            {
                _logger.LogError("Session {SessionId} not found for token exchange", sessionId);
                return (null, DateTime.UtcNow);
            }

            var httpClient = _httpClientFactory.CreateClient();
            var requestData = new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
                ["subject_token"] = session.AccessToken,
                ["subject_token_type"] = "urn:ietf:params:oauth:token-type:access_token"
            };

            if (!string.IsNullOrEmpty(policy.ClientId))
            {
                requestData["client_id"] = policy.ClientId;
            }

            if (!string.IsNullOrEmpty(policy.ClientSecret))
            {
                requestData["client_secret"] = policy.ClientSecret;
            }

            if (!string.IsNullOrEmpty(policy.Scope))
            {
                requestData["scope"] = policy.Scope;
            }

            var content = new FormUrlEncodedContent(requestData);
            var response = await httpClient.PostAsync(policy.TokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to exchange token: {Error}", error);
                return (null, DateTime.UtcNow);
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse == null)
            {
                return (null, DateTime.UtcNow);
            }

            var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in);
            return (tokenResponse.access_token, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging token for route {RouteId}", policy.RouteId);
            return (null, DateTime.UtcNow);
        }
    }

    private async Task<(string? token, DateTime expiresAt)> GenerateSelfSignedTokenAsync(RoutePolicy policy, int sessionId)
    {
        try
        {
            var session = await _dbContext.SessionTokens
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            
            if (session == null)
            {
                _logger.LogError("Session {SessionId} not found for self-signed token", sessionId);
                return (null, DateTime.UtcNow);
            }

            // Generate a JWT token with session info
            var claims = new Dictionary<string, string>
            {
                ["sub"] = session.User.Username,
                ["email"] = session.User.Email,
                ["session_id"] = session.Id.ToString(),
                ["route_id"] = policy.RouteId
            };

            var token = _jwtService.GenerateToken(claims, policy.TokenExpirationSeconds);
            var expiresAt = DateTime.UtcNow.AddSeconds(policy.TokenExpirationSeconds);
            
            return (token, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating self-signed token for route {RouteId}", policy.RouteId);
            return (null, DateTime.UtcNow);
        }
    }

    private static string GetCacheKey(string routeId, int? sessionId)
    {
        return sessionId.HasValue 
            ? $"upstream_token:{routeId}:{sessionId}" 
            : $"upstream_token:{routeId}:global";
    }

    private async Task CacheTokenAsync(string cacheKey, string token, DateTime expiresAt)
    {
        var tokenInfo = new CachedTokenInfo
        {
            AccessToken = token,
            ExpiresAt = expiresAt
        };

        var json = JsonSerializer.Serialize(tokenInfo);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiresAt
        };

        await _cache.SetStringAsync(cacheKey, json, options);
    }

    private class CachedTokenInfo
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    private class TokenResponse
    {
        public string access_token { get; set; } = string.Empty;
        public int expires_in { get; set; }
        public string token_type { get; set; } = string.Empty;
    }
}
