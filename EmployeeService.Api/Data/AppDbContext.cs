using EmployeeService.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeService.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeePhone> EmployeePhones => Set<EmployeePhone>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Id)
                .UseIdentityColumn();

            e.Property(x => x.DocumentNumber)
                .IsRequired();

            e.HasIndex(x => x.DocumentNumber)
                .IsUnique();

            e.HasIndex(x => x.Email)
                .IsUnique();

            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            e.Property(x => x.Email).IsRequired().HasMaxLength(200);

            e.Property(x => x.Password)
                .IsRequired()
                .HasMaxLength(500);

            // Manager: FK a Employee.Id (NO document number)
            e.HasOne(x => x.Manager)
                .WithMany()
                .HasForeignKey(x => x.ManagerId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<EmployeePhone>(p =>
        {
            p.HasKey(x => x.Id);

            p.Property(x => x.PhoneNumber)
                .HasColumnType("bigint");

            // FK a Employee.Id
            p.HasOne(x => x.Employee)
                .WithMany(x => x.Phones)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
