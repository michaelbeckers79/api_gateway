namespace ApiGateway.Models;

public class ClusterConfig
{
    public int Id { get; set; }
    public string ClusterId { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
