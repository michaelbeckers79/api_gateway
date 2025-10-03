using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services;
using ApiGateway.Models;
using ApiGateway.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly ApiGatewayDbContext _dbContext;
    private readonly IClientCredentialService _clientCredentialService;
    private readonly IUserService _userService;
    private readonly ISessionTokenService _sessionTokenService;
    private readonly DatabaseProxyConfigProvider _proxyConfigProvider;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApiGatewayDbContext dbContext,
        IClientCredentialService clientCredentialService,
        IUserService userService,
        ISessionTokenService sessionTokenService,
        DatabaseProxyConfigProvider proxyConfigProvider,
        ILogger<AdminController> logger)
    {
        _dbContext = dbContext;
        _clientCredentialService = clientCredentialService;
        _userService = userService;
        _sessionTokenService = sessionTokenService;
        _proxyConfigProvider = proxyConfigProvider;
        _logger = logger;
    }

    private async Task<bool> ValidateClientCredentialsAsync()
    {
        // Check for Authorization header
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            _logger.LogWarning("Missing Authorization header");
            return false;
        }

        var authValue = authHeader.ToString();
        if (!authValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Invalid Authorization header format");
            return false;
        }

        try
        {
            var encodedCredentials = authValue.Substring(6);
            var decodedCredentials = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var parts = decodedCredentials.Split(':', 2);

            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid credentials format");
                return false;
            }

            var clientId = parts[0];
            var clientSecret = parts[1];

            return await _clientCredentialService.ValidateClientCredentialsAsync(clientId, clientSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating client credentials");
            return false;
        }
    }

    // Routes Management
    [HttpGet("routes")]
    public async Task<IActionResult> GetRoutes()
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var routes = await _dbContext.RouteConfigs.ToListAsync();
        return Ok(routes);
    }

    [HttpPost("routes")]
    public async Task<IActionResult> CreateRoute([FromBody] RouteConfigDto dto)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var route = new RouteConfig
        {
            RouteId = dto.RouteId,
            ClusterId = dto.ClusterId,
            Match = dto.Match,
            Order = dto.Order,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.RouteConfigs.Add(route);
        await _dbContext.SaveChangesAsync();

        // Reload YARP configuration
        _proxyConfigProvider.Reload();

        _logger.LogInformation("Created route {RouteId}", route.RouteId);
        return Ok(route);
    }

    [HttpPut("routes/{id}")]
    public async Task<IActionResult> UpdateRoute(int id, [FromBody] RouteConfigDto dto)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var route = await _dbContext.RouteConfigs.FindAsync(id);
        if (route == null)
            return NotFound();

        route.RouteId = dto.RouteId;
        route.ClusterId = dto.ClusterId;
        route.Match = dto.Match;
        route.Order = dto.Order;
        route.IsActive = dto.IsActive;
        route.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        // Reload YARP configuration
        _proxyConfigProvider.Reload();

        _logger.LogInformation("Updated route {RouteId}", route.RouteId);
        return Ok(route);
    }

    [HttpDelete("routes/{id}")]
    public async Task<IActionResult> DeleteRoute(int id)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var route = await _dbContext.RouteConfigs.FindAsync(id);
        if (route == null)
            return NotFound();

        _dbContext.RouteConfigs.Remove(route);
        await _dbContext.SaveChangesAsync();

        // Reload YARP configuration
        _proxyConfigProvider.Reload();

        _logger.LogInformation("Deleted route {RouteId}", route.RouteId);
        return Ok(new { message = "Route deleted" });
    }

    // Clusters Management
    [HttpGet("clusters")]
    public async Task<IActionResult> GetClusters()
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var clusters = await _dbContext.ClusterConfigs.ToListAsync();
        return Ok(clusters);
    }

    [HttpPost("clusters")]
    public async Task<IActionResult> CreateCluster([FromBody] ClusterConfigDto dto)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var cluster = new ClusterConfig
        {
            ClusterId = dto.ClusterId,
            DestinationAddress = dto.DestinationAddress,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.ClusterConfigs.Add(cluster);
        await _dbContext.SaveChangesAsync();

        // Reload YARP configuration
        _proxyConfigProvider.Reload();

        _logger.LogInformation("Created cluster {ClusterId}", cluster.ClusterId);
        return Ok(cluster);
    }

    [HttpPut("clusters/{id}")]
    public async Task<IActionResult> UpdateCluster(int id, [FromBody] ClusterConfigDto dto)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var cluster = await _dbContext.ClusterConfigs.FindAsync(id);
        if (cluster == null)
            return NotFound();

        cluster.ClusterId = dto.ClusterId;
        cluster.DestinationAddress = dto.DestinationAddress;
        cluster.IsActive = dto.IsActive;
        cluster.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        // Reload YARP configuration
        _proxyConfigProvider.Reload();

        _logger.LogInformation("Updated cluster {ClusterId}", cluster.ClusterId);
        return Ok(cluster);
    }

    [HttpDelete("clusters/{id}")]
    public async Task<IActionResult> DeleteCluster(int id)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var cluster = await _dbContext.ClusterConfigs.FindAsync(id);
        if (cluster == null)
            return NotFound();

        _dbContext.ClusterConfigs.Remove(cluster);
        await _dbContext.SaveChangesAsync();

        // Reload YARP configuration
        _proxyConfigProvider.Reload();

        _logger.LogInformation("Deleted cluster {ClusterId}", cluster.ClusterId);
        return Ok(new { message = "Cluster deleted" });
    }

    // Users Management
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var users = await _userService.GetAllUsersAsync();
        return Ok(users.Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            u.IsEnabled,
            u.CreatedAt,
            u.UpdatedAt,
            u.LastLoginAt,
            ActiveSessions = u.Sessions.Count(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
        }));
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var user = await _dbContext.Users
            .Include(u => u.Sessions.Where(s => !s.IsRevoked))
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.IsEnabled,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt,
            Sessions = user.Sessions.Select(s => new
            {
                s.Id,
                s.TokenId,
                s.CreatedAt,
                s.LastAccessedAt,
                s.ExpiresAt,
                s.IpAddress,
                s.UserAgent,
                IsExpired = s.ExpiresAt < DateTime.UtcNow
            })
        });
    }

    [HttpPost("users/{id}/enable")]
    public async Task<IActionResult> EnableUser(int id)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        await _userService.EnableUserAsync(id);
        _logger.LogInformation("Enabled user {UserId}", id);
        return Ok(new { message = "User enabled" });
    }

    [HttpPost("users/{id}/disable")]
    public async Task<IActionResult> DisableUser(int id)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        await _userService.DisableUserAsync(id);
        _logger.LogInformation("Disabled user {UserId}", id);
        return Ok(new { message = "User disabled" });
    }

    // Session Management
    [HttpGet("users/{userId}/sessions")]
    public async Task<IActionResult> GetUserSessions(int userId)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var sessions = await _dbContext.SessionTokens
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.TokenId,
                s.CreatedAt,
                s.LastAccessedAt,
                s.ExpiresAt,
                s.IpAddress,
                s.UserAgent,
                s.IsRevoked,
                IsExpired = s.ExpiresAt < DateTime.UtcNow
            })
            .ToListAsync();

        return Ok(sessions);
    }

    [HttpPost("sessions/{sessionId}/revoke")]
    public async Task<IActionResult> RevokeSession(int sessionId)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var session = await _dbContext.SessionTokens.FindAsync(sessionId);
        if (session == null)
            return NotFound();

        await _sessionTokenService.RevokeSessionAsync(session.TokenId);
        _logger.LogInformation("Revoked session {SessionId}", sessionId);
        return Ok(new { message = "Session revoked" });
    }

    [HttpPost("users/{userId}/sessions/revoke-all")]
    public async Task<IActionResult> RevokeAllUserSessions(int userId)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var sessions = await _dbContext.SessionTokens
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync();

        foreach (var session in sessions)
        {
            await _sessionTokenService.RevokeSessionAsync(session.TokenId);
        }

        _logger.LogInformation("Revoked all sessions for user {UserId}, count: {Count}", userId, sessions.Count);
        return Ok(new { message = $"Revoked {sessions.Count} sessions" });
    }

    // Client Credentials Management
    [HttpGet("clients")]
    public async Task<IActionResult> GetClients()
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        var clients = await _clientCredentialService.GetAllClientsAsync();
        return Ok(clients.Select(c => new
        {
            c.Id,
            c.ClientId,
            c.Description,
            c.IsEnabled,
            c.CreatedAt,
            c.LastUsedAt
        }));
    }

    [HttpPost("clients")]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        try
        {
            var client = await _clientCredentialService.CreateClientAsync(
                request.ClientId,
                request.ClientSecret,
                request.Description);

            return Ok(new
            {
                client.Id,
                client.ClientId,
                client.Description,
                client.IsEnabled,
                client.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("clients/{id}/enable")]
    public async Task<IActionResult> EnableClient(int id)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        await _clientCredentialService.EnableClientAsync(id);
        _logger.LogInformation("Enabled client {ClientId}", id);
        return Ok(new { message = "Client enabled" });
    }

    [HttpPost("clients/{id}/disable")]
    public async Task<IActionResult> DisableClient(int id)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        await _clientCredentialService.DisableClientAsync(id);
        _logger.LogInformation("Disabled client {ClientId}", id);
        return Ok(new { message = "Client disabled" });
    }

    [HttpDelete("clients/{id}")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        if (!await ValidateClientCredentialsAsync())
            return Unauthorized(new { error = "invalid_client" });

        await _clientCredentialService.DeleteClientAsync(id);
        _logger.LogInformation("Deleted client {ClientId}", id);
        return Ok(new { message = "Client deleted" });
    }
}

// DTOs
public record RouteConfigDto(string RouteId, string ClusterId, string Match, int Order, bool IsActive);
public record ClusterConfigDto(string ClusterId, string DestinationAddress, bool IsActive);
public record CreateClientRequest(string ClientId, string ClientSecret, string Description);
