using Microsoft.EntityFrameworkCore;
using VehicleService.Api.Contracts.ServiceRequests;
using VehicleService.Api.Data;
using VehicleService.Api.Identity;
using VehicleService.Api.Services.Interfaces;
using VehicleService.Core.Enums;

namespace VehicleService.Api.Services
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSummaryResponse> GetSummary(
            string userId,
            string role,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("A valid user ID is required", nameof(userId));
            }

            var query = _context.ServiceRequests
                .AsNoTracking()
                .AsQueryable();

            query = role switch
            {
                AppRoles.Admin => query,

                AppRoles.VehicleOwner => query.Where(x => x.OwnerId == userId),

                AppRoles.ServiceProvider => query.Where(x => x.AssignedProviderId == userId),

                _ => throw new UnauthorizedAccessException("Unsupported dashboard role")
            };

            var statusCounts = await query
                .GroupBy(x => x.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.Status,
                    x => x.Count,
                    cancellationToken);

            int Count(ServiceRequestStatus status) => statusCounts.GetValueOrDefault(status);

            int? totalVehicles = null;

            if (role == AppRoles.VehicleOwner)
            {
                totalVehicles = await _context.Vehicles
                    .AsNoTracking()
                    .CountAsync(
                        x => x.OwnerId == userId,
                        cancellationToken);
            }

            var assigned = Count(ServiceRequestStatus.Assigned);
            var accepted = Count(ServiceRequestStatus.Accepted);
            var inProgress = Count(ServiceRequestStatus.InProgress);

            return new DashboardSummaryResponse
            {
                TotalRequests = statusCounts.Values.Sum(),

                PendingRequests = Count(ServiceRequestStatus.Pending),
                AssignedRequests = assigned,
                AcceptedRequests = accepted,
                InProgressRequests = inProgress,
                CompletedRequests = Count(ServiceRequestStatus.Completed),
                ClosedRequests = Count(ServiceRequestStatus.Closed),
                CancelledRequests = Count(ServiceRequestStatus.Cancelled),

                ActiveRequests = assigned + accepted + inProgress,

                TotalVehicles = totalVehicles
            };
        }
    }
}
