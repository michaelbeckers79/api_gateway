using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;
using ApiGateway.Data;

namespace ApiGateway.Services;

public class DatabaseProxyConfigProvider : IProxyConfigProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseProxyConfigProvider> _logger;
    private volatile DatabaseProxyConfig? _config;

    public DatabaseProxyConfigProvider(
        IServiceProvider serviceProvider,
        ILogger<DatabaseProxyConfigProvider> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IProxyConfig GetConfig()
    {
        if (_config == null)
        {
            _config = LoadConfig();
        }
        return _config;
    }

    private DatabaseProxyConfig LoadConfig()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiGatewayDbContext>();

        var routes = dbContext.RouteConfigs
            .Where(r => r.IsActive)
            .ToList();

        var clusters = dbContext.ClusterConfigs
            .Where(c => c.IsActive)
            .ToList();

        var routeConfigs = routes.Select(r => new RouteConfig
        {
            RouteId = r.RouteId,
            ClusterId = r.ClusterId,
            Match = new RouteMatch
            {
                Path = r.Match
            },
            Order = r.Order
        }).ToList();

        var clusterConfigs = clusters.ToDictionary(
            c => c.ClusterId,
            c => new ClusterConfig
            {
                ClusterId = c.ClusterId,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    [c.ClusterId + "-destination"] = new DestinationConfig
                    {
                        Address = c.DestinationAddress
                    }
                }
            });

        _logger.LogInformation("Loaded {RouteCount} routes and {ClusterCount} clusters from database",
            routeConfigs.Count, clusterConfigs.Count);

        return new DatabaseProxyConfig(routeConfigs, clusterConfigs);
    }

    public void Reload()
    {
        var oldConfig = _config;
        _config = LoadConfig();
        oldConfig?.SignalChange();
    }
}

internal class DatabaseProxyConfig : IProxyConfig
{
    private readonly CancellationTokenSource _cts = new();
    private readonly IReadOnlyDictionary<string, ClusterConfig> _clusters;

    public DatabaseProxyConfig(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyDictionary<string, ClusterConfig> clusters)
    {
        Routes = routes;
        _clusters = clusters;
        ChangeToken = new CancellationChangeToken(_cts.Token);
    }

    public IReadOnlyList<RouteConfig> Routes { get; }

    public IReadOnlyList<ClusterConfig> Clusters => _clusters.Values.ToList();

    public IChangeToken ChangeToken { get; }

    internal void SignalChange()
    {
        _cts.Cancel();
    }
}
