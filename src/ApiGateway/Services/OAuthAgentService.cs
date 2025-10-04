using System.Security.Cryptography;
using System.Text;

namespace ApiGateway.Services;

public interface IOAuthAgentService
{
    AuthorizationRequest GenerateAuthorizationRequest(string redirectUri);
    Task<TokenExchangeResult> ExchangeCodeForTokensAsync(string code, string codeVerifier);
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

    public OAuthAgentService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<OAuthAgentService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public AuthorizationRequest GenerateAuthorizationRequest(string redirectUri)
    {
        // Generate PKCE parameters
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        var state = GenerateState();
        var nonce = GenerateNonce(); // Add nonce for OpenID Connect

        var authEndpoint = _configuration["OAuth:AuthorizationEndpoint"] 
            ?? "https://auth.example.com/authorize";
        var clientId = _configuration["OAuth:ClientId"] ?? "your-client-id";
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

    public async Task<TokenExchangeResult> ExchangeCodeForTokensAsync(string code, string codeVerifier)
    {
        try
        {
            var tokenEndpoint = _configuration["OAuth:TokenEndpoint"] 
                ?? "https://auth.example.com/token";
            var clientId = _configuration["OAuth:ClientId"] ?? "your-client-id";
            var clientSecret = _configuration["OAuth:ClientSecret"] ?? "";
            var redirectUri = _configuration["OAuth:RedirectUri"] 
                ?? "https://localhost:5000/oauth/callback";

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
}
