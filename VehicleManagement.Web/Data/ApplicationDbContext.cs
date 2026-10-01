using Microsoft.EntityFrameworkCore;
using VehicleManagement.Core.Entities;

namespace VehicleManagement.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();

    public DbSet<VehicleCategory> VehicleCategories => Set<VehicleCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}