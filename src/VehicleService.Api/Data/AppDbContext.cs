using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VehicleService.Api.Identity;
using VehicleService.Core.Entities;

namespace VehicleService.Api.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Vehicle>()
                 .ToTable("vehicles", table =>
                 {
                     table.HasCheckConstraint(
                         "CK_Vehicle_NonNegativeMileage",
                         "\"Mileage\" >= 0");

                     table.HasCheckConstraint(
                         "CK_Vehicle_ValidYearRange",
                         "\"Year\" BETWEEN 1886 AND 2100");
                 });

            modelBuilder.Entity<Vehicle>()
               .HasOne<ApplicationUser>()
               .WithMany()
               .HasForeignKey(v => v.OwnerId)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
               .HasMany(v => v.ServiceRequests)
               .WithOne(sh => sh.Vehicle)
               .HasForeignKey(sh => sh.VehicleId)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
                .HasIndex(e => e.RegistrationNumber)
                .IsUnique();

            modelBuilder.Entity<Vehicle>()
                .Property(e => e.Make)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Vehicle>()
                .Property(e => e.Model)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Vehicle>()
                .Property(e => e.RegistrationNumber)
                .HasMaxLength(30)
                .IsRequired();

            modelBuilder.Entity<ServiceRequest>()
                .ToTable("service_requests", table =>
                {
                    table.HasCheckConstraint(
                        "CK_ServiceRequest_PickupAddressRequired",
                        """
                        NOT "PickupRequired"
                        OR (
                            "PickupAddress" IS NOT NULL
                            AND length(trim("PickupAddress")) > 0
                        )
                        """);
                });

            modelBuilder.Entity<ServiceRequest>()
                .HasIndex(e => e.RequestNumber)
                .IsUnique();

            modelBuilder.Entity<ServiceRequest>()
                .Property(e => e.RequestNumber)
                .HasMaxLength(30)
                .IsRequired();

            modelBuilder.Entity<ServiceRequest>()
                .Property(e => e.IssueDescription)
                .HasMaxLength(2000)
                .IsRequired();
            
            modelBuilder.Entity<ServiceRequest>()
               .Property(e => e.ContactPhoneNumber)
               .HasMaxLength(30)
               .IsRequired();

            modelBuilder.Entity<ServiceRequest>()
               .Property(e => e.PickupAddress)
               .HasMaxLength(500)
               .IsRequired(false);

            modelBuilder.Entity<ServiceRequest>()
                .Property(e => e.ServiceType)
                .HasConversion<string>()
                .HasMaxLength(30);

            modelBuilder.Entity<ServiceRequest>()
                .Property(e => e.TimeSlot)
                .HasConversion<string>()
                .HasMaxLength(30);

            modelBuilder.Entity<ServiceRequest>()
                .Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            modelBuilder.Entity<ServiceRequest>()
                .Property(e => e.Version)
                .IsConcurrencyToken();

            modelBuilder.Entity<ServiceRequest>()
                .Property(e => e.CreatedAtUtc)
                .IsRequired();
            modelBuilder.Entity<ServiceRequest>()
                .Property(e => e.UpdatedAtUtc)
                .IsRequired();

            //Owned Requests
            modelBuilder.Entity<ServiceRequest>()
              .HasOne<ApplicationUser>()
              .WithMany()
              .HasForeignKey(v => v.OwnerId)
              .OnDelete(DeleteBehavior.Restrict);

            //Assigned Requests
            modelBuilder.Entity<ServiceRequest>()
              .HasOne<ApplicationUser>()
              .WithMany()
              .HasForeignKey(v => v.AssignedProviderId)
              .IsRequired(false)
              .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceRequest>()
                .HasMany(sr => sr.StatusHistories)
                .WithOne(sh => sh.ServiceRequest)
                .HasForeignKey(sh => sh.ServiceRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceRequest>()
                .HasMany(sr => sr.Progresses)
                .WithOne(sp => sp.ServiceRequest)
                .HasForeignKey(sp => sp.ServiceRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceRequest>()
                .HasOne(sr => sr.Completion)
                .WithOne(sc => sc.ServiceRequest)
                .HasForeignKey<ServiceCompletion>(sc => sc.ServiceRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceRequestStatusHistory>().ToTable("service_request_status_histories");

            modelBuilder.Entity<ServiceRequestStatusHistory>()
                .HasIndex(h => new { h.ServiceRequestId, h.ChangedAtUtc });

            modelBuilder.Entity<ServiceRequestStatusHistory>()
                .Property(h => h.FromStatus)
                .HasConversion<string>()
                .HasMaxLength(30);

            modelBuilder.Entity<ServiceRequestStatusHistory>()
                .Property(h => h.ToStatus)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            modelBuilder.Entity<ServiceRequestStatusHistory>()
                .Property(h => h.Notes)
                .HasMaxLength(2000)
                .IsRequired(false);

            //StatusHistory actor
            modelBuilder.Entity<ServiceRequestStatusHistory>()
             .HasOne<ApplicationUser>()
             .WithMany()
             .HasForeignKey(h => h.ChangedByUserId)
             .OnDelete(DeleteBehavior.Restrict);

            //StatusHistory related provider
            modelBuilder.Entity<ServiceRequestStatusHistory>()
             .HasOne<ApplicationUser>()
             .WithMany()
             .HasForeignKey(h => h.RelatedProviderId)
             .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceRequestProgress>().ToTable("service_request_progresses");

            modelBuilder.Entity<ServiceRequestProgress>()
                .Property(p => p.TechnicianNotes)
                .HasMaxLength(2000)
                .IsRequired();

            modelBuilder.Entity<ServiceRequestProgress>()
                .HasIndex(p => new { p.ServiceRequestId, p.CreatedAtUtc });

            //Progress provider
            modelBuilder.Entity<ServiceRequestProgress>()
             .HasOne<ApplicationUser>()
             .WithMany()
             .HasForeignKey(p => p.ProviderId)
             .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceCompletion>()
                .ToTable("service_completions", table =>
                {
                    table.HasCheckConstraint(
                        "CK_ServiceCompletion_NonNegativeFinalAmount",
                        "\"FinalAmount\" >= 0");
                });

            modelBuilder.Entity<ServiceCompletion>()
                .Property(c => c.CompletionNotes)
                .HasMaxLength(2000)
                .IsRequired();

            modelBuilder.Entity<ServiceCompletion>()
                .Property(c => c.FinalAmount)
                .HasPrecision(18, 2);

            //Completion provider
            modelBuilder.Entity<ServiceCompletion>()
             .HasOne<ApplicationUser>()
             .WithMany()
             .HasForeignKey(c => c.CompletedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<ServiceRequestStatusHistory> ServiceRequestStatusHistories { get; set; }
        public DbSet<ServiceRequestProgress> ServiceRequestProgresses { get; set; }
        public DbSet<ServiceCompletion> ServiceCompletions { get; set; }
    }
}
