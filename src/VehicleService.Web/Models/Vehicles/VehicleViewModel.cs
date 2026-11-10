namespace VehicleService.Web.Models.Vehicles;

public sealed class VehicleViewModel
{
    public Guid Id { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Mileage { get; set; }
}