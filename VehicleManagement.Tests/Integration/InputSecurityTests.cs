using Microsoft.EntityFrameworkCore;
using VehicleManagement.Core.Entities;
using VehicleManagement.Core.Services;
using VehicleManagement.Web.Data;
using VehicleManagement.Web.Pages.Vehicles;

namespace VehicleManagement.Tests.Integration;

public class InputSecurityTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task OwnerName_WithApostrophe_IsStoredAsData()
    {
        await using var context = CreateContext();

        context.Manufacturers.Add(new Manufacturer { Id = 1, Name = "Toyota" });
        context.Vehicles.Add(new Vehicle
        {
            OwnerName = "O'Connor",
            ManufacturerId = 1,
            YearOfManufacture = 2026,
            WeightKg = 1000m
        });

        await context.SaveChangesAsync();

        Assert.Equal("O'Connor", (await context.Vehicles.SingleAsync()).OwnerName);
    }

    [Fact]
    public async Task SqlLikeOwnerInput_IsStoredAsText_NotInterpretedByApplicationLogic()
    {
        await using var context = CreateContext();

        const string input = "'; DROP TABLE Vehicles; --";

        context.Manufacturers.Add(new Manufacturer { Id = 1, Name = "Toyota" });
        context.Vehicles.Add(new Vehicle
        {
            OwnerName = input,
            ManufacturerId = 1,
            YearOfManufacture = 2026,
            WeightKg = 1000m
        });

        await context.SaveChangesAsync();

        Assert.Equal(input, (await context.Vehicles.SingleAsync()).OwnerName);
        Assert.True(await context.Manufacturers.AnyAsync());
    }

    [Fact]
    public async Task InvalidSortText_FallsBackToSafeOwnerSort()
    {
        await using var context = CreateContext();

        context.Manufacturers.Add(new Manufacturer { Id = 1, Name = "Toyota" });
        context.VehicleCategories.Add(
            new VehicleCategory { Id = 1, Name = "All", MinWeight = 0m, MaxWeight = null, Icon = "car" });
        context.Vehicles.AddRange(
            new Vehicle { OwnerName = "Zed", ManufacturerId = 1, YearOfManufacture = 2026, WeightKg = 100m },
            new Vehicle { OwnerName = "Amy", ManufacturerId = 1, YearOfManufacture = 2026, WeightKg = 200m });
        await context.SaveChangesAsync();

        var model = new IndexModel(context, new CategoryService());

        await model.OnGetAsync("owner; DROP TABLE Vehicles; --", "desc; DELETE");

        Assert.Equal(new[] { "Amy", "Zed" }, model.Vehicles.Select(v => v.OwnerName));
        Assert.Equal(2, await context.Vehicles.CountAsync());
    }
}
