using System.ComponentModel.DataAnnotations;

namespace VehicleService.Web.Models.ServiceRequests;

public sealed class RejectServiceRequestViewModel
{
    public Guid ServiceRequestId { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}