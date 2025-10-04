using System.Text.Json;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Services;

public interface IOidcDiscoveryService
{
    Task<OpenIdConnectConfiguration> GetConfigurationAsync();
    Task<TokenValidationParameters> GetTokenValidationParametersAsync();
}

public class OidcDiscoveryService : IOidcDiscoveryService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OidcDiscoveryService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private OpenIdConnectConfiguration? _cachedConfiguration;
    private DateTime _cacheExpiration = DateTime.MinValue;
    private readonly TimeSpan _cacheLifetime = TimeSpan.FromHours(24);
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public OidcDiscoveryService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<OidcDiscoveryService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync()
    {
        // Check if we have a valid cached configuration
        if (_cachedConfiguration != null && DateTime.UtcNow < _cacheExpiration)
        {
            return _cachedConfiguration;
        }

        await _cacheLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cachedConfiguration != null && DateTime.UtcNow < _cacheExpiration)
            {
                return _cachedConfiguration;
            }

            var issuer = GetIssuerFromConfiguration();
            var wellKnownEndpoint = $"{issuer.TrimEnd('/')}/.well-known/openid-configuration";

            _logger.LogInformation("Fetching OIDC configuration from {Endpoint}", wellKnownEndpoint);

            var httpClient = _httpClientFactory.CreateClient();
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                wellKnownEndpoint,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(httpClient) { RequireHttps = true });

            _cachedConfiguration = await configManager.GetConfigurationAsync(CancellationToken.None);
            _cacheExpiration = DateTime.UtcNow.Add(_cacheLifetime);

            _logger.LogInformation("Successfully retrieved OIDC configuration. Issuer: {Issuer}, JWKS URI: {JwksUri}",
                _cachedConfiguration.Issuer, _cachedConfiguration.JwksUri);

            return _cachedConfiguration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve OIDC configuration from well-known endpoint");
            throw new InvalidOperationException("Failed to retrieve OIDC configuration", ex);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<TokenValidationParameters> GetTokenValidationParametersAsync()
    {
        var config = await GetConfigurationAsync();
        var clientId = _configuration["OAuth:ClientId"] ?? throw new InvalidOperationException("OAuth:ClientId not configured");

        return new TokenValidationParameters
        {
            // OWASP Guidelines: Validate issuer
            ValidateIssuer = true,
            ValidIssuer = config.Issuer,

            // OWASP Guidelines: Validate audience
            ValidateAudience = true,
            ValidAudience = clientId,
            ValidAudiences = new[] { clientId },

            // OWASP Guidelines: Validate signature using JWKS
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = config.SigningKeys,

            // OWASP Guidelines: Validate token lifetime
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew

            // Additional security settings
            RequireExpirationTime = true,
            RequireSignedTokens = true,

            // For ID tokens, these should be validated
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };
    }

    private string GetIssuerFromConfiguration()
    {
        // Try to get issuer from configuration
        var issuer = _configuration["OAuth:Issuer"];
        if (!string.IsNullOrEmpty(issuer))
        {
            return issuer;
        }

        // Try to derive from authorization endpoint
        var authEndpoint = _configuration["OAuth:AuthorizationEndpoint"];
        if (!string.IsNullOrEmpty(authEndpoint))
        {
            var uri = new Uri(authEndpoint);
            // Remove the authorization path (e.g., /authorize, /protocol/openid-connect/auth)
            var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            // For Keycloak: https://keycloak.example.com/realms/myrealm/protocol/openid-connect/auth
            // Issuer should be: https://keycloak.example.com/realms/myrealm
            if (pathSegments.Length >= 2 && pathSegments[0] == "realms")
            {
                return $"{uri.Scheme}://{uri.Authority}/realms/{pathSegments[1]}";
            }
            
            // For other OIDC providers, try to use the base URL
            return $"{uri.Scheme}://{uri.Authority}";
        }

        throw new InvalidOperationException("Cannot determine OIDC issuer. Please configure OAuth:Issuer or OAuth:AuthorizationEndpoint");
    }
}
