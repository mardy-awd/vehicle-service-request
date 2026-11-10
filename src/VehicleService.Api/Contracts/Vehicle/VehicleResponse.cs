namespace VehicleService.Api.Contracts.Vehicle
{
    public class VehicleResponse
    {
        public Guid Id { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Mileage { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
