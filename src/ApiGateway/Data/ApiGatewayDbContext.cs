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
    public DbSet<User> Users { get; set; }
    public DbSet<ClientCredential> ClientCredentials { get; set; }

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

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<SessionToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TokenId).IsUnique();
            entity.Property(e => e.TokenId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AccessToken).IsRequired();
            entity.Property(e => e.RefreshToken).IsRequired();
            
            // Configure relationship
            entity.HasOne(e => e.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClientCredential>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClientId).IsUnique();
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClientSecretHash).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
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
