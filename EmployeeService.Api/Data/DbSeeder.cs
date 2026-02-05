using EmployeeService.Api.Auth;
using EmployeeService.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeService.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, ILogger logger)
    {
        await db.Database.MigrateAsync();

        var adminDoc = 1000;

        // ✅ Only skip if the admin user already exists
        var adminExists = await db.Employees.AnyAsync(e => e.DocumentNumber == adminDoc);
        if (adminExists)
        {
            logger.LogInformation(
                "Seed skipped: admin user already exists (doc={Doc}).",
                adminDoc
            );
            return;
        }

        logger.LogInformation("Seeding initial admin user (Director)...");

        var admin = new Employee
        {
            DocumentNumber = adminDoc,
            FirstName = "Admin",
            LastName = "Admin",
            Email = "admin@demo.com",
            BirthDate = new DateTime(1995, 2, 10),
            Gender = Gender.Female,
            Role = EmployeeRole.Director,
            Password = hasher.Hash("Admin@123456"),
            Phones = new List<EmployeePhone>
            {
                new EmployeePhone { PhoneNumber = 11999999999 },
                new EmployeePhone { PhoneNumber = 11888888888 }
            }
        };

        db.Employees.Add(admin);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Seed completed: admin created (doc={Doc}, role=Director).",
            adminDoc
        );
    }
}
