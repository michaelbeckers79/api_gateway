using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Services;

public interface IOAuthAgentService
{
    Task<AuthorizationRequest> GenerateAuthorizationRequestAsync(string redirectUri);
    Task<TokenExchangeResult> ExchangeCodeForTokensAsync(string code, string codeVerifier, string? expectedNonce = null);
}

public class AuthorizationRequest
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
    public string CodeChallenge { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty; // OpenID Connect nonce
}

public class TokenExchangeResult
{
    public bool Success { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string? Error { get; set; }
}

public class OAuthAgentService : IOAuthAgentService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OAuthAgentService> _logger;
    private readonly IOidcDiscoveryService _oidcDiscovery;
    private readonly JwtSecurityTokenHandler _jwtHandler;

    public OAuthAgentService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IOidcDiscoveryService oidcDiscovery,
        ILogger<OAuthAgentService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _oidcDiscovery = oidcDiscovery;
        _logger = logger;
        _jwtHandler = new JwtSecurityTokenHandler();
    }

    public async Task<AuthorizationRequest> GenerateAuthorizationRequestAsync(string redirectUri)
    {
        // Generate PKCE parameters
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        var state = GenerateState();
        var nonce = GenerateNonce(); // Add nonce for OpenID Connect

        // Get OIDC configuration from discovery endpoint
        var oidcConfig = await _oidcDiscovery.GetConfigurationAsync();
        var authEndpoint = oidcConfig.AuthorizationEndpoint;
        
        var clientId = _configuration["OAuth:ClientId"] ?? throw new InvalidOperationException("OAuth:ClientId not configured");
        var scope = _configuration["OAuth:Scope"] ?? "openid profile email";

        var queryParams = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["scope"] = scope,
            ["state"] = state,
            ["nonce"] = nonce, // OpenID Connect nonce for replay protection
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        var queryString = string.Join("&", queryParams.Select(kvp => 
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        var authorizationUrl = $"{authEndpoint}?{queryString}";

        _logger.LogInformation("Generated OpenID Connect authorization request with state {State} and nonce {Nonce}", state, nonce);

        return new AuthorizationRequest
        {
            AuthorizationUrl = authorizationUrl,
            State = state,
            CodeVerifier = codeVerifier,
            CodeChallenge = codeChallenge,
            Nonce = nonce
        };
    }

    public async Task<TokenExchangeResult> ExchangeCodeForTokensAsync(string code, string codeVerifier, string? expectedNonce = null)
    {
        try
        {
            // Get OIDC configuration from discovery endpoint
            var oidcConfig = await _oidcDiscovery.GetConfigurationAsync();
            var tokenEndpoint = oidcConfig.TokenEndpoint;
            
            var clientId = _configuration["OAuth:ClientId"] ?? throw new InvalidOperationException("OAuth:ClientId not configured");
            var clientSecret = _configuration["OAuth:ClientSecret"] ?? "";
            var redirectUri = _configuration["OAuth:RedirectUri"] 
                ?? throw new InvalidOperationException("OAuth:RedirectUri not configured");

            var httpClient = _httpClientFactory.CreateClient();

            var requestData = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = clientId,
                ["code_verifier"] = codeVerifier
            };

            if (!string.IsNullOrEmpty(clientSecret))
            {
                requestData["client_secret"] = clientSecret;
            }

            var content = new FormUrlEncodedContent(requestData);
            var response = await httpClient.PostAsync(tokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token exchange failed: {Error}", errorContent);
                return new TokenExchangeResult
                {
                    Success = false,
                    Error = errorContent
                };
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

            if (tokenResponse == null)
            {
                return new TokenExchangeResult
                {
                    Success = false,
                    Error = "Invalid token response"
                };
            }

            _logger.LogInformation("Successfully exchanged authorization code for tokens");

            // OWASP Guidelines: Validate ID token if present
            if (!string.IsNullOrEmpty(tokenResponse.id_token))
            {
                var validationResult = await ValidateIdTokenAsync(tokenResponse.id_token, expectedNonce);
                if (!validationResult.Success)
                {
                    _logger.LogError("ID token validation failed: {Error}", validationResult.Error);
                    return new TokenExchangeResult
                    {
                        Success = false,
                        Error = $"ID token validation failed: {validationResult.Error}"
                    };
                }
                
                _logger.LogInformation("ID token validated successfully. Subject: {Subject}", validationResult.Subject);
            }
            else
            {
                _logger.LogWarning("No ID token returned from token endpoint. OpenID Connect requires ID token.");
            }

            return new TokenExchangeResult
            {
                Success = true,
                AccessToken = tokenResponse.access_token,
                RefreshToken = tokenResponse.refresh_token ?? string.Empty,
                IdToken = tokenResponse.id_token ?? string.Empty,
                ExpiresIn = tokenResponse.expires_in
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging authorization code for tokens");
            return new TokenExchangeResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private async Task<IdTokenValidationResult> ValidateIdTokenAsync(string idToken, string? expectedNonce)
    {
        try
        {
            // Get token validation parameters from OIDC discovery
            var validationParameters = await _oidcDiscovery.GetTokenValidationParametersAsync();

            // OWASP Guidelines: Validate token signature, issuer, audience, and lifetime
            var principal = _jwtHandler.ValidateToken(idToken, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return new IdTokenValidationResult
                {
                    Success = false,
                    Error = "Invalid token format"
                };
            }

            // OWASP Guidelines: Validate algorithm (must be RS256 or other approved algorithm)
            if (jwtToken.Header.Alg != SecurityAlgorithms.RsaSha256 &&
                jwtToken.Header.Alg != SecurityAlgorithms.RsaSha384 &&
                jwtToken.Header.Alg != SecurityAlgorithms.RsaSha512)
            {
                _logger.LogWarning("ID token uses non-RS algorithm: {Algorithm}", jwtToken.Header.Alg);
                return new IdTokenValidationResult
                {
                    Success = false,
                    Error = $"Unsupported signing algorithm: {jwtToken.Header.Alg}"
                };
            }

            // OIDC Requirement: Validate nonce if provided
            if (!string.IsNullOrEmpty(expectedNonce))
            {
                var nonceClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "nonce");
                if (nonceClaim == null)
                {
                    _logger.LogError("ID token missing nonce claim");
                    return new IdTokenValidationResult
                    {
                        Success = false,
                        Error = "ID token missing nonce claim"
                    };
                }

                if (nonceClaim.Value != expectedNonce)
                {
                    _logger.LogError("Nonce mismatch. Expected: {Expected}, Got: {Actual}", expectedNonce, nonceClaim.Value);
                    return new IdTokenValidationResult
                    {
                        Success = false,
                        Error = "Nonce validation failed"
                    };
                }

                _logger.LogDebug("Nonce validated successfully");
            }

            // Extract subject (user ID)
            var subject = principal.FindFirst("sub")?.Value ?? principal.FindFirst("preferred_username")?.Value;

            return new IdTokenValidationResult
            {
                Success = true,
                Subject = subject,
                Principal = principal
            };
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogError(ex, "ID token validation failed");
            return new IdTokenValidationResult
            {
                Success = false,
                Error = $"Token validation error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during ID token validation");
            return new IdTokenValidationResult
            {
                Success = false,
                Error = $"Unexpected validation error: {ex.Message}"
            };
        }
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static string GenerateState()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static string GenerateNonce()
    {
        // Generate a nonce for OpenID Connect replay protection
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private class TokenResponse
    {
        public string access_token { get; set; } = string.Empty;
        public string? refresh_token { get; set; }
        public string? id_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; } = string.Empty;
    }

    private class IdTokenValidationResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Subject { get; set; }
        public System.Security.Claims.ClaimsPrincipal? Principal { get; set; }
    }
}
