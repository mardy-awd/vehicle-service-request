using FluentAssertions;
using VehicleService.Api.Identity;
using VehicleService.Api.Services;
using VehicleService.Core.Entities;
using VehicleService.Core.Enums;
using VehicleService.Tests.Helpers;

namespace VehicleService.Tests.Services;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSummary_ShouldReturnRoleScopedStatistics()
    {
        // Arrange
        var (context, connection) = await TestDbContextFactory.CreateAsync();

        await using var db = context;
        await using var conn = connection;

        var admin1 = "admin-1";

        var provider1 = "provider-1";

        var owner1 = "owner-1";
        var owner2 = "owner-2";

        db.Users.AddRange(
            new ApplicationUser
            {
                Id = admin1,
                UserName = "admin1@test.com",
                NormalizedUserName = "ADMIN1@TEST.COM",
                Email = "admin1@test.com",
                NormalizedEmail = "ADMIN1@TEST.COM",
                FullName = "Admin One",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new ApplicationUser
            {
                Id = provider1,
                UserName = "provider1@test.com",
                NormalizedUserName = "PROVIDER1@TEST.COM",
                Email = "provider1@test.com",
                NormalizedEmail = "PROVIDER1@TEST.COM",
                FullName = "Provider One",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new ApplicationUser
            {
                Id = owner1,
                UserName = "owner1@test.com",
                NormalizedUserName = "OWNER1@TEST.COM",
                Email = "owner1@test.com",
                NormalizedEmail = "OWNER1@TEST.COM",
                FullName = "Owner One",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new ApplicationUser
            {
                Id = owner2,
                UserName = "owner2@test.com",
                NormalizedUserName = "OWNER2@TEST.COM",
                Email = "owner2@test.com",
                NormalizedEmail = "OWNER2@TEST.COM",
                FullName = "Owner Two",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync();

        var vehicle1 = new Vehicle
        {
            Id = Guid.NewGuid(),
            OwnerId = owner1,
            Make = "Volvo",
            Model = "XC60",
            RegistrationNumber = "ABC123",
            Year = 2025,
            Mileage = 10000,
            CreatedAtUtc = DateTime.UtcNow
        };

        var vehicle2 = new Vehicle
        {
            Id = Guid.NewGuid(),
            OwnerId = owner2,
            Make = "BMW",
            Model = "X3",
            RegistrationNumber = "XYZ456",
            Year = 2024,
            Mileage = 15000,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Vehicles.AddRange(vehicle1, vehicle2);

        var providerRequest = CreateRequest(
            owner2,
            vehicle2.Id,
            ServiceRequestStatus.InProgress);

        providerRequest.AssignedProviderId = provider1;

        db.ServiceRequests.AddRange(
            CreateRequest(owner1, vehicle1.Id, ServiceRequestStatus.Pending),

            CreateRequest(owner1, vehicle1.Id, ServiceRequestStatus.Completed),

            providerRequest
        );

        await db.SaveChangesAsync();

        var service = new DashboardService(db);

        // Act

        var adminResult = await service.GetSummary(admin1, AppRoles.Admin);
        var providerResult = await service.GetSummary(provider1, AppRoles.ServiceProvider);
        var ownerResult = await service.GetSummary(owner1, AppRoles.VehicleOwner);

        // Assert

        // Admin
        adminResult.TotalRequests.Should().Be(3);
        adminResult.PendingRequests.Should().Be(1);
        adminResult.CompletedRequests.Should().Be(1);
        adminResult.InProgressRequests.Should().Be(1);
        adminResult.ActiveRequests.Should().Be(1);
        adminResult.TotalVehicles.Should().BeNull();

        // Provider
        providerResult.TotalRequests.Should().Be(1);
        providerResult.InProgressRequests.Should().Be(1);
        providerResult.PendingRequests.Should().Be(0);
        providerResult.ActiveRequests.Should().Be(1);
        providerResult.TotalVehicles.Should().BeNull();

        // Owner
        ownerResult.TotalRequests.Should().Be(2);
        ownerResult.PendingRequests.Should().Be(1);
        ownerResult.CompletedRequests.Should().Be(1);
        ownerResult.InProgressRequests.Should().Be(0);
        ownerResult.TotalVehicles.Should().Be(1);
        ownerResult.ActiveRequests.Should().Be(0);
    }

    private static ServiceRequest CreateRequest(
        string ownerId,
        Guid vehicleId,
        ServiceRequestStatus status)
    {
        return new ServiceRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = $"SR-{Guid.NewGuid():N}"[..30],
            OwnerId = ownerId,
            VehicleId = vehicleId,
            ServiceType = ServiceType.GeneralService,
            IssueDescription = "General vehicle inspection",
            PreferredServiceAtUtc = DateTime.UtcNow.AddDays(1),
            TimeSlot = ServiceTimeSlot.Morning,
            PickupRequired = false,
            ContactPhoneNumber = "01012345678",
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Version = 1
        };
    }
}