using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using ApiGateway.Data;
using ApiGateway.Models;

namespace ApiGateway.Services;

public interface IClientCredentialService
{
    Task<bool> ValidateClientCredentialsAsync(string clientId, string clientSecret);
    Task<ClientCredential?> GetClientByIdAsync(string clientId);
    Task<ClientCredential> CreateClientAsync(string clientId, string clientSecret, string description);
    Task<List<ClientCredential>> GetAllClientsAsync();
    Task EnableClientAsync(int id);
    Task DisableClientAsync(int id);
    Task DeleteClientAsync(int id);
}

public class ClientCredentialService : IClientCredentialService
{
    private readonly ApiGatewayDbContext _dbContext;
    private readonly ILogger<ClientCredentialService> _logger;

    public ClientCredentialService(ApiGatewayDbContext dbContext, ILogger<ClientCredentialService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> ValidateClientCredentialsAsync(string clientId, string clientSecret)
    {
        var client = await _dbContext.ClientCredentials
            .FirstOrDefaultAsync(c => c.ClientId == clientId && c.IsEnabled);

        if (client == null)
        {
            _logger.LogWarning("Client not found or disabled: {ClientId}", clientId);
            return false;
        }

        var secretHash = HashSecret(clientSecret);
        var isValid = client.ClientSecretHash == secretHash;

        if (isValid)
        {
            client.LastUsedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Client credentials validated for: {ClientId}", clientId);
        }
        else
        {
            _logger.LogWarning("Invalid client secret for: {ClientId}", clientId);
        }

        return isValid;
    }

    public async Task<ClientCredential?> GetClientByIdAsync(string clientId)
    {
        return await _dbContext.ClientCredentials
            .FirstOrDefaultAsync(c => c.ClientId == clientId);
    }

    public async Task<ClientCredential> CreateClientAsync(string clientId, string clientSecret, string description)
    {
        var existingClient = await GetClientByIdAsync(clientId);
        if (existingClient != null)
        {
            throw new InvalidOperationException($"Client with ID {clientId} already exists");
        }

        var client = new ClientCredential
        {
            ClientId = clientId,
            ClientSecretHash = HashSecret(clientSecret),
            Description = description,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ClientCredentials.Add(client);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created new client: {ClientId}", clientId);
        
        return client;
    }

    public async Task<List<ClientCredential>> GetAllClientsAsync()
    {
        return await _dbContext.ClientCredentials.ToListAsync();
    }

    public async Task EnableClientAsync(int id)
    {
        var client = await _dbContext.ClientCredentials.FindAsync(id);
        if (client != null)
        {
            client.IsEnabled = true;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Enabled client {ClientId}", client.ClientId);
        }
    }

    public async Task DisableClientAsync(int id)
    {
        var client = await _dbContext.ClientCredentials.FindAsync(id);
        if (client != null)
        {
            client.IsEnabled = false;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Disabled client {ClientId}", client.ClientId);
        }
    }

    public async Task DeleteClientAsync(int id)
    {
        var client = await _dbContext.ClientCredentials.FindAsync(id);
        if (client != null)
        {
            _dbContext.ClientCredentials.Remove(client);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Deleted client {ClientId}", client.ClientId);
        }
    }

    private static string HashSecret(string secret)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(hash);
    }
}
