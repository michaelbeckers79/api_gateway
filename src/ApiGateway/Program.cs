using Microsoft.EntityFrameworkCore;
using ApiGateway.Data;
using ApiGateway.Services;
using ApiGateway.Middleware;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure DbContext with SQLite
builder.Services.AddDbContext<ApiGatewayDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=apigateway.db"));

// Add Data Protection for encrypted cookies (OWASP best practice)
builder.Services.AddDataProtection();

// Add distributed cache (default to memory cache)
builder.Services.AddDistributedMemoryCache();

// Add HTTP client factory
builder.Services.AddHttpClient();

// Register custom services
builder.Services.AddScoped<ISessionTokenService, SessionTokenService>();
builder.Services.AddScoped<IOAuthAgentService, OAuthAgentService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IClientCredentialService, ClientCredentialService>();
builder.Services.AddScoped<IUpstreamTokenService, UpstreamTokenService>();
builder.Services.AddSingleton<DatabaseProxyConfigProvider>();
builder.Services.AddSingleton<IProxyConfigProvider>(sp => 
    sp.GetRequiredService<DatabaseProxyConfigProvider>());

// Add background services
builder.Services.AddHostedService<SessionCleanupBackgroundService>();

// Add YARP reverse proxy with database configuration
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Configure CORS if needed
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                ?? new[] { "http://localhost:3000" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApiGatewayDbContext>();

var app = builder.Build();

// Apply database migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApiGatewayDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Apply security headers (OWASP best practices)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy", 
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;");
    await next();
});

app.UseHttpsRedirection();
app.UseCors();

// Use session validation middleware before routing
app.UseSessionValidation();

app.MapControllers();
app.MapHealthChecks("/health");

// Map YARP reverse proxy
app.MapReverseProxy();

app.Run();
