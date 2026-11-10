using VehicleService.Core.Enums;

namespace VehicleService.Api.Contracts.ServiceRequests
{
    public class ServiceRequestTimelineResponse
    {
        public Guid ServiceRequestId { get; init; }
        public string RequestNumber { get; init; } = string.Empty;
        public ServiceRequestStatus CurrentStatus { get; init; }

        public List<ServiceRequestTimelineEventResponse> Events { get; init; } = [];
    }

    public class ServiceRequestTimelineEventResponse
    {
        public Guid Id { get; init; }
        public string EventType { get; init; } = string.Empty;
        public DateTime OccurredAtUtc { get; init; }
        public string PerformedByUserId { get; init; } = string.Empty;
        public string? Notes { get; init; }

        public ServiceRequestStatus? FromStatus { get; init; }
        public ServiceRequestStatus? ToStatus { get; init; }

        public string? RelatedProviderId { get; init; }
        public DateTime? EstimatedCompletionAtUtc { get; init; }
        public decimal? FinalAmount { get; init; }
    }
}