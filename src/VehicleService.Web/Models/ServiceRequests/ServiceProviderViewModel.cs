namespace VehicleService.Web.Models.ServiceRequests;

public sealed class ServiceProviderViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
}