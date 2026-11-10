using System.ComponentModel.DataAnnotations;

namespace VehicleService.Web.Models.ServiceRequests;

public sealed class StartServiceRequestViewModel
{
    public Guid ServiceRequestId { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 5)]
    public string TechnicianNotes { get; set; } = string.Empty;

    [Required]
    public DateTime? EstimatedCompletionAtUtc { get; set; }
}