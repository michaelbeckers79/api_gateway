using Microsoft.AspNetCore.DataProtection;
using ApiGateway.Services;

namespace ApiGateway.Middleware;

public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionValidationMiddleware> _logger;
    private const string SessionCookieName = "__Host-Session";

    // Paths that don't require authentication
    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/oauth/login/start",
        "/oauth/callback",
        "/oauth/login/end",
        "/oauth/isloggedin",
        "/health",
        "/admin"
    };

    public SessionValidationMiddleware(
        RequestDelegate next,
        ILogger<SessionValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISessionTokenService sessionTokenService,
        IDataProtectionProvider dataProtectionProvider)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip validation for public paths
        if (PublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Check for session cookie
        if (!context.Request.Cookies.TryGetValue(SessionCookieName, out var encryptedSessionToken))
        {
            _logger.LogWarning("No session cookie found for path {Path}", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "unauthorized", message = "Authentication required" });
            return;
        }

        try
        {
            var protector = dataProtectionProvider.CreateProtector("AuthCookies");
            var sessionTokenId = protector.Unprotect(encryptedSessionToken);

            var session = await sessionTokenService.GetSessionAsync(sessionTokenId);

            if (session == null)
            {
                _logger.LogWarning("Invalid or expired session token");
                context.Response.Cookies.Delete(SessionCookieName);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "unauthorized", message = "Session expired" });
                return;
            }

            // Add session info to context for downstream use
            context.Items["SessionTokenId"] = sessionTokenId;
            context.Items["UserId"] = session.UserId;
            context.Items["AccessToken"] = session.AccessToken;

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "unauthorized", message = "Invalid session" });
        }
    }
}

public static class SessionValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionValidationMiddleware>();
    }
}
