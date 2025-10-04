namespace ApiGateway.Models;

public class UpstreamToken
{
    public int Id { get; set; }
    public string RouteId { get; set; } = string.Empty;
    public int? SessionId { get; set; } // Nullable for client_credentials flow
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRefreshedAt { get; set; }
    
    // Navigation properties
    public SessionToken? Session { get; set; }
}
