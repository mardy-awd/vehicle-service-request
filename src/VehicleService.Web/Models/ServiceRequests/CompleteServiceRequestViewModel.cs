using System.ComponentModel.DataAnnotations;

namespace VehicleService.Web.Models.ServiceRequests;

public sealed class CompleteServiceRequestViewModel
{
    public Guid ServiceRequestId { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 5)]
    public string CompletionNotes { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal? FinalAmount { get; set; }
}