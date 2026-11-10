using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using VehicleService.Api.Caching;
using VehicleService.Api.Contracts.ServiceRequests;
using VehicleService.Api.Services.Interfaces;

namespace VehicleService.Api.Services
{
    public class CachedDashboardService : IDashboardService
    {
        private readonly IDashboardService _inner;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CachedDashboardService> _logger;

        public CachedDashboardService(IDashboardService inner,
            IDistributedCache cache,
            ILogger<CachedDashboardService> logger)
        {
            _inner = inner;
            _cache = cache;
            _logger = logger;
        }

        public async Task<DashboardSummaryResponse> GetSummary(string userId, string role, CancellationToken cancellationToken = default)
        {
            var cacheKey = DashboardCacheKeys.For(userId, role);
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

                if (cached is not null)
                {
                    var summary =
                         JsonSerializer.Deserialize<DashboardSummaryResponse>(cached);

                    if (summary is not null)
                    {
                        _logger.LogInformation("Dashboard cache HIT for role {Role}", role);

                        return summary;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Redis unavailable. Falling back to database");
            }

            _logger.LogInformation("Dashboard cache MISS for role {Role}", role);

            var result = await _inner.GetSummary(userId, role, cancellationToken);
            try
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(60)
                },
                cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Unable to cache dashboard summary");
            }
            return result;
        }
    }
}