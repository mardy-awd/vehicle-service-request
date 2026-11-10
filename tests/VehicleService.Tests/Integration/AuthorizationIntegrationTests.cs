
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using VehicleService.Api.Data;
using VehicleService.Api.Identity;
using VehicleService.Core.Entities;

namespace VehicleService.Tests.Integration;

public class AuthorizationIntegrationTests
{
    [Fact]
    public async Task GetVehicles_WithoutAuthentication_Returns401()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/vehicles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProviders_AsVehicleOwner_Returns403()
    {
        using var factory = CreateAuthenticatedFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/providers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetVehicle_BelongingToAnotherOwner_Returns404()
    {
        using var factory = CreateAuthenticatedFactory();
        using var client = factory.CreateClient();

        var vehicleId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Users.Add(new ApplicationUser
            {
                Id = "another-owner",
                UserName = "another@test.com",
                NormalizedUserName = "ANOTHER@TEST.COM",
                Email = "another@test.com",
                NormalizedEmail = "ANOTHER@TEST.COM",
                FullName = "Another Owner",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });

            db.Vehicles.Add(new Vehicle
            {
                Id = vehicleId,
                OwnerId = "another-owner",
                Make = "Volvo",
                Model = "XC60",
                RegistrationNumber = "TEST-OWN-001",
                Year = 2025,
                Mileage = 10000,
                CreatedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/vehicles/{vehicleId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateAuthenticatedFactory()
    {
        return new CustomWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions,
                            TestAuthenticationHandler>("Test", _ => { });

                    services.PostConfigure<AuthenticationOptions>(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                        options.DefaultForbidScheme = "Test";
                    });
                });
            });
    }

    private sealed class TestAuthenticationHandler
        : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-owner"),
                new Claim(ClaimTypes.Role, "VehicleOwner")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
