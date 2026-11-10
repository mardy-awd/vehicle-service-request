using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using VehicleService.Web.Models.Account;
using VehicleService.Web.Services;

namespace VehicleService.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly VehicleServiceApiClient _apiClient;

    public AccountController(IHttpClientFactory httpClientFactory, VehicleServiceApiClient apiClient)
    {
        _httpClientFactory = httpClientFactory;
        _apiClient = apiClient;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        HttpResponseMessage response;

        try
        {
            response = await _apiClient.PostAsync("api/auth/login",
                new
                {
                    model.Email,
                    model.Password
                },
                cancellationToken, requiresAuthentication: false);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Unable to connect to the authentication service");

            return View(model);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            ModelState.AddModelError(string.Empty, "The authentication service did not respond in time");

            return View(model);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password");

                return View(model);
            }

            var loginResult = await response.Content.ReadFromJsonAsync<LoginApiResponse>(cancellationToken: cancellationToken);

            if (loginResult is null ||
                string.IsNullOrWhiteSpace(loginResult.AccessToken) ||
                string.IsNullOrWhiteSpace(loginResult.UserId) ||
                string.IsNullOrWhiteSpace(loginResult.Role))
            {
                ModelState.AddModelError(string.Empty, "Invalid authentication response");

                return View(model);
            }

            var expiresAt = new DateTimeOffset(DateTime.SpecifyKind(loginResult.ExpiresAtUtc, DateTimeKind.Utc));

            if (expiresAt <= DateTimeOffset.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "The authentication token has expired");

                return View(model);
            }

            var claims = new List<Claim>
            {
                new( ClaimTypes.NameIdentifier,loginResult.UserId),

                new(ClaimTypes.Name,loginResult.Email),

                new(ClaimTypes.Email,loginResult.Email),

                new(ClaimTypes.Role,loginResult.Role),

                new("access_token",loginResult.AccessToken)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var authenticationProperties =
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = expiresAt,
                    AllowRefresh = false
                };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authenticationProperties);

            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        using var response = await _apiClient.PostAsync("api/auth/register",
            new
            {
                model.FullName,
                model.Email,
                model.Password
            },
            cancellationToken, requiresAuthentication: false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);

            ModelState.AddModelError(
                string.Empty,
                string.IsNullOrWhiteSpace(error)
                    ? "Registration failed. Please try again."
                    : error);

            return View(model);
        }

        TempData["Success"] = "Account created successfully.";

        return RedirectToAction(nameof(Login));
    }
}