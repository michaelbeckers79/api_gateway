using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using ApiGateway.Services;
using System.Text.Json;

namespace ApiGateway.Controllers;

[ApiController]
[Route("oauth")]
public class AuthController : ControllerBase
{
    private readonly IOAuthAgentService _oauthAgent;
    private readonly ISessionTokenService _sessionTokenService;
    private readonly IJwtService _jwtService;
    private readonly IUserService _userService;
    private readonly IDataProtector _protector;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    // OWASP best practices for cookie configuration
    private const string SessionCookieName = "__Host-Session";
    private const string StateCookieName = "__Host-State";
    private const string CodeVerifierCookieName = "__Host-CodeVerifier";
    private const string NonceCookieName = "__Host-Nonce";

    public AuthController(
        IOAuthAgentService oauthAgent,
        ISessionTokenService sessionTokenService,
        IJwtService jwtService,
        IUserService userService,
        IDataProtectionProvider dataProtectionProvider,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _oauthAgent = oauthAgent;
        _sessionTokenService = sessionTokenService;
        _jwtService = jwtService;
        _userService = userService;
        _protector = dataProtectionProvider.CreateProtector("AuthCookies");
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login/start")]
    public IActionResult LoginStart([FromBody] LoginStartRequest request)
    {
        try
        {
            var redirectUri = request.RedirectUri ?? $"{Request.Scheme}://{Request.Host}/oauth/callback";
            
            var authRequest = _oauthAgent.GenerateAuthorizationRequest(redirectUri);

            // Store state and code verifier in secure, encrypted cookies
            // Following OWASP best practices:
            // - HttpOnly: prevents XSS attacks
            // - Secure: only sent over HTTPS
            // - SameSite=Strict: prevents CSRF attacks
            // - __Host- prefix: ensures Secure flag and no Domain attribute
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromMinutes(10),
                Path = "/",
                IsEssential = true
            };

            // Encrypt sensitive data before storing in cookies
            var encryptedState = _protector.Protect(authRequest.State);
            var encryptedCodeVerifier = _protector.Protect(authRequest.CodeVerifier);
            var encryptedNonce = _protector.Protect(authRequest.Nonce);

            Response.Cookies.Append(StateCookieName, encryptedState, cookieOptions);
            Response.Cookies.Append(CodeVerifierCookieName, encryptedCodeVerifier, cookieOptions);
            Response.Cookies.Append(NonceCookieName, encryptedNonce, cookieOptions);

            _logger.LogInformation("Login started for redirect URI: {RedirectUri}", redirectUri);

            return Ok(new LoginStartResponse
            {
                AuthorizationUrl = authRequest.AuthorizationUrl,
                Instructions = new AuthInstructions
                {
                    Action = "redirect",
                    Url = authRequest.AuthorizationUrl,
                    Method = "GET"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting login flow");
            return StatusCode(500, new { error = "internal_error", message = "Failed to start login flow" });
        }
    }

    [HttpGet("callback")]
    public async Task<IActionResult> LoginCallback([FromQuery] string? code, [FromQuery] string? state, 
        [FromQuery] string? error)
    {
        try
        {
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("OAuth error: {Error}", error);
                return BadRequest(new { error, message = "OAuth authorization failed" });
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return BadRequest(new { error = "invalid_request", message = "Missing code or state" });
            }

            // Retrieve and validate state from cookie
            if (!Request.Cookies.TryGetValue(StateCookieName, out var encryptedState))
            {
                _logger.LogWarning("State cookie not found");
                return BadRequest(new { error = "invalid_state", message = "State validation failed" });
            }

            var storedState = _protector.Unprotect(encryptedState);
            if (storedState != state)
            {
                _logger.LogWarning("State mismatch: expected {Expected}, got {Actual}", storedState, state);
                return BadRequest(new { error = "invalid_state", message = "State validation failed" });
            }

            // Retrieve code verifier from cookie
            if (!Request.Cookies.TryGetValue(CodeVerifierCookieName, out var encryptedCodeVerifier))
            {
                _logger.LogWarning("Code verifier cookie not found");
                return BadRequest(new { error = "invalid_request", message = "Missing code verifier" });
            }

            var codeVerifier = _protector.Unprotect(encryptedCodeVerifier);

            // Exchange code for tokens
            var tokenResult = await _oauthAgent.ExchangeCodeForTokensAsync(code, codeVerifier);

            if (!tokenResult.Success)
            {
                _logger.LogError("Token exchange failed: {Error}", tokenResult.Error);
                return BadRequest(new { error = "token_exchange_failed", message = tokenResult.Error });
            }

            // Create session with opaque token
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers.UserAgent.ToString();
            
            // Extract user ID from ID token or access token (simplified - in production, decode JWT)
            var userId = await ExtractUserIdFromTokenAsync(tokenResult.IdToken ?? tokenResult.AccessToken);

            var sessionTokenId = await _sessionTokenService.CreateSessionAsync(
                userId, 
                tokenResult.AccessToken, 
                tokenResult.RefreshToken,
                ipAddress,
                userAgent);

            // Set session cookie with opaque token
            var sessionCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromHours(8),
                Path = "/",
                IsEssential = true
            };

            var encryptedSessionToken = _protector.Protect(sessionTokenId);
            Response.Cookies.Append(SessionCookieName, encryptedSessionToken, sessionCookieOptions);

            // Clear temporary cookies
            Response.Cookies.Delete(StateCookieName);
            Response.Cookies.Delete(CodeVerifierCookieName);
            Response.Cookies.Delete(NonceCookieName);

            _logger.LogInformation("Login completed successfully for user {UserId}", userId);

            return Ok(new LoginEndResponse
            {
                Success = true,
                Message = "Login successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing login flow");
            return StatusCode(500, new { error = "internal_error", message = "Failed to complete login" });
        }
    }

    [HttpPost("login/end")]
    public async Task<IActionResult> LoginEnd([FromBody] LoginEndRequest request)
    {
        // Alternative endpoint for SPA to call after OAuth redirect
        return await LoginCallback(request.Code, request.State, request.Error);
    }

    [HttpGet("isloggedin")]
    public async Task<IActionResult> IsLoggedIn()
    {
        try
        {
            if (!Request.Cookies.TryGetValue(SessionCookieName, out var encryptedSessionToken))
            {
                return Ok(new IsLoggedInResponse { IsLoggedIn = false });
            }

            var sessionTokenId = _protector.Unprotect(encryptedSessionToken);
            var isValid = await _sessionTokenService.ValidateSessionAsync(sessionTokenId);

            if (!isValid)
            {
                // Clear invalid session cookie
                Response.Cookies.Delete(SessionCookieName);
                return Ok(new IsLoggedInResponse { IsLoggedIn = false });
            }

            var session = await _sessionTokenService.GetSessionAsync(sessionTokenId);

            return Ok(new IsLoggedInResponse
            {
                IsLoggedIn = true,
                UserId = session?.UserId.ToString(),
                Username = session?.User?.Username
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking login status");
            return Ok(new IsLoggedInResponse { IsLoggedIn = false });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            if (Request.Cookies.TryGetValue(SessionCookieName, out var encryptedSessionToken))
            {
                var sessionTokenId = _protector.Unprotect(encryptedSessionToken);
                await _sessionTokenService.RevokeSessionAsync(sessionTokenId);
                
                _logger.LogInformation("Session {SessionTokenId} logged out", sessionTokenId);
            }

            // Clear all auth cookies
            Response.Cookies.Delete(SessionCookieName);
            Response.Cookies.Delete(StateCookieName);
            Response.Cookies.Delete(CodeVerifierCookieName);
            Response.Cookies.Delete(NonceCookieName);

            return Ok(new LogoutResponse
            {
                Success = true,
                Message = "Logout successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { error = "internal_error", message = "Failed to logout" });
        }
    }

    private async Task<string> ExtractUserIdFromTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                return Guid.NewGuid().ToString();

            // Get the configured claim name for username
            var usernameClaim = _configuration["Jwt:UsernameClaim"] ?? "preferred_username";
            
            // Extract username from JWT
            var username = _jwtService.ExtractUsername(token, usernameClaim);
            
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("Could not extract username from token, using fallback");
                return Guid.NewGuid().ToString();
            }

            // Extract email from JWT (fallback to username if not found)
            var principal = _jwtService.ValidateToken(token);
            var email = principal?.FindFirst("email")?.Value ?? username;

            // Get or create user
            var user = await _userService.GetOrCreateUserAsync(username, email);
            
            if (user == null)
            {
                _logger.LogError("Failed to get or create user for username: {Username}", username);
                return Guid.NewGuid().ToString();
            }

            // Check if user is enabled
            if (!user.IsEnabled)
            {
                _logger.LogWarning("User is disabled: {Username}", username);
                throw new UnauthorizedAccessException($"User is disabled: {username}");
            }

            // Update last login
            await _userService.UpdateLastLoginAsync(user.Id);

            return username;
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user from token");
            return Guid.NewGuid().ToString();
        }
    }
}

// Request/Response DTOs
public record LoginStartRequest(string? RedirectUri = null);

public record LoginStartResponse
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public AuthInstructions Instructions { get; set; } = new();
}

public record AuthInstructions
{
    public string Action { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
}

public record LoginEndRequest(string? Code, string? State, string? Error);

public record LoginEndResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public record IsLoggedInResponse
{
    public bool IsLoggedIn { get; set; }
    public string? UserId { get; set; }
    public string? Username { get; set; }
}

public record LogoutResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
