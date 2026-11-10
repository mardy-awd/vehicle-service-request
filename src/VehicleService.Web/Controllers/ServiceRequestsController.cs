using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;
using VehicleService.Web.Models.Common;
using VehicleService.Web.Models.ServiceRequests;
using VehicleService.Web.Models.Vehicles;
using VehicleService.Web.Services;

namespace VehicleService.Web.Controllers;

[Authorize(Roles = "Admin,VehicleOwner,ServiceProvider")]
public sealed class ServiceRequestsController : Controller
{
    private readonly VehicleServiceApiClient _apiClient;

    public ServiceRequestsController(VehicleServiceApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? status = null, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);

        const int pageSize = 10;

        var endpoint = $"api/service-requests?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(status))
        {
            endpoint += $"&status={Uri.EscapeDataString(status)}";
        }

        try
        {
            var result = await _apiClient.GetAsync<PagedResultViewModel<ServiceRequestViewModel>>(
                    endpoint,
                    cancellationToken);

            ViewBag.SelectedStatus = status;

            return View(result ?? new PagedResultViewModel<ServiceRequestViewModel>());
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Account");
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Unable to load service requests.";

            return View(new PagedResultViewModel<ServiceRequestViewModel>());
        }
    }

    [HttpGet]
    [Authorize(Roles = "VehicleOwner")]
    public async Task<IActionResult> Create(
    CancellationToken cancellationToken)
    {
        var model = new CreateServiceRequestViewModel();

        await PopulateVehicles(model, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "VehicleOwner")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
    CreateServiceRequestViewModel model,
    CancellationToken cancellationToken)
    {
        if (model.PickupRequired &&
            string.IsNullOrWhiteSpace(model.PickupAddress))
        {
            ModelState.AddModelError(
                nameof(model.PickupAddress),
                "Pickup address is required when pickup is selected.");
        }

        var preferredUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(
                model.PreferredServiceAtUtc,
                DateTimeKind.Unspecified),
            TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"));

        if (preferredUtc <= DateTime.UtcNow)
        {
            ModelState.AddModelError(
                nameof(model.PreferredServiceAtUtc),
                "Preferred service date must be in the future.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateVehicles(model, cancellationToken);
            return View(model);
        }

        var request = new
        {
            model.VehicleId,
            model.ServiceType,
            model.IssueDescription,
            PreferredServiceAtUtc = preferredUtc,
            model.TimeSlot,
            model.PickupRequired,
            PickupAddress = model.PickupRequired
                ? model.PickupAddress
                : null,
            model.ContactPhoneNumber
        };

        try
        {
            using var response = await _apiClient.PostAsync(
                "api/service-requests",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Service request created successfully.";

                return RedirectToAction(nameof(Index));
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return RedirectToAction("Login", "Account");

            ModelState.AddModelError(
                string.Empty,
                response.StatusCode == HttpStatusCode.BadRequest
                    ? "Please check the request details."
                    : "Unable to create service request.");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Unable to connect to the API.");
        }

        await PopulateVehicles(model, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        try
        {
            var request = await _apiClient.GetAsync<ServiceRequestViewModel>($"api/service-requests/{id}", cancellationToken);

            if (request is null)
            {
                return NotFound();

            }

            var timeline = await _apiClient.GetAsync<ServiceRequestTimelineViewModel>($"api/service-requests/{id}/timeline", cancellationToken);

            var model = new ServiceRequestDetailsViewModel
            {
                Request = request,
                Timeline = timeline ?? new()
            };

            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Account");
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Unable to load service request details.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Assign(Guid id, CancellationToken cancellationToken)
    {
        var request = await _apiClient.GetAsync<ServiceRequestViewModel>($"api/service-requests/{id}", cancellationToken);

        if (request is null)
            return NotFound();

        if (request.Status != "Pending")
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new AssignProviderViewModel
        {
            ServiceRequestId = id
        };

        await PopulateProviders(model, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignProviderViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateProviders(model, cancellationToken);
            return View(model);
        }

        try
        {
            using var response = await _apiClient.PostAsync($"api/service-requests/{model.ServiceRequestId}/assign",
                new { model.ProviderId },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Service provider assigned successfully.";

                return RedirectToAction(nameof(Details), new { id = model.ServiceRequestId });
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToAction("Login", "Account");
            }

            ModelState.AddModelError(
                string.Empty,
                response.StatusCode == HttpStatusCode.Conflict
                    ? "This request can no longer be assigned."
                    : "Unable to assign the selected provider.");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Unable to connect to the API.");
        }

        await PopulateProviders(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "ServiceProvider")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _apiClient.PostAsync($"api/service-requests/{id}/accept",
                new { },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Service request accepted successfully";
            }
            else
            {
                TempData["Error"] = "Unable to accept this service request. Please check its current status";
            }
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Unable to connect to the API";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "ServiceProvider")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RejectServiceRequestViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please provide a rejection reason between 5 and 500 characters";

            return RedirectToAction(
                nameof(Details),
                new { id = model.ServiceRequestId });
        }

        try
        {
            using var response = await _apiClient.PostAsync($"api/service-requests/{model.ServiceRequestId}/reject",
                new { model.Reason },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Service request rejected successfully";
            }
            else
            {
                TempData["Error"] = "Unable to reject this service request. Please check its current status";
            }
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Unable to connect to the API";
        }

        return RedirectToAction(nameof(Index), new { id = model.ServiceRequestId });
    }

    [HttpPost]
    [Authorize(Roles = "ServiceProvider")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(StartServiceRequestViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.EstimatedCompletionAtUtc is null)
        {
            TempData["Error"] = "Enter technician notes and an estimated completion date";

            return RedirectToAction(nameof(Details), new { id = model.ServiceRequestId });
        }

        var localDateTime = DateTime.SpecifyKind(model.EstimatedCompletionAtUtc.Value, DateTimeKind.Local);

        var estimatedCompletionUtc = localDateTime.ToUniversalTime();

        if (estimatedCompletionUtc <= DateTime.UtcNow)
        {
            TempData["Error"] = "Estimated completion must be in the future";

            return RedirectToAction(nameof(Details), new { id = model.ServiceRequestId });
        }

        try
        {
            using var response = await _apiClient.PostAsync($"api/service-requests/{model.ServiceRequestId}/start",
                new
                {
                    model.TechnicianNotes,
                    EstimatedCompletionAtUtc = estimatedCompletionUtc
                },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Service started successfully";
            }
            else
            {
                TempData["Error"] =
                    response.StatusCode == HttpStatusCode.Conflict
                        ? "The request is no longer in Accepted status"
                        : "Unable to start this service request";
            }
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Unable to connect to the API";
        }

        return RedirectToAction(nameof(Details), new { id = model.ServiceRequestId });
    }

    [HttpPost]
    [Authorize(Roles = "ServiceProvider")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProgress(AddServiceRequestProgressViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Technician notes must contain between 5 and 2000 characters";

            return RedirectToAction(nameof(Details), new { id = model.ServiceRequestId });
        }

        DateTime? estimatedCompletionUtc = null;

        if (model.EstimatedCompletionAtUtc.HasValue)
        {
            var localDateTime = DateTime.SpecifyKind(model.EstimatedCompletionAtUtc.Value, DateTimeKind.Local);

            estimatedCompletionUtc = localDateTime.ToUniversalTime();

            if (estimatedCompletionUtc <= DateTime.UtcNow)
            {
                TempData["Error"] = "Estimated completion must be in the future";

                return RedirectToAction(nameof(Details), new { id = model.ServiceRequestId });
            }
        }

        try
        {
            using var response = await _apiClient.PostAsync($"api/service-requests/{model.ServiceRequestId}/progress",
                new
                {
                    model.TechnicianNotes,
                    EstimatedCompletionAtUtc = estimatedCompletionUtc
                },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Progress update added successfully";
            }
            else
            {
                TempData["Error"] =
                    response.StatusCode == HttpStatusCode.Conflict
                        ? "This request is no longer in progress"
                        : "Unable to add the progress update";
            }
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Unable to connect to the API";
        }

        return RedirectToAction(nameof(Details), new { id = model.ServiceRequestId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ServiceProvider")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(CompleteServiceRequestViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter valid completion notes and a non-negative final amount";

            return RedirectToAction(nameof(Details), new { id = model.ServiceRequestId });
        }

        try
        {
            using var response = await _apiClient.PostAsync($"api/service-requests/{model.ServiceRequestId}/complete",
                new
                {
                    model.CompletionNotes,
                    model.FinalAmount
                },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Service completed successfully";
            }
            else
            {
                TempData["Error"] =
                    response.StatusCode == HttpStatusCode.Conflict
                        ? "This request can no longer be completed"
                        : "Unable to complete the service request";
            }
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Unable to connect to the API";
        }

        return RedirectToAction(nameof(Details), new { id = model.ServiceRequestId });
    }

    [HttpPost]
    [Authorize(Roles = "VehicleOwner")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _apiClient.PostAsync($"api/service-requests/{id}/close",
                new { },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Service request closed successfully";
            }
            else
            {
                TempData["Error"] =
                    response.StatusCode == HttpStatusCode.Conflict
                        ? "This request is no longer eligible for closure"
                        : "Unable to close the service request";
            }
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Unable to connect to the API";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,VehicleOwner")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(CancelServiceRequestViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please enter a cancellation reason between 5 and 500 characters";

            return RedirectToAction(
                nameof(Details),
                new { id = model.ServiceRequestId });
        }

        try
        {
            using var response = await _apiClient.PostAsync($"api/service-requests/{model.ServiceRequestId}/cancel",
                new { model.Reason },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Service request cancelled successfully";
            }
            else
            {
                TempData["Error"] =
                    response.StatusCode == HttpStatusCode.Conflict
                        ? "This request can no longer be cancelled"
                        : "Unable to cancel the service request";
            }
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Unable to connect to the API";
        }

        return RedirectToAction(nameof(Details), new { id = model.ServiceRequestId });
    }

    private async Task PopulateProviders(AssignProviderViewModel model, CancellationToken cancellationToken)
    {
        var providers = await _apiClient.GetAsync<List<ServiceProviderViewModel>>("api/admin/providers", cancellationToken);

        model.Providers = (providers ?? [])
            .Select(x => new SelectListItem
            {
                Value = x.Id,
                Text = $"{x.FullName} ({x.Email})"
            })
            .ToList();
    }

    private async Task PopulateVehicles(CreateServiceRequestViewModel model, CancellationToken cancellationToken)
    {
        var vehicles = await _apiClient.GetAsync<List<VehicleViewModel>>("api/vehicles", cancellationToken);

        model.Vehicles = (vehicles ?? [])
            .Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.Make} {v.Model} ({v.RegistrationNumber})"
            })
            .ToList();
    }
}