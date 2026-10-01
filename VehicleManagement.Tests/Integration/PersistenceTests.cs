using Microsoft.EntityFrameworkCore;
using VehicleManagement.Core.Entities;
using VehicleManagement.Core.Services;
using VehicleManagement.Web.Data;
using VehicleManagement.Web.Pages.Vehicles;

namespace VehicleManagement.Tests.Integration;

public class PersistenceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Vehicle_CanBePersistedWithManufacturer()
    {
        await using var context = CreateContext();

        context.Manufacturers.Add(new Manufacturer { Id = 1, Name = "Toyota" });
        context.Vehicles.Add(new Vehicle
        {
            OwnerName = "Test Owner",
            ManufacturerId = 1,
            YearOfManufacture = 2026,
            WeightKg = 1200m
        });

        await context.SaveChangesAsync();

        var vehicle = await context.Vehicles
            .Include(v => v.Manufacturer)
            .SingleAsync();

        Assert.Equal("Toyota", vehicle.Manufacturer.Name);
    }

    [Fact]
    public async Task Category_AddUpdateDelete_Persists()
    {
        await using var context = CreateContext();

        context.VehicleCategories.AddRange(
            new VehicleCategory { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 500m, Icon = "car" },
            new VehicleCategory { Id = 2, Name = "Heavy", MinWeight = 500m, MaxWeight = null, Icon = "truck" });
        await context.SaveChangesAsync();

        var light = await context.VehicleCategories.SingleAsync(c => c.Id == 1);
        light.Name = "Light Vehicle";
        context.VehicleCategories.Add(
            new VehicleCategory { Id = 3, Name = "Medium", MinWeight = 500m, MaxWeight = 2500m, Icon = "van" });

        var heavy = await context.VehicleCategories.SingleAsync(c => c.Id == 2);
        heavy.MinWeight = 2500m;

        await context.SaveChangesAsync();

        Assert.Equal("Light Vehicle",
            (await context.VehicleCategories.SingleAsync(c => c.Id == 1)).Name);
        Assert.True(await context.VehicleCategories.AnyAsync(c => c.Id == 3));

        context.VehicleCategories.Remove(
            await context.VehicleCategories.SingleAsync(c => c.Id == 3));
        light.MaxWeight = 2500m;
        await context.SaveChangesAsync();

        Assert.False(await context.VehicleCategories.AnyAsync(c => c.Id == 3));
    }

    [Fact]
    public async Task ExistingVehicle_CategoryChangesWhenRangesChange()
    {
        await using var context = CreateContext();

        context.Manufacturers.Add(new Manufacturer { Id = 1, Name = "Toyota" });
        context.VehicleCategories.AddRange(
            new VehicleCategory { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 500m, Icon = "car" },
            new VehicleCategory { Id = 2, Name = "Medium", MinWeight = 500m, MaxWeight = 2500m, Icon = "van" },
            new VehicleCategory { Id = 3, Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" });
        context.Vehicles.Add(new Vehicle
        {
            OwnerName = "Boundary Test",
            ManufacturerId = 1,
            YearOfManufacture = 2026,
            WeightKg = 2200m
        });
        await context.SaveChangesAsync();

        var page = new IndexModel(context, new CategoryService());
        await page.OnGetAsync();
        Assert.Equal("Medium", Assert.Single(page.Vehicles).CategoryName);

        var medium = await context.VehicleCategories.SingleAsync(c => c.Id == 2);
        var heavy = await context.VehicleCategories.SingleAsync(c => c.Id == 3);
        medium.MaxWeight = 2000m;
        heavy.MinWeight = 2000m;
        await context.SaveChangesAsync();

        await page.OnGetAsync();

        Assert.Equal("Heavy", Assert.Single(page.Vehicles).CategoryName);
    }
}
