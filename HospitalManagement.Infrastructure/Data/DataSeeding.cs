using Microsoft.AspNetCore.Identity;
using HospitalManagement.Domain.Models;

namespace HospitalManagement.Infrastructure.Data
{
    public class DataSeeding
    {
        public static async Task SeedRolesAndAdminAsync(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager, ApplicationDBContext context)
        { 
            string[] roles = { "Admin", "Doctor", "Patient", "Receptionist" };
             
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
             
            const string adminEmail = "hospital@gmail.com";
            const string adminPassword = "Password1!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var newAdminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newAdminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdminUser, "Admin");
                }
            }
             
            const string receptionistEmail = "receptionist@hospital.com";
            const string receptionistPassword = "Reception123!";

            var receptionistUser = await userManager.FindByEmailAsync(receptionistEmail);
            if (receptionistUser == null)
            {
                var newReceptionistUser = new IdentityUser
                {
                    UserName = receptionistEmail,
                    Email = receptionistEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newReceptionistUser, receptionistPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newReceptionistUser, "Receptionist");
                }
            }
             
            if (context != null)
            {
                if (!context.Medicines.Any())
                {
                    var meds = new List<Medicine>
                    {
                        new Medicine { Id = Guid.NewGuid(), Name = "Paracetamol 500mg", ManufacturerName = "PharmaCorp", Price = 10.00m, StockQuantity = 100 },
                        new Medicine { Id = Guid.NewGuid(), Name = "Amoxicillin 250mg", ManufacturerName = "HealthMed", Price = 25.50m, StockQuantity = 50 },
                        new Medicine { Id = Guid.NewGuid(), Name = "Ibuprofen 200mg", ManufacturerName = "MediCare", Price = 12.75m, StockQuantity = 80 },
                        new Medicine { Id = Guid.NewGuid(), Name = "Cough Syrup 100ml", ManufacturerName = "RemedyLab", Price = 45.00m, StockQuantity = 40 },
                        new Medicine { Id = Guid.NewGuid(), Name = "Vitamin C 500mg", ManufacturerName = "NutriPlus", Price = 20.00m, StockQuantity = 200 }
                    };

                    context.Medicines.AddRange(meds);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
