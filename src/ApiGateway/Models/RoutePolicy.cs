namespace ApiGateway.Models;

public class RoutePolicy
{
    public int Id { get; set; }
    public string RouteId { get; set; } = string.Empty;
    public string SecurityType { get; set; } = string.Empty; // "none", "session", "client_credentials", "token_exchange", "self_signed"
    public string? TokenEndpoint { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Scope { get; set; }
    public int TokenExpirationSeconds { get; set; } = 3600; // Default 1 hour
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
