using Microsoft.EntityFrameworkCore;
using VehicleManagement.Core.Entities;
using VehicleManagement.Core.Services;
using VehicleManagement.Web.Data;
using VehicleManagement.Web.Pages.Vehicles;

namespace VehicleManagement.Tests.Vehicles;

public class VehicleSortingTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedAsync(ApplicationDbContext context)
    {
        var mazda = new Manufacturer { Id = 1, Name = "Mazda" };
        var toyota = new Manufacturer { Id = 2, Name = "Toyota" };

        context.Manufacturers.AddRange(mazda, toyota);
        context.VehicleCategories.AddRange(
            new VehicleCategory { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 500m, Icon = "car" },
            new VehicleCategory { Id = 2, Name = "Medium", MinWeight = 500m, MaxWeight = 2500m, Icon = "van" },
            new VehicleCategory { Id = 3, Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" });

        context.Vehicles.AddRange(
            new Vehicle { OwnerName = "Charlie", ManufacturerId = 1, YearOfManufacture = 2020, WeightKg = 3000m },
            new Vehicle { OwnerName = "Alice", ManufacturerId = 2, YearOfManufacture = 2024, WeightKg = 400m },
            new Vehicle { OwnerName = "Bob", ManufacturerId = 1, YearOfManufacture = 2022, WeightKg = 1500m });

        await context.SaveChangesAsync();
    }

    [Theory]
    [InlineData("owner", "asc", "Alice", "Bob", "Charlie")]
    [InlineData("owner", "desc", "Charlie", "Bob", "Alice")]
    public async Task OnGet_SortsByOwner(
        string sortBy, string direction, string first, string second, string third)
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = new IndexModel(context, new CategoryService());

        await model.OnGetAsync(sortBy, direction);

        Assert.Equal(new[] { first, second, third }, model.Vehicles.Select(v => v.OwnerName));
    }

    [Theory]
    [InlineData("year", "asc", 2020, 2022, 2024)]
    [InlineData("year", "desc", 2024, 2022, 2020)]
    public async Task OnGet_SortsByYear(
        string sortBy, string direction, int first, int second, int third)
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = new IndexModel(context, new CategoryService());

        await model.OnGetAsync(sortBy, direction);

        Assert.Equal(new[] { first, second, third }, model.Vehicles.Select(v => v.YearOfManufacture));
    }

    [Theory]
    [InlineData("weight", "asc", 400, 1500, 3000)]
    [InlineData("weight", "desc", 3000, 1500, 400)]
    public async Task OnGet_SortsByWeight(
        string sortBy, string direction, decimal first, decimal second, decimal third)
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = new IndexModel(context, new CategoryService());

        await model.OnGetAsync(sortBy, direction);

        Assert.Equal(new[] { first, second, third }, model.Vehicles.Select(v => v.WeightKg));
    }

    [Theory]
    [InlineData("manufacturer", "asc", "Mazda", "Mazda", "Toyota")]
    [InlineData("manufacturer", "desc", "Toyota", "Mazda", "Mazda")]
    public async Task OnGet_SortsByManufacturer(
        string sortBy, string direction, string first, string second, string third)
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = new IndexModel(context, new CategoryService());

        await model.OnGetAsync(sortBy, direction);

        Assert.Equal(new[] { first, second, third }, model.Vehicles.Select(v => v.Manufacturer));
    }

    [Fact]
    public async Task OnGet_InvalidSortField_FallsBackToOwnerAscending()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = new IndexModel(context, new CategoryService());

        await model.OnGetAsync("DROP TABLE Vehicles", "anything");

        Assert.Equal("asc", model.SortDirection);
        Assert.Equal(new[] { "Alice", "Bob", "Charlie" }, model.Vehicles.Select(v => v.OwnerName));
    }

    [Fact]
    public async Task OnGet_UsesCurrentCategoryConfiguration()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = new IndexModel(context, new CategoryService());
        await model.OnGetAsync();

        Assert.Equal("Medium", model.Vehicles.Single(v => v.OwnerName == "Bob").CategoryName);

        var medium = await context.VehicleCategories.SingleAsync(c => c.Name == "Medium");
        var heavy = await context.VehicleCategories.SingleAsync(c => c.Name == "Heavy");
        medium.MaxWeight = 1000m;
        heavy.MinWeight = 1000m;
        await context.SaveChangesAsync();

        await model.OnGetAsync();

        Assert.Equal("Heavy", model.Vehicles.Single(v => v.OwnerName == "Bob").CategoryName);
    }
}
