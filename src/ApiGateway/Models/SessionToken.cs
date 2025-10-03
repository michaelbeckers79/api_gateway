namespace ApiGateway.Models;

public class SessionToken
{
    public int Id { get; set; }
    public string TokenId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    
    // Navigation property
    public User User { get; set; } = null!;
}
