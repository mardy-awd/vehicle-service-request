using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VehicleService.Api.Contracts.ServiceRequests;
using VehicleService.Api.Identity;
using VehicleService.Api.Services.Interfaces;

namespace VehicleService.Api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize(Roles =
    AppRoles.Admin + "," +
    AppRoles.VehicleOwner + "," +
    AppRoles.ServiceProvider)]
    public sealed class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                return Forbid();
            }

            var summary = await _dashboardService.GetSummary(
                userId,
                role,
                cancellationToken);

            return Ok(summary);
        }
    }
}
