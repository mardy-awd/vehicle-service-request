using Microsoft.AspNetCore.Identity;
using VehicleService.Api.Identity;

namespace VehicleService.Api.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            await using var scope = services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Seed roles
            foreach (var roleName in new[] { AppRoles.Admin, AppRoles.ServiceProvider, AppRoles.VehicleOwner })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole(roleName);
                    var result = await roleManager.CreateAsync(role);

                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Failed to create role '{roleName}': " +
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            // Seed admin user
            var adminSettings = configuration.GetSection("SeedAdmin");

            var adminEmail = adminSettings["Email"];
            var adminPassword = adminSettings["Password"];
            var adminFullName = adminSettings["FullName"];

            if (string.IsNullOrWhiteSpace(adminEmail) ||
                string.IsNullOrWhiteSpace(adminPassword) ||
                string.IsNullOrWhiteSpace(adminFullName))
            {
                throw new InvalidOperationException(
                    "SeedAdmin configuration is missing or incomplete.");
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = adminFullName,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to create admin user: " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                var roleResult = await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to assign Admin role: " +
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                var roles = await userManager.GetRolesAsync(adminUser);

                if (roles.Count == 0)
                {
                    throw new InvalidOperationException(
                        "The configured admin account exists without a role. " +
                        "Manual verification is required before assigning Admin privileges.");
                }

                if (roles.Count != 1 || roles[0] != AppRoles.Admin)
                {
                    throw new InvalidOperationException(
                        "The configured admin account has an invalid role assignment.");
                }
            }
        }
    }
}
