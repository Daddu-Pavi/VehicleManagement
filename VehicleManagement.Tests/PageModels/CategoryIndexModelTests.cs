using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VehicleManagement.Core.Entities;
using VehicleManagement.Core.Services;
using VehicleManagement.Web.Data;
using CategoryIndexModel = VehicleManagement.Web.Pages.Categories.IndexModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace VehicleManagement.Tests.PageModels;

public class CategoryIndexModelTests
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
        context.VehicleCategories.AddRange(
            new VehicleCategory { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 500m, Icon = "car" },
            new VehicleCategory { Id = 2, Name = "Medium", MinWeight = 500m, MaxWeight = 2500m, Icon = "van" },
            new VehicleCategory { Id = 3, Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" });

        await context.SaveChangesAsync();
    }

    private static CategoryIndexModel CreateModel(ApplicationDbContext context)
    {
    var model = new CategoryIndexModel(
        context,
        new CategoryService(),
        NullLogger<CategoryIndexModel>.Instance);

    model.TempData = new TempDataDictionary(
        new DefaultHttpContext(),
        new TestTempDataProvider());

    return model;
    }

    private static List<CategoryIndexModel.CategoryInput> ValidInputs() =>
    [
        new() { Id = 1, ClientKey = "existing-1", Name = "Light", MinWeight = 0m, MaxWeight = 500m, Icon = "car" },
        new() { Id = 2, ClientKey = "existing-2", Name = "Medium", MinWeight = 500m, MaxWeight = 2500m, Icon = "van" },
        new() { Id = 3, ClientKey = "existing-3", Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" }
    ];

    [Fact]
    public async Task OnGet_LoadsCategoriesAndProposedConfigurationInWeightOrder()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);

        await model.OnGetAsync();

        Assert.Equal(3, model.Categories.Count);
        Assert.Equal(3, model.ProposedCategories.Count);
        Assert.Equal(new[] { "Light", "Medium", "Heavy" }, model.Categories.Select(c => c.Name));
    }

    [Fact]
    public async Task Save_Gap_IsRejectedAndDatabaseIsUnchanged()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);
        model.ProposedCategories = ValidInputs();
        model.ProposedCategories[1].MinWeight = 600m;

        var result = await model.OnPostSaveChangesAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);

        var medium = await context.VehicleCategories.AsNoTracking().SingleAsync(c => c.Id == 2);
        Assert.Equal(500m, medium.MinWeight);
    }

    [Fact]
    public async Task Save_Overlap_IsRejected()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);
        model.ProposedCategories = ValidInputs();
        model.ProposedCategories[1].MinWeight = 400m;

        var result = await model.OnPostSaveChangesAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task Save_FinalCategoryWithMaximum_IsRejected()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);
        model.ProposedCategories = ValidInputs();
        model.ProposedCategories[2].MaxWeight = 5000m;

        var result = await model.OnPostSaveChangesAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task Save_InvalidIcon_IsRejected()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);
        model.ProposedCategories = ValidInputs();
        model.ProposedCategories[1].Icon = "<script>";

        var result = await model.OnPostSaveChangesAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task Save_DuplicateNameIgnoringCase_IsRejected()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);
        model.ProposedCategories = ValidInputs();
        model.ProposedCategories[1].Name = "LIGHT";

        var result = await model.OnPostSaveChangesAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task Save_UnknownExistingId_IsRejected()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);
        model.ProposedCategories = ValidInputs();
        model.ProposedCategories[1].Id = 999;

        var result = await model.OnPostSaveChangesAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task Save_DuplicateExistingId_IsRejected()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);
        model.ProposedCategories = ValidInputs();
        model.ProposedCategories[1].Id = 1;

        var result = await model.OnPostSaveChangesAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task Save_UnconfirmedDeletion_IsRejected()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);
        model.ProposedCategories =
        [
            new() { Id = 1, ClientKey = "existing-1", Name = "Light", MinWeight = 0m, MaxWeight = 2500m, Icon = "car" },
            new() { Id = 3, ClientKey = "existing-3", Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" }
        ];

        var result = await model.OnPostSaveChangesAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(await context.VehicleCategories.AnyAsync(c => c.Id == 2));
    }

    [Fact]
    public async Task Save_ConfirmedDeletion_RemovesCategory()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);
        model.ProposedCategories =
        [
            new() { Id = 1, ClientKey = "existing-1", Name = "Light", MinWeight = 0m, MaxWeight = 2500m, Icon = "car" },
            new() { Id = 3, ClientKey = "existing-3", Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" }
        ];
        model.ConfirmedDeletedCategoryIds = [2];

        // RedirectToPage itself does not require a rendered Razor page.
        var result = await model.OnPostSaveChangesAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.False(await context.VehicleCategories.AnyAsync(c => c.Id == 2));
    }

    [Fact]
    public async Task Save_MoreThanTwoDecimalPlaces_IsRejected()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var model = CreateModel(context);
        model.ProposedCategories = ValidInputs();
        model.ProposedCategories[1].MinWeight = 500.123m;

        var result = await model.OnPostSaveChangesAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }
    [Fact]
