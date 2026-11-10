namespace VehicleService.Core.Entities
{
    public class Vehicle
    {
        public Guid Id { get; set; }
        public required string OwnerId { get; set; }
        public required string Make { get; set; }
        public required string Model { get; set; }
        public int Year { get; set; }
        public required string RegistrationNumber { get; set; }
        public int Mileage { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public ICollection<ServiceRequest> ServiceRequests { get; set; } = [];
    }
}