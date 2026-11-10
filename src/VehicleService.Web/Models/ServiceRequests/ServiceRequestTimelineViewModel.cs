namespace VehicleService.Web.Models.ServiceRequests;

public sealed class ServiceRequestTimelineViewModel
{
    public Guid ServiceRequestId { get; set; }

    public string RequestNumber { get; set; } = string.Empty;

    public string CurrentStatus { get; set; } = string.Empty;

    public List<ServiceRequestTimelineEventViewModel> Events { get; set; } = [];
}

public sealed class ServiceRequestTimelineEventViewModel
{
    public Guid Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    public string PerformedByUserId { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public string? FromStatus { get; set; }

    public string? ToStatus { get; set; }

    public string? RelatedProviderId { get; set; }

    public DateTime? EstimatedCompletionAtUtc { get; set; }

    public decimal? FinalAmount { get; set; }
}