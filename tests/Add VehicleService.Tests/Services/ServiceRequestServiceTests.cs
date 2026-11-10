//using FluentAssertions;
//using Microsoft.AspNetCore.Identity;
//using Moq;
//using VehicleService.Api.Data;
//using VehicleService.Api.Identity;
//using VehicleService.Api.Services;
//using VehicleService.Api.Services.Results;
//using VehicleService.Core.Entities;
//using VehicleService.Core.Enums;
//using VehicleService.Tests.Helpers;

//namespace Add_VehicleService.Tests.Services
//{

//    public class ServiceRequestServiceTests
//    {
//        [Fact]
//        public async Task AcceptAsync_WhenProviderIsNotAssigned_ReturnsNotFound()
//        {
//            // Arrange
//            var (context, connection) =
//                await TestDbContextFactory.CreateAsync();

//            await using var db = context;
//            await using var conn = connection;

//            var ownerId = "owner-1";
//            var assignedProviderId = "provider-1";
//            var otherProviderId = "provider-2";

//            db.Users.AddRange(
//                CreateUser(ownerId),
//                CreateUser(assignedProviderId),
//                CreateUser(otherProviderId));

//            await db.SaveChangesAsync();

//            var vehicle = CreateVehicle(ownerId);
//            db.Vehicles.Add(vehicle);

//            var request = CreateRequest(
//                ownerId,
//                vehicle.Id,
//                ServiceRequestStatus.Assigned);

//            request.AssignedProviderId = assignedProviderId;

//            db.ServiceRequests.Add(request);
//            await db.SaveChangesAsync();

//            var service = CreateService(db);

//            // Act
//            var result = await service.Accept(request.Id, otherProviderId);

//            // Assert
//            result.IsSuccess.Should().BeFalse();
//            result.Error.Should().Be(ServiceError.NotFound);

//            var savedRequest = await db.ServiceRequests.FindAsync(request.Id);

//            savedRequest!.Status.Should().Be(ServiceRequestStatus.Assigned);
//        }

//        [Fact]
//        public async Task AcceptAsync_WhenRequestIsPending_ReturnsConflict()
//        {
//            // Arrange
//            var (context, connection) =
//                await TestDbContextFactory.CreateAsync();

//            await using var db = context;
//            await using var conn = connection;

//            var ownerId = "owner-1";
//            var providerId = "provider-1";

//            db.Users.AddRange(
//                CreateUser(ownerId),
//                CreateUser(providerId));

//            await db.SaveChangesAsync();

//            var vehicle = CreateVehicle(ownerId);
//            db.Vehicles.Add(vehicle);

//            var request = CreateRequest(
//                ownerId,
//                vehicle.Id,
//                ServiceRequestStatus.Pending);

//            request.AssignedProviderId = providerId;

//            db.ServiceRequests.Add(request);
//            await db.SaveChangesAsync();

//            var service = CreateService(db);

//            // Act
//            var result = await service.Accept(
//                request.Id,
//                providerId);

//            // Assert
//            result.IsSuccess.Should().BeFalse();
//            result.Error.Should().Be(ServiceError.Conflict);

//            var savedRequest = await db.ServiceRequests.FindAsync(request.Id);

//            savedRequest!.Status.Should().Be(ServiceRequestStatus.Pending);
//        }

//        private static ApplicationUser CreateUser(string id)
//        {
//            return new ApplicationUser
//            {
//                Id = id,
//                UserName = $"{id}@test.com",
//                NormalizedUserName = $"{id.ToUpperInvariant()}@TEST.COM",
//                Email = $"{id}@test.com",
//                NormalizedEmail = $"{id.ToUpperInvariant()}@TEST.COM",
//                FullName = id,
//                IsActive = true,
//                CreatedAtUtc = DateTime.UtcNow
//            };
//        }

//        private static Vehicle CreateVehicle(string ownerId)
//        {
//            return new Vehicle
//            {
//                Id = Guid.NewGuid(),
//                OwnerId = ownerId,
//                Make = "Volvo",
//                Model = "XC60",
//                RegistrationNumber = $"REG-{Guid.NewGuid():N}"[..15],
//                Year = 2025,
//                Mileage = 10000,
//                CreatedAtUtc = DateTime.UtcNow
//            };
//        }

//        private static ServiceRequest CreateRequest(
//            string ownerId,
//            Guid vehicleId,
//            ServiceRequestStatus status)
//        {
//            return new ServiceRequest
//            {
//                Id = Guid.NewGuid(),
//                RequestNumber = $"SR-{Guid.NewGuid():N}"[..30],
//                OwnerId = ownerId,
//                VehicleId = vehicleId,
//                ServiceType = ServiceType.GeneralService,
//                IssueDescription = "General vehicle inspection",
//                PreferredServiceAtUtc = DateTime.UtcNow.AddDays(1),
//                TimeSlot = ServiceTimeSlot.Morning,
//                PickupRequired = false,
//                ContactPhoneNumber = "01012345678",
//                Status = status,
//                CreatedAtUtc = DateTime.UtcNow,
//                UpdatedAtUtc = DateTime.UtcNow,
//                Version = 1
//            };
//        }

//        private static ServiceRequestService CreateService(
//            AppDbContext db)
//        {
//            var userManager = new Mock<UserManager<ApplicationUser>>(
//                Mock.Of<IUserStore<ApplicationUser>>(),
//                null!,
//                null!,
//                null!,
//                null!,
//                null!,
//                null!,
//                null!,
//                null!);

//            return new ServiceRequestService(db, userManager.Object);
//        }
//    }
//}
