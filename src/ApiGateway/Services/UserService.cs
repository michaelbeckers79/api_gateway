using Microsoft.EntityFrameworkCore;
using ApiGateway.Data;
using ApiGateway.Models;

namespace ApiGateway.Services;

public interface IUserService
{
    Task<User?> GetOrCreateUserAsync(string username, string email);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByIdAsync(int userId);
    Task<bool> IsUserEnabledAsync(int userId);
    Task<bool> IsUserEnabledAsync(string username);
    Task EnableUserAsync(int userId);
    Task DisableUserAsync(int userId);
    Task<List<User>> GetAllUsersAsync();
    Task UpdateLastLoginAsync(int userId);
    Task RegisterPasskeyAsync(int userId, string passkey);
    Task<bool> ValidatePasskeyAsync(int userId, string passkey);
}

public class UserService : IUserService
{
    private readonly ApiGatewayDbContext _dbContext;
    private readonly ILogger<UserService> _logger;

    public UserService(ApiGatewayDbContext dbContext, ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<User?> GetOrCreateUserAsync(string username, string email)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        
        if (user == null)
        {
            user = new User
            {
                Username = username,
                Email = email,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Created new user: {Username}", username);
        }
        
        return user;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _dbContext.Users.FindAsync(userId);
    }

    public async Task<bool> IsUserEnabledAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        return user?.IsEnabled ?? false;
    }

    public async Task<bool> IsUserEnabledAsync(string username)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user?.IsEnabled ?? false;
    }

    public async Task EnableUserAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsEnabled = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Enabled user {UserId}", userId);
        }
    }

    public async Task DisableUserAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsEnabled = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Disabled user {UserId}", userId);
        }
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _dbContext.Users
            .Include(u => u.Sessions.Where(s => !s.IsRevoked))
            .ToListAsync();
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task RegisterPasskeyAsync(int userId, string passkey)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User {userId} not found");
        }

        user.Passkey = passkey;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Registered passkey for user {UserId}", userId);
    }

    public async Task<bool> ValidatePasskeyAsync(int userId, string passkey)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        return user.Passkey == passkey;
    }
}
