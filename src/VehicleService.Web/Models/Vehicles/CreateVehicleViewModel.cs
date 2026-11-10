using System.ComponentModel.DataAnnotations;

namespace VehicleService.Web.Models.Vehicles;

public sealed class CreateVehicleViewModel
{
    [Required]
    [StringLength(100)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    [Display(Name = "Registration Number")]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Range(1900, 2100)]
    public int Year { get; set; }

    [Range(0, int.MaxValue)]
    public int Mileage { get; set; }
}