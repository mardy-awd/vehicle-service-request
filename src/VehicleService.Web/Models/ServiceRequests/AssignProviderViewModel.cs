using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VehicleService.Web.Models.ServiceRequests;

public sealed class AssignProviderViewModel
{
    public Guid ServiceRequestId { get; set; }

    [Required(ErrorMessage = "Please select a service provider.")]
    public string ProviderId { get; set; } = string.Empty;

    public List<SelectListItem> Providers { get; set; } = [];
}