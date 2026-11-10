using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using VehicleService.Web.Models.Vehicles;
using VehicleService.Web.Services;

namespace VehicleService.Web.Controllers;

[Authorize(Roles = "VehicleOwner")]
public sealed class VehiclesController : Controller
{
    private readonly VehicleServiceApiClient _apiClient;

    public VehiclesController(VehicleServiceApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var vehicles = await _apiClient.GetAsync<List<VehicleViewModel>>("api/vehicles", cancellationToken);

            return View(vehicles ?? []);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Account");
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Unable to load vehicles";
            return View(new List<VehicleViewModel>());
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateVehicleViewModel
        {
            Year = DateTime.UtcNow.Year
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateVehicleViewModel createVehicleViewModel, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(createVehicleViewModel);
        }

        try
        {
            using var response = await _apiClient.PostAsync("api/vehicles", createVehicleViewModel, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Vehicle registered successfully.";

                return RedirectToAction(nameof(Index));
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToAction("Login", "Account");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(string.Empty, "Unable to register vehicle. Check the details and try again");

                return View(createVehicleViewModel);
            }

            ModelState.AddModelError(string.Empty, "An unexpected error occurred");

            return View(createVehicleViewModel);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Unable to connect to the API");

            return View(createVehicleViewModel);
        }
    }
}