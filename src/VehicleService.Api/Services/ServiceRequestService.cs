using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VehicleService.Api.Caching;
using VehicleService.Api.Contracts.Common;
using VehicleService.Api.Contracts.ServiceRequests;
using VehicleService.Api.Data;
using VehicleService.Api.Identity;
using VehicleService.Api.Services.Interfaces;
using VehicleService.Api.Services.Results;
using VehicleService.Core.Entities;
using VehicleService.Core.Enums;

namespace VehicleService.Api.Services
{
    public class ServiceRequestService : IServiceRequestService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDashboardCacheInvalidator _dashboardCacheInvalidator;

        public ServiceRequestService(AppDbContext context, UserManager<ApplicationUser> userManager, IDashboardCacheInvalidator dashboardCacheInvalidator)
        {
            _context = context;
            _userManager = userManager;
            _dashboardCacheInvalidator = dashboardCacheInvalidator;
        }

        public async Task<ServiceResult<ServiceRequestResponse>> Create(string ownerId, CreateServiceRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Forbidden, "Invalid owner");
            }

            if (request.VehicleId == Guid.Empty)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "Vehicle ID is required");
            }

            if (!Enum.IsDefined(request.ServiceType) ||
                !Enum.IsDefined(request.TimeSlot))
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "Invalid service type or time slot");
            }

            if (string.IsNullOrWhiteSpace(request.IssueDescription))
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "Issue description is required");
            }

            if (request.PreferredServiceAtUtc.Kind != DateTimeKind.Utc ||
                request.PreferredServiceAtUtc <= DateTime.UtcNow)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "Preferred service date must be a future UTC date");
            }

            if (request.PickupRequired && string.IsNullOrWhiteSpace(request.PickupAddress))
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "Pickup address is required when pickup is requested");
            }

            var vehicleExists = await _context.Vehicles
                .AnyAsync(
                    v => v.Id == request.VehicleId &&
                         v.OwnerId == ownerId,
                    cancellationToken);

            if (!vehicleExists)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.NotFound, "Vehicle not found");
            }

            var now = DateTime.UtcNow;
            var uniquePart = Guid.NewGuid()
                .ToString("N")[..12]
                .ToUpperInvariant();

            var serviceRequest = new ServiceRequest
            {
                Id = Guid.NewGuid(),
                RequestNumber = $"SR-{now:yyyyMMdd}-{uniquePart}",
                OwnerId = ownerId,
                VehicleId = request.VehicleId,
                ServiceType = request.ServiceType,
                IssueDescription = request.IssueDescription.Trim(),
                PreferredServiceAtUtc = request.PreferredServiceAtUtc,
                TimeSlot = request.TimeSlot,
                PickupRequired = request.PickupRequired,
                PickupAddress = request.PickupRequired
                    ? request.PickupAddress?.Trim()
                    : null,
                ContactPhoneNumber = request.ContactPhoneNumber.Trim(),
                Status = ServiceRequestStatus.Pending,
                AssignedProviderId = null,
                EstimatedCompletionAtUtc = null,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Version = 1
            };

            serviceRequest.StatusHistories.Add(
                new ServiceRequestStatusHistory
                {
                    ServiceRequestId = serviceRequest.Id,
                    ChangedByUserId = ownerId,
                    FromStatus = null,
                    ToStatus = ServiceRequestStatus.Pending,
                    ChangedAtUtc = now,
                    Notes = "Service request created"
                });

            _context.ServiceRequests.Add(serviceRequest);

            await _context.SaveChangesAsync(cancellationToken);

            await _dashboardCacheInvalidator.InvalidateAsync(ownerId, providerId: null, cancellationToken);

            return ServiceResult<ServiceRequestResponse>.Success(MapToResponse(serviceRequest));
        }

        public async Task<PagedResult<ServiceRequestResponse>> GetAll(string userId, string role, ServiceRequestQuery request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID is required", nameof(userId));
            }

            var query = _context.ServiceRequests
                .AsNoTracking()
                .AsQueryable();

            switch (role)
            {
                case AppRoles.Admin:
                    break;

                case AppRoles.VehicleOwner:
                    query = query.Where(x => x.OwnerId == userId);
                    break;

                case AppRoles.ServiceProvider:
                    query = query.Where(x => x.AssignedProviderId == userId);
                    break;

                default:
                    throw new ArgumentException("Unsupported user role", nameof(role));
            }

            if (request.Status.HasValue)
            {
                query = query.Where(x => x.Status == request.Status.Value);
            }

            if (request.ServiceType.HasValue)
            {
                query = query.Where(x => x.ServiceType == request.ServiceType.Value);
            }

            if (request.FromDateUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc >= request.FromDateUtc.Value);
            }

            if (request.ToDateUtc.HasValue)
            {
                query = query.Where(x =>
                    x.CreatedAtUtc <= request.ToDateUtc.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x =>
                    EF.Functions.ILike(x.RequestNumber, $"%{search}%") ||
                    EF.Functions.ILike(x.IssueDescription, $"%{search}%") ||
                    EF.Functions.ILike(x.Vehicle.Make, $"%{search}%") ||
                    EF.Functions.ILike(x.Vehicle.Model, $"%{search}%") ||
                    EF.Functions.ILike(x.Vehicle.RegistrationNumber, $"%{search}%"));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .ThenByDescending(x => x.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new ServiceRequestResponse
                {
                    Id = x.Id,
                    RequestNumber = x.RequestNumber,
                    VehicleId = x.VehicleId,
                    OwnerId = x.OwnerId,
                    AssignedProviderId = x.AssignedProviderId,
                    ServiceType = x.ServiceType,
                    IssueDescription = x.IssueDescription,
                    PreferredServiceAtUtc = x.PreferredServiceAtUtc,
                    TimeSlot = x.TimeSlot,
                    PickupRequired = x.PickupRequired,
                    PickupAddress = x.PickupAddress,
                    ContactPhoneNumber = x.ContactPhoneNumber,
                    Status = x.Status,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<ServiceRequestResponse>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<ServiceResult<ServiceRequestResponse>> GetById(
            Guid id,
            string userId,
            string role,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "A service request ID is required");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Forbidden, "Invalid user");
            }

            var query = _context.ServiceRequests
                .AsNoTracking()
                .AsQueryable();

            switch (role)
            {
                case AppRoles.Admin:
                    break;

                case AppRoles.VehicleOwner:
                    query = query.Where(x => x.OwnerId == userId);
                    break;

                case AppRoles.ServiceProvider:
                    query = query.Where(x => x.AssignedProviderId == userId);
                    break;

                default:
                    return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Forbidden, "Access denied");
            }

            var serviceRequest = await query
                .Where(x => x.Id == id)
                .Select(x => new ServiceRequestResponse
                {
                    Id = x.Id,
                    RequestNumber = x.RequestNumber,
                    VehicleId = x.VehicleId,
                    OwnerId = x.OwnerId,
                    AssignedProviderId = x.AssignedProviderId,
                    ServiceType = x.ServiceType,
                    IssueDescription = x.IssueDescription,
                    PreferredServiceAtUtc = x.PreferredServiceAtUtc,
                    TimeSlot = x.TimeSlot,
                    PickupRequired = x.PickupRequired,
                    PickupAddress = x.PickupAddress,
                    ContactPhoneNumber = x.ContactPhoneNumber,
                    Status = x.Status,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (serviceRequest is null)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.NotFound, "Service request not found");
            }

            return ServiceResult<ServiceRequestResponse>.Success(serviceRequest);
        }

        public async Task<ServiceResult<ServiceRequestResponse>> AssignProvider(
            Guid id,
            string adminId,
            AssignServiceProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(request.ProviderId))
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "A valid service request ID and provider ID are required");
            }

            if (string.IsNullOrWhiteSpace(adminId))
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Forbidden, "Invalid administrator");
            }

            var serviceRequest = await _context.ServiceRequests
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (serviceRequest is null)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.NotFound, "Service request not found");
            }

            if (serviceRequest.Status != ServiceRequestStatus.Pending)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "only pending service requests can be assigned");
            }

            var provider = await _userManager.FindByIdAsync(
                request.ProviderId.Trim());

            if (provider is null ||
                !provider.IsActive ||
                !await _userManager.IsInRoleAsync(provider, AppRoles.ServiceProvider))
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "selected service provider is invalid or inactive");
            }

            serviceRequest.AssignedProviderId = provider.Id;

            ApplyStatusTransition(
                serviceRequest,
                ServiceRequestStatus.Assigned,
                adminId,
                string.IsNullOrWhiteSpace(request.Notes) ? "Service provider assigned" : request.Notes.Trim(),
                provider.Id);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "This service request was modified by another user. refresh and try again");
            }

            await _dashboardCacheInvalidator.InvalidateAsync(serviceRequest.OwnerId, serviceRequest.AssignedProviderId, cancellationToken);

            return ServiceResult<ServiceRequestResponse>.Success(MapToResponse(serviceRequest));
        }

        public async Task<ServiceResult<ServiceRequestResponse>> Accept(
            Guid id,
            string providerId,
            CancellationToken cancellationToken = default)
        {
            var serviceRequest = await _context.ServiceRequests
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (serviceRequest is null)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.NotFound, "Service request not found");
            }

            if (serviceRequest.AssignedProviderId != providerId)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.NotFound, "Service request not found");
            }

            if (serviceRequest.Status != ServiceRequestStatus.Assigned)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "Only assigned requests can be accepted");
            }

            ApplyStatusTransition(
                serviceRequest,
                ServiceRequestStatus.Accepted,
                providerId,
                "Service request accepted by provider",
                providerId);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "The service request was modified. Refresh and try again");
            }

            await _dashboardCacheInvalidator.InvalidateAsync(serviceRequest.OwnerId, serviceRequest.AssignedProviderId, cancellationToken);

            return ServiceResult<ServiceRequestResponse>.Success(MapToResponse(serviceRequest));
        }

        public async Task<ServiceResult<ServiceRequestResponse>> Reject(
            Guid id,
            string providerId,
            RejectServiceRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Reason) ||
                request.Reason.Trim().Length < 5)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "A rejection reason of at least 5 characters is required");
            }

            var serviceRequest = await _context.ServiceRequests
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (serviceRequest is null ||
                serviceRequest.AssignedProviderId != providerId)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.NotFound, "Service request not found");
            }

            if (serviceRequest.Status != ServiceRequestStatus.Assigned)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "Only assigned requests can be rejected");
            }

            var previousProviderId = serviceRequest.AssignedProviderId;

            ApplyStatusTransition(
                serviceRequest,
                ServiceRequestStatus.Pending,
                providerId,
                request.Reason.Trim(),
                providerId);

            serviceRequest.AssignedProviderId = null;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "The service request was modified. Refresh and try again");
            }

            await _dashboardCacheInvalidator.InvalidateAsync(serviceRequest.OwnerId, previousProviderId, cancellationToken);

            return ServiceResult<ServiceRequestResponse>.Success(MapToResponse(serviceRequest));
        }

        public async Task<ServiceResult<ServiceRequestResponse>> Start(
            Guid id,
            string providerId,
            StartServiceRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "A valid service request ID is required");
            }

            if (string.IsNullOrWhiteSpace(providerId))
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Forbidden, "Invalid service provider");
            }

            if (string.IsNullOrWhiteSpace(request.TechnicianNotes) ||
                request.TechnicianNotes.Trim().Length < 5 ||
                request.TechnicianNotes.Trim().Length > 2000)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "Technician notes must contain between 5 and 2000 characters");
            }

            var now = DateTime.UtcNow;

            if (request.EstimatedCompletionAtUtc.Kind != DateTimeKind.Utc || request.EstimatedCompletionAtUtc <= now)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "Estimated completion must be a future UTC date");
            }

            var serviceRequest = await _context.ServiceRequests
                .FirstOrDefaultAsync(
                    x => x.Id == id &&
                         x.AssignedProviderId == providerId,
                    cancellationToken);

            if (serviceRequest is null)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.NotFound, "Service request not found");
            }

            if (serviceRequest.Status != ServiceRequestStatus.Accepted)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "Only accepted service requests can be started");
            }

            serviceRequest.EstimatedCompletionAtUtc = request.EstimatedCompletionAtUtc;

            serviceRequest.Progresses.Add(new ServiceRequestProgress
            {
                ServiceRequestId = serviceRequest.Id,
                ProviderId = providerId,
                TechnicianNotes = request.TechnicianNotes.Trim(),
                EstimatedCompletionAtUtc = request.EstimatedCompletionAtUtc,
                CreatedAtUtc = now
            });

            ApplyStatusTransition(
                serviceRequest,
                ServiceRequestStatus.InProgress,
                providerId,
                "Service work started",
                providerId);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "This service request was modified by another user. Refresh and try again");
            }

            await _dashboardCacheInvalidator.InvalidateAsync(serviceRequest.OwnerId, serviceRequest.AssignedProviderId, cancellationToken);

            return ServiceResult<ServiceRequestResponse>.Success(MapToResponse(serviceRequest));
        }

        public async Task<ServiceResult<ServiceRequestResponse>> AddProgress(
            Guid id,
            string providerId,
            AddServiceRequestProgressRequest request,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "A valid service request ID is required");
            }

            if (string.IsNullOrWhiteSpace(providerId))
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Forbidden, "Invalid service provider");
            }

            var notes = request.TechnicianNotes?.Trim();

            if (string.IsNullOrWhiteSpace(notes) ||
                notes.Length < 5 ||
                notes.Length > 2000)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "Technician notes must contain between 5 and 2000 characters");
            }

            var now = DateTime.UtcNow;

            if (request.EstimatedCompletionAtUtc.HasValue)
            {
                var estimatedCompletion = request.EstimatedCompletionAtUtc.Value;

                if (estimatedCompletion.Kind != DateTimeKind.Utc ||
                    estimatedCompletion <= now)
                {
                    return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "Estimated completion must be a future UTC date");
                }
            }

            var serviceRequest = await _context.ServiceRequests
                .FirstOrDefaultAsync(
                    x => x.Id == id &&
                         x.AssignedProviderId == providerId,
                    cancellationToken);

            if (serviceRequest is null)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.NotFound, "Service request not found");
            }

            if (serviceRequest.Status != ServiceRequestStatus.InProgress)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "Progress can only be added to requests that are in progress");
            }

            if (request.EstimatedCompletionAtUtc.HasValue)
            {
                serviceRequest.EstimatedCompletionAtUtc =
                    request.EstimatedCompletionAtUtc.Value;
            }

            serviceRequest.UpdatedAtUtc = now;
            serviceRequest.Version++;

            serviceRequest.Progresses.Add(new ServiceRequestProgress
            {
                ServiceRequestId = serviceRequest.Id,
                ProviderId = providerId,
                TechnicianNotes = notes,
                EstimatedCompletionAtUtc =
                    serviceRequest.EstimatedCompletionAtUtc,
                CreatedAtUtc = now
            });

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict,
                    "This service request was modified by another user. Refresh and try again");
            }

            return ServiceResult<ServiceRequestResponse>.Success(MapToResponse(serviceRequest));
        }

        public async Task<ServiceResult<ServiceRequestResponse>> Complete(
            Guid id,
            string providerId,
            CompleteServiceRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "A valid service request ID is required");
            }

            if (string.IsNullOrWhiteSpace(providerId))
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Forbidden, "Invalid service provider");
            }

            var notes = request.CompletionNotes?.Trim();

            if (string.IsNullOrWhiteSpace(notes) ||
                notes.Length < 5 ||
                notes.Length > 2000)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "Completion notes must contain between 5 and 2000 characters");
            }

            if (request.FinalAmount is < 0 or > 999999999.99m)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "Final amount must be a valid nonnegative value");
            }

            var serviceRequest = await _context.ServiceRequests
                .FirstOrDefaultAsync(
                    x => x.Id == id &&
                         x.AssignedProviderId == providerId,
                    cancellationToken);

            if (serviceRequest is null)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.NotFound, "Service request not found");
            }

            if (serviceRequest.Status != ServiceRequestStatus.InProgress)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "Only requests that are in progress can be completed");
            }

            var alreadyCompleted = await _context.ServiceCompletions
                .AnyAsync(
                    x => x.ServiceRequestId == id,
                    cancellationToken);

            if (alreadyCompleted)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "A completion record already exists for this request");
            }

            var now = DateTime.UtcNow;

            serviceRequest.Completion = new ServiceCompletion
            {
                ServiceRequestId = serviceRequest.Id,
                CompletedByUserId = providerId,
                CompletionNotes = notes,
                CompletedAtUtc = now,
                FinalAmount = request.FinalAmount
            };

            ApplyStatusTransition(
                serviceRequest,
                ServiceRequestStatus.Completed,
                providerId,
                "Service work completed",
                providerId);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict,
                    "This service request was modified by another user. Refresh and try again");
            }

            await _dashboardCacheInvalidator.InvalidateAsync(serviceRequest.OwnerId, serviceRequest.AssignedProviderId, cancellationToken);

            return ServiceResult<ServiceRequestResponse>.Success(MapToResponse(serviceRequest));
        }

        public async Task<ServiceResult<ServiceRequestResponse>> Close(
            Guid id,
            string userId,
            string role,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "A valid service request ID is required");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Forbidden, "Invalid user");
            }

            if (role != AppRoles.Admin &&
                role != AppRoles.VehicleOwner)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Forbidden, "Only an administrator or vehicle owner can close a request");
            }

            var query = _context.ServiceRequests.AsQueryable();

            if (role == AppRoles.VehicleOwner)
            {
                query = query.Where(x => x.OwnerId == userId);
            }

            var serviceRequest = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (serviceRequest is null)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.NotFound, "Service request not found");
            }

            if (serviceRequest.Status != ServiceRequestStatus.Completed)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "Only completed service requests can be closed");
            }

            ApplyStatusTransition(
                serviceRequest,
                ServiceRequestStatus.Closed,
                userId,
                "Service request closed",
                serviceRequest.AssignedProviderId);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "this service request was modified by another user. refresh and try again");
            }

            await _dashboardCacheInvalidator.InvalidateAsync(serviceRequest.OwnerId, serviceRequest.AssignedProviderId, cancellationToken);

            return ServiceResult<ServiceRequestResponse>.Success(MapToResponse(serviceRequest));
        }

        public async Task<ServiceResult<ServiceRequestResponse>> Cancel(
            Guid id,
            string userId,
            string role,
            CancelServiceRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "A valid service request ID is required");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Forbidden, "Invalid user");
            }

            if (role != AppRoles.Admin && role != AppRoles.VehicleOwner)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Forbidden, "only administrator or vehicle owner can cancel a request");
            }

            var reason = request.Reason?.Trim();

            if (reason?.Length > 500)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Validation, "Cancellation reason cannot exceed 500 characters");
            }

            var query = _context.ServiceRequests.AsQueryable();

            if (role == AppRoles.VehicleOwner)
            {
                query = query.Where(x => x.OwnerId == userId);
            }

            var serviceRequest = await query
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (serviceRequest is null)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.NotFound, "Service request not found");
            }

            if (serviceRequest.Status != ServiceRequestStatus.Pending)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "Only pending service requests can be cancelled");
            }

            ApplyStatusTransition(
                serviceRequest,
                ServiceRequestStatus.Cancelled,
                userId,
                string.IsNullOrWhiteSpace(reason)
                    ? "Service request cancelled"
                    : reason,
                serviceRequest.AssignedProviderId);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<ServiceRequestResponse>.Failure(ServiceError.Conflict, "This service request was modified by another user. Refresh and try again");
            }

            await _dashboardCacheInvalidator.InvalidateAsync(serviceRequest.OwnerId, serviceRequest.AssignedProviderId, cancellationToken);

            return ServiceResult<ServiceRequestResponse>.Success(MapToResponse(serviceRequest));
        }

        public async Task<ServiceResult<ServiceRequestTimelineResponse>> GetTimeline(
            Guid id,
            string userId,
            string role,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                return ServiceResult<ServiceRequestTimelineResponse>.Failure(ServiceError.Validation, "A valid service request ID is required");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult<ServiceRequestTimelineResponse>.Failure(ServiceError.Forbidden, "Invalid user");
            }

            if (role != AppRoles.Admin &&
                role != AppRoles.VehicleOwner &&
                role != AppRoles.ServiceProvider)
            {
                return ServiceResult<ServiceRequestTimelineResponse>.Failure(ServiceError.Forbidden, "Access denied");
            }

            var query = ApplyVisibility(
                _context.ServiceRequests.AsNoTracking(),
                userId,
                role);

            var serviceRequest = await query
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.RequestNumber,
                    x.Status
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (serviceRequest is null)
            {
                return ServiceResult<ServiceRequestTimelineResponse>.Failure(ServiceError.NotFound, "Service request not found");
            }

            // 1. Status transitions
            var statusEvents = await _context
                .Set<ServiceRequestStatusHistory>()
                .AsNoTracking()
                .Where(x => x.ServiceRequestId == id)
                .Select(x => new ServiceRequestTimelineEventResponse
                {
                    Id = x.Id,
                    EventType = "StatusChanged",
                    OccurredAtUtc = x.ChangedAtUtc,
                    PerformedByUserId = x.ChangedByUserId,
                    Notes = x.Notes,
                    FromStatus = x.FromStatus,
                    ToStatus = x.ToStatus,
                    RelatedProviderId = x.RelatedProviderId
                })
                .ToListAsync(cancellationToken);

            // 2. Technician progress updates
            var progressEvents = await _context
                .Set<ServiceRequestProgress>()
                .AsNoTracking()
                .Where(x => x.ServiceRequestId == id)
                .Select(x => new ServiceRequestTimelineEventResponse
                {
                    Id = x.Id,
                    EventType = "ProgressUpdated",
                    OccurredAtUtc = x.CreatedAtUtc,
                    PerformedByUserId = x.ProviderId,
                    Notes = x.TechnicianNotes,
                    EstimatedCompletionAtUtc = x.EstimatedCompletionAtUtc
                })
                .ToListAsync(cancellationToken);

            // 3. Completion record
            var completionEvent = await _context
                .Set<ServiceCompletion>()
                .AsNoTracking()
                .Where(x => x.ServiceRequestId == id)
                .Select(x => new ServiceRequestTimelineEventResponse
                {
                    Id = x.Id,
                    EventType = "Completed",
                    OccurredAtUtc = x.CompletedAtUtc,
                    PerformedByUserId = x.CompletedByUserId,
                    Notes = x.CompletionNotes,
                    FinalAmount = x.FinalAmount
                })
                .FirstOrDefaultAsync(cancellationToken);

            var events = statusEvents
                .Concat(progressEvents)
                .ToList();

            if (completionEvent is not null)
            {
                events.Add(completionEvent);
            }

            events = events
                .OrderBy(x => x.OccurredAtUtc)
                .ThenBy(x => x.Id)
                .ToList();

            var response = new ServiceRequestTimelineResponse
            {
                ServiceRequestId = serviceRequest.Id,
                RequestNumber = serviceRequest.RequestNumber,
                CurrentStatus = serviceRequest.Status,
                Events = events
            };

            return ServiceResult<ServiceRequestTimelineResponse>.Success(response);
        }

        private static IQueryable<ServiceRequest> ApplyVisibility(
            IQueryable<ServiceRequest> query,
            string userId,
            string role)
        {
            return role switch
            {
                AppRoles.Admin => query,

                AppRoles.VehicleOwner =>
                    query.Where(x => x.OwnerId == userId),

                AppRoles.ServiceProvider =>
                    query.Where(x => x.AssignedProviderId == userId),

                _ => query.Where(x => false)
            };
        }

        private void ApplyStatusTransition(
            ServiceRequest serviceRequest,
            ServiceRequestStatus newStatus,
            string changedByUserId,
            string? notes,
            string? relatedProviderId)
        {
            var now = DateTime.UtcNow;
            var previousStatus = serviceRequest.Status;

            serviceRequest.Status = newStatus;
            serviceRequest.UpdatedAtUtc = now;
            serviceRequest.Version++;

            serviceRequest.StatusHistories.Add(
                new ServiceRequestStatusHistory
                {
                    ServiceRequestId = serviceRequest.Id,
                    FromStatus = previousStatus,
                    ToStatus = newStatus,
                    ChangedByUserId = changedByUserId,
                    RelatedProviderId = relatedProviderId,
                    Notes = notes,
                    ChangedAtUtc = now
                });
        }

        private static ServiceRequestResponse MapToResponse(ServiceRequest request)
        {
            return new ServiceRequestResponse
            {
                Id = request.Id,
                RequestNumber = request.RequestNumber,
                VehicleId = request.VehicleId,
                OwnerId = request.OwnerId,
                AssignedProviderId = request.AssignedProviderId,
                ServiceType = request.ServiceType,
                IssueDescription = request.IssueDescription,
                PreferredServiceAtUtc = request.PreferredServiceAtUtc,
                TimeSlot = request.TimeSlot,
                PickupRequired = request.PickupRequired,
                PickupAddress = request.PickupAddress,
                ContactPhoneNumber = request.ContactPhoneNumber,
                Status = request.Status,
                CreatedAtUtc = request.CreatedAtUtc,
                UpdatedAtUtc = request.UpdatedAtUtc
            };
        }
    }
}