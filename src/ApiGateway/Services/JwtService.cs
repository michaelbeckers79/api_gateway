using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Services;

public interface IJwtService
{
    string? ExtractUsername(string token, string claimName);
    ClaimsPrincipal? ValidateToken(string token);
    string GenerateToken(Dictionary<string, string> claims, int expirationSeconds = 3600);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public string? ExtractUsername(string token, string claimName)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                return null;

            // Handle Bearer prefix
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring(7);
            }

            var jwtToken = _tokenHandler.ReadJwtToken(token);
            
            // Try to get the claim by the configured name
            var claim = jwtToken.Claims.FirstOrDefault(c => c.Type == claimName);
            if (claim != null)
            {
                _logger.LogDebug("Extracted username from claim {ClaimName}: {Username}", claimName, claim.Value);
                return claim.Value;
            }

            // Fallback to common claim names
            var fallbackClaims = new[] { "preferred_username", "username", "name", "email", "sub", "upn" };
            foreach (var fallbackClaim in fallbackClaims)
            {
                claim = jwtToken.Claims.FirstOrDefault(c => c.Type == fallbackClaim);
                if (claim != null)
                {
                    _logger.LogWarning("Username not found in configured claim {ClaimName}, using fallback claim {FallbackClaim}: {Username}", 
                        claimName, fallbackClaim, claim.Value);
                    return claim.Value;
                }
            }

            _logger.LogWarning("Could not extract username from token using claim {ClaimName} or fallback claims", claimName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting username from token");
            return null;
        }
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                return null;

            // Handle Bearer prefix
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring(7);
            }

            // For now, just read the token without validation (signature validation requires issuer keys)
            // In production, you should validate the signature
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            
            var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return null;
        }
    }

    public string GenerateToken(Dictionary<string, string> claims, int expirationSeconds = 3600)
    {
        try
        {
            // Get signing key from configuration or generate a default one
            var jwtSecret = _configuration["Jwt:Secret"] ?? "default-secret-key-change-in-production-minimum-32-characters";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var issuer = _configuration["Jwt:Issuer"] ?? "api-gateway";
            var audience = _configuration["Jwt:Audience"] ?? "api-gateway";

            var claimsList = claims.Select(kvp => new Claim(kvp.Key, kvp.Value)).ToList();
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claimsList),
                Expires = DateTime.UtcNow.AddSeconds(expirationSeconds),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            return _tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token");
            throw;
        }
    }
}
