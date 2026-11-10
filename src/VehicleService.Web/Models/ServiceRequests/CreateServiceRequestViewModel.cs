using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VehicleService.Web.Models.ServiceRequests;

public sealed class CreateServiceRequestViewModel
{
    [Required(ErrorMessage = "Please select a vehicle.")]
    public Guid? VehicleId { get; set; }

    [Required]
    public string ServiceType { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    [Display(Name = "Issue Description")]
    public string IssueDescription { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Preferred Service Date")]
    public DateTime PreferredServiceAtUtc { get; set; }
        = DateTime.UtcNow.AddDays(1);

    [Required]
    [Display(Name = "Preferred Time Slot")]
    public string TimeSlot { get; set; } = string.Empty;

    [Display(Name = "Pickup Required")]
    public bool PickupRequired { get; set; }

    [Display(Name = "Pickup Address")]
    public string? PickupAddress { get; set; }

    [Required]
    [Phone]
    [Display(Name = "Contact Phone Number")]
    public string ContactPhoneNumber { get; set; } = string.Empty;

    public List<SelectListItem> Vehicles { get; set; } = [];
}