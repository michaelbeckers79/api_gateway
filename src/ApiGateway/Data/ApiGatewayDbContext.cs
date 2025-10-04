using Microsoft.EntityFrameworkCore;
using ApiGateway.Models;
using System.Text;

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
    public DbSet<RoutePolicy> RoutePolicies { get; set; }
    public DbSet<UpstreamToken> UpstreamTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure snake_case naming convention
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Convert table names to snake_case
            entity.SetTableName(ToSnakeCase(entity.GetTableName() ?? entity.DisplayName()));

            // Convert column names to snake_case
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            // Convert foreign key constraint names to snake_case
            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName() ?? $"PK_{entity.GetTableName()}"));
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName() ?? 
                    $"FK_{entity.GetTableName()}_{foreignKey.PrincipalEntityType.GetTableName()}"));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName() ?? 
                    $"IX_{entity.GetTableName()}_{string.Join("_", index.Properties.Select(p => p.Name))}"));
            }
        }

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

        modelBuilder.Entity<RoutePolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RouteId).IsUnique();
            entity.Property(e => e.RouteId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SecurityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TokenEndpoint).HasMaxLength(500);
            entity.Property(e => e.ClientId).HasMaxLength(200);
            entity.Property(e => e.ClientSecret).HasMaxLength(500);
            entity.Property(e => e.Scope).HasMaxLength(500);
        });

        modelBuilder.Entity<UpstreamToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RouteId, e.SessionId });
            entity.Property(e => e.RouteId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AccessToken).IsRequired();
            
            // Configure relationship
            entity.HasOne(e => e.Session)
                .WithMany()
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
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

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
