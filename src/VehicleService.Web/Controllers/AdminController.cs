using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using VehicleService.Web.Models.Admin;
using VehicleService.Web.Services;

namespace VehicleService.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly VehicleServiceApiClient _apiClient;

    public AdminController(VehicleServiceApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public IActionResult CreateProvider()
    {
        return View(new CreateProviderViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProvider(CreateProviderViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            using var response = await _apiClient.PostAsync("api/admin/providers",
                new
                {
                    model.FullName,
                    model.Email,
                    model.Password
                },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Service provider created successfully";

                return RedirectToAction(nameof(CreateProvider));
            }

            ModelState.AddModelError(
                string.Empty,
                response.StatusCode == HttpStatusCode.Conflict
                    ? "A user with this email already exists."
                    : "Unable to create service provider.");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Unable to connect to the API.");
        }

        return View(model);
    }
}