public async Task Save_CategoryNameExceedsMaximumLength_IsRejected()
{
    await using var context = CreateContext();
    await SeedAsync(context);

    var model = CreateModel(context);
    model.ProposedCategories = ValidInputs();
    model.ProposedCategories[0].Name = "ThisNameIsOverTwentyChars";

    var result = await model.OnPostSaveChangesAsync();

    Assert.IsType<PageResult>(result);
    Assert.False(model.ModelState.IsValid);

    var light = await context.VehicleCategories
        .AsNoTracking()
        .SingleAsync(c => c.Id == 1);

    Assert.Equal("Light", light.Name);
}
[Fact]
public async Task Save_NegativeCategoryId_IsRejected()
{
    await using var context = CreateContext();
    await SeedAsync(context);

    var model = CreateModel(context);
    model.ProposedCategories = ValidInputs();
    model.ProposedCategories[1].Id = -1;

    var result = await model.OnPostSaveChangesAsync();

    Assert.IsType<PageResult>(result);
    Assert.False(model.ModelState.IsValid);
}
[Fact]
public async Task Save_ValidConfiguration_UpdatesAddsAndDeletes()
{
    await using var context = CreateContext();
    await SeedAsync(context);

    var model = CreateModel(context);

    model.ProposedCategories =
    [
        new()
        {
            Id = 1,
            ClientKey = "existing-1",
            Name = "Light",
            MinWeight = 0m,
            MaxWeight = 400m,
            Icon = "car"
        },
        new()
        {
            Id = 0,
            ClientKey = "new-light-plus",
            Name = "Light Plus",
            MinWeight = 400m,
            MaxWeight = 700m,
            Icon = "suv"
        },
        new()
        {
            Id = 2,
            ClientKey = "existing-2",
            Name = "Medium",
            MinWeight = 700m,
            MaxWeight = null,
            Icon = "van"
        }
    ];

    model.ConfirmedDeletedCategoryIds = [3];

    var result = await model.OnPostSaveChangesAsync();

    Assert.IsType<RedirectToPageResult>(result);

    var categories = await context.VehicleCategories
        .AsNoTracking()
        .OrderBy(c => c.MinWeight)
        .ToListAsync();

    Assert.Equal(3, categories.Count);

    var light = categories.Single(c => c.Name == "Light");
    Assert.Equal(0m, light.MinWeight);
    Assert.Equal(400m, light.MaxWeight);

    var lightPlus = categories.Single(c => c.Name == "Light Plus");
    Assert.Equal(400m, lightPlus.MinWeight);
    Assert.Equal(700m, lightPlus.MaxWeight);
    Assert.Equal("suv", lightPlus.Icon);

    var medium = categories.Single(c => c.Name == "Medium");
    Assert.Equal(700m, medium.MinWeight);
    Assert.Null(medium.MaxWeight);

    Assert.DoesNotContain(
        categories,
        c => c.Name == "Heavy");
}
    private sealed class TestTempDataProvider : ITempDataProvider
{
    private readonly Dictionary<string, object> _data = new();

    public IDictionary<string, object> LoadTempData(HttpContext context)
    {
        return new Dictionary<string, object>(_data);
    }

    public void SaveTempData(
        HttpContext context,
        IDictionary<string, object> values)
    {
        _data.Clear();

        foreach (var item in values)
        {
            _data[item.Key] = item.Value;
        }
    }
}
}
