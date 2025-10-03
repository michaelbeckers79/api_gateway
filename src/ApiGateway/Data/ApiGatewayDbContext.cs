using Microsoft.EntityFrameworkCore;
using ApiGateway.Models;

namespace ApiGateway.Data;

public class ApiGatewayDbContext : DbContext
{
    public ApiGatewayDbContext(DbContextOptions<ApiGatewayDbContext> options)
        : base(options)
    {
    }

    public DbSet<RouteConfig> RouteConfigs { get; set; }
    public DbSet<ClusterConfig> ClusterConfigs { get; set; }
    public DbSet<SessionToken> SessionTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RouteConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RouteId).IsUnique();
            entity.Property(e => e.RouteId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClusterId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Match).IsRequired();
        });

        modelBuilder.Entity<ClusterConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClusterId).IsUnique();
            entity.Property(e => e.ClusterId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DestinationAddress).IsRequired();
        });

        modelBuilder.Entity<SessionToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TokenId).IsUnique();
            entity.Property(e => e.TokenId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AccessToken).IsRequired();
            entity.Property(e => e.RefreshToken).IsRequired();
        });

        // Seed some default routes
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        modelBuilder.Entity<ClusterConfig>().HasData(
            new ClusterConfig
            {
                Id = 1,
                ClusterId = "backend-api",
                DestinationAddress = "http://localhost:5001",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );

        modelBuilder.Entity<RouteConfig>().HasData(
            new RouteConfig
            {
                Id = 1,
                RouteId = "api-route",
                ClusterId = "backend-api",
                Match = "/api/{**catch-all}",
                Order = 1,
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );
    }
}
