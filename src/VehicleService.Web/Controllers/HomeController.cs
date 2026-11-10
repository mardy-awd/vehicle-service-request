using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleService.Web.Models.Dashboard;
using VehicleService.Web.Services;

namespace VehicleService.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly VehicleServiceApiClient _apiClient;

    public HomeController(VehicleServiceApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _apiClient.GetAsync<DashboardSummaryViewModel>("api/dashboard/summary", cancellationToken);

            if (summary is null)
            {
                return View("Error");
            }

            return View(summary);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Account");
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Unable to load dashboard statistics.";

            return View(new DashboardSummaryViewModel());
        }
    }
}