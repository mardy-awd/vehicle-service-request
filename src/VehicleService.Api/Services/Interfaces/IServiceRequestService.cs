using VehicleService.Api.Contracts.Common;
using VehicleService.Api.Contracts.ServiceRequests;
using VehicleService.Api.Services.Results;

namespace VehicleService.Api.Services.Interfaces
{
    public interface IServiceRequestService
    {
        Task<ServiceResult<ServiceRequestResponse>> Create(
        string ownerId,
        CreateServiceRequestRequest request,
        CancellationToken cancellationToken = default);

        Task<PagedResult<ServiceRequestResponse>> GetAll(
            string userId,
            string role,
            ServiceRequestQuery query,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<ServiceRequestResponse>> GetById(
            Guid id,
            string userId,
            string role,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<ServiceRequestResponse>> AssignProvider(
            Guid id,
            string adminId,
            AssignServiceProviderRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<ServiceRequestResponse>> Accept(
            Guid id,
            string providerId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<ServiceRequestResponse>> Reject(
            Guid id,
            string providerId,
            RejectServiceRequestRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<ServiceRequestResponse>> Start(
            Guid id,
            string providerId,
            StartServiceRequestRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<ServiceRequestResponse>> AddProgress(
            Guid id,
            string providerId,
            AddServiceRequestProgressRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<ServiceRequestResponse>> Complete(
            Guid id,
            string providerId,
            CompleteServiceRequestRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<ServiceRequestResponse>> Close(
            Guid id,
            string userId,
            string role,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<ServiceRequestResponse>> Cancel(
            Guid id,
            string userId,
            string role,
            CancelServiceRequestRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<ServiceRequestTimelineResponse>> GetTimeline(
            Guid id,
            string userId,
            string role,
            CancellationToken cancellationToken = default);
    }
}