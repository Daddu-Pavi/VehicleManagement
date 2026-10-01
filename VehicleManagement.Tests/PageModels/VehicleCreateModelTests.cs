using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleManagement.Core.Entities;
using VehicleManagement.Core.Services;
using VehicleManagement.Core.Validation;
using VehicleManagement.Web.Data;
using VehicleManagement.Web.Pages.Vehicles;

namespace VehicleManagement.Tests.PageModels;

public class VehicleCreateModelTests
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
        context.Manufacturers.Add(new Manufacturer { Id = 1, Name = "Toyota" });
        context.VehicleCategories.AddRange(
            new VehicleCategory { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 500m, Icon = "car" },
            new VehicleCategory { Id = 2, Name = "Medium", MinWeight = 500m, MaxWeight = 2500m, Icon = "van" },
            new VehicleCategory { Id = 3, Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" });
        await context.SaveChangesAsync();
    }

    private static CreateModel CreateModel(ApplicationDbContext context)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddRazorPages()
            .Services
            .BuildServiceProvider();

        var model = new CreateModel(
            context,
            new CategoryService(),
            new VehicleValidator());

        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = services
            }
        };

        return model;
    }

    [Fact]
    public async Task OnGet_LoadsManufacturersAndCategories()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);

        await model.OnGetAsync();

        Assert.NotNull(model.ManufacturerOptions);
        Assert.Equal(3, model.Categories.Count);
        Assert.Equal(DateTime.UtcNow.Year, model.Vehicle.YearOfManufacture);
    }

    [Fact]
    public async Task OnPost_ValidVehicle_SavesAndRedirects()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);
        model.Vehicle = new Vehicle
        {
            OwnerName = "  Pavithra  ",
            ManufacturerId = 1,
            YearOfManufacture = DateTime.UtcNow.Year,
            WeightKg = 1500.50m
        };

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var saved = await context.Vehicles.SingleAsync();
        Assert.Equal("Pavithra", saved.OwnerName);
        Assert.Equal(1500.50m, saved.WeightKg);
    }

    [Fact]
    public async Task OnPost_InvalidManufacturer_DoesNotSave()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);
        model.Vehicle = new Vehicle
        {
            OwnerName = "Test",
            ManufacturerId = 999,
            YearOfManufacture = DateTime.UtcNow.Year,
            WeightKg = 1000m
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Empty(await context.Vehicles.ToListAsync());
    }

    [Fact]
    public async Task OnPost_WeightWithMoreThanTwoDecimals_DoesNotSave()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);
        model.Vehicle = new Vehicle
        {
            OwnerName = "Test",
            ManufacturerId = 1,
            YearOfManufacture = DateTime.UtcNow.Year,
            WeightKg = 1000.123m
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Empty(await context.Vehicles.ToListAsync());
    }

    [Fact]
    public async Task OnPost_InvalidCategoryConfiguration_DoesNotSaveVehicle()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var medium = await context.VehicleCategories.SingleAsync(c => c.Name == "Medium");
        medium.MinWeight = 600m;
        await context.SaveChangesAsync();

        var model = CreateModel(context);
        model.Vehicle = new Vehicle
        {
            OwnerName = "Test",
            ManufacturerId = 1,
            YearOfManufacture = DateTime.UtcNow.Year,
            WeightKg = 1000m
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Empty(await context.Vehicles.ToListAsync());
    }
}
