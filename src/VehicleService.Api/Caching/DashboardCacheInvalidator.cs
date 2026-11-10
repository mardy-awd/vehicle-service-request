using Microsoft.Extensions.Caching.Distributed;
using VehicleService.Api.Identity;

namespace VehicleService.Api.Caching;

public sealed class DashboardCacheInvalidator : IDashboardCacheInvalidator
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DashboardCacheInvalidator> _logger;

    public DashboardCacheInvalidator(
        IDistributedCache cache,
        ILogger<DashboardCacheInvalidator> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task InvalidateAsync(string ownerId, string? providerId, CancellationToken cancellationToken = default)
    {
        var keys = new HashSet<string>
        {
            DashboardCacheKeys.For(string.Empty, AppRoles.Admin),
            DashboardCacheKeys.For(ownerId, AppRoles.VehicleOwner)
        };

        if (!string.IsNullOrWhiteSpace(providerId))
        {
            keys.Add(DashboardCacheKeys.For(providerId, AppRoles.ServiceProvider));
        }

        foreach (var key in keys)
        {
            try
            {
                await _cache.RemoveAsync(key, cancellationToken);

                _logger.LogInformation("Dashboard cache invalidated: {CacheKey}", key);
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Unable to invalidate dashboard cache: {CacheKey}", key);
            }
        }
    }
}