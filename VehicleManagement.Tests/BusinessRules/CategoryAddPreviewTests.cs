using VehicleManagement.Core.Entities;
using VehicleManagement.Core.Services;

namespace VehicleManagement.Tests.BusinessRules;

public class CategoryAddPreviewTests
{
    private readonly CategoryService _service = new();

    private static List<VehicleCategory> Categories() =>
    [
        new() { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 500m, Icon = "car" },
        new() { Id = 2, Name = "Medium", MinWeight = 500m, MaxWeight = 2500m, Icon = "van" },
        new() { Id = 3, Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" }
    ];

    [Fact]
    public void BuildAddPreview_AddBetweenCategories_ReturnsContinuousConfiguration()
    {
        var result = _service.BuildAddPreview(
            "Light Plus", "suv", 400m, 700m, Categories());

        Assert.True(result.CanProceed);
        Assert.Empty(_service.ValidateConfiguration(result.ProposedCategories));

        var added = Assert.Single(
    result.ProposedCategories,
    c => c.Name == "Light Plus");
        Assert.Equal(400m, added.MinWeight);
        Assert.Equal(700m, added.MaxWeight);

        var light = result.ProposedCategories.Single(c => c.Name == "Light");
        var medium = result.ProposedCategories.Single(c => c.Name == "Medium");

        Assert.Equal(400m, light.MaxWeight);
        Assert.Equal(700m, medium.MinWeight);
    }

    [Fact]
    public void BuildAddPreview_AddNewFinalCategory_ClosesOldFinalAndMakesNewFinalOpenEnded()
    {
        var result = _service.BuildAddPreview(
            "Super Heavy", "heavy-truck", 5000m, null, Categories());

        Assert.True(result.CanProceed);
        Assert.Empty(_service.ValidateConfiguration(result.ProposedCategories));

        var heavy = result.ProposedCategories.Single(c => c.Name == "Heavy");
        var superHeavy = result.ProposedCategories.Single(c => c.Name == "Super Heavy");

        Assert.Equal(5000m, heavy.MaxWeight);
        Assert.Equal(5000m, superHeavy.MinWeight);
        Assert.Null(superHeavy.MaxWeight);
    }

    [Fact]
    public void BuildAddPreview_AddInsideExistingRange_UsesContinuousRangeModel()
    {
        var categories = new List<VehicleCategory>
        {
            new() { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 600m, Icon = "car" },
            new() { Id = 2, Name = "Medium", MinWeight = 600m, MaxWeight = 1600m, Icon = "van" },
            new() { Id = 3, Name = "Heavy", MinWeight = 1600m, MaxWeight = null, Icon = "truck" }
        };

        var result = _service.BuildAddPreview(
            "Extra Light", "motorcycle", 100m, 300m, categories);

        Assert.True(result.CanProceed);
        Assert.Empty(_service.ValidateConfiguration(result.ProposedCategories));

        var light = result.ProposedCategories.Single(c => c.Name == "Light");
        var extra = result.ProposedCategories.Single(c => c.Name == "Extra Light");
        var medium = result.ProposedCategories.Single(c => c.Name == "Medium");

        Assert.Equal(100m, light.MaxWeight);
        Assert.Equal(100m, extra.MinWeight);
        Assert.Equal(300m, extra.MaxWeight);
        Assert.Equal(300m, medium.MinWeight);
    }

    [Fact]
    public void BuildAddPreview_SwallowsOneCategory_ReportsCategoryForDeletion()
    {
        var result = _service.BuildAddPreview(
            "Combined", "suv", 400m, 2600m, Categories());

        Assert.True(result.CanProceed);
        Assert.True(result.RequiresDeletion);
        Assert.Contains(result.CategoriesToDelete, c => c.Name == "Medium");
        Assert.Empty(_service.ValidateConfiguration(result.ProposedCategories));
    }

    [Fact]
    public void BuildAddPreview_SwallowsMultipleCategories_ReportsAllForDeletion()
    {
        var categories = new List<VehicleCategory>
        {
            new() { Id = 1, Name = "A", MinWeight = 0m, MaxWeight = 500m, Icon = "car" },
            new() { Id = 2, Name = "B", MinWeight = 500m, MaxWeight = 1000m, Icon = "suv" },
            new() { Id = 3, Name = "C", MinWeight = 1000m, MaxWeight = 1500m, Icon = "van" },
            new() { Id = 4, Name = "D", MinWeight = 1500m, MaxWeight = 2500m, Icon = "truck" },
            new() { Id = 5, Name = "E", MinWeight = 2500m, MaxWeight = null, Icon = "heavy-truck" }
        };

        var result = _service.BuildAddPreview(
            "Combined", "bus", 400m, 2000m, categories);

        Assert.True(result.CanProceed);
        Assert.True(result.RequiresDeletion);
        Assert.Equal(2, result.CategoriesToDelete.Count);
        Assert.Contains(result.CategoriesToDelete, c => c.Name == "B");
        Assert.Contains(result.CategoriesToDelete, c => c.Name == "C");
        Assert.Empty(_service.ValidateConfiguration(result.ProposedCategories));
    }

    [Theory]
    [InlineData("", "car", 100, 200)]
    [InlineData("New", "", 100, 200)]
    [InlineData("New", "car", -1, 200)]
    [InlineData("New", "car", 200, 200)]
    [InlineData("New", "car", 300, 200)]
    public void BuildAddPreview_InvalidInput_CannotProceed(
        string name, string icon, decimal min, decimal max)
    {
        var result = _service.BuildAddPreview(name, icon, min, max, Categories());

        Assert.False(result.CanProceed);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public void BuildAddPreview_DuplicateName_CannotProceed()
    {
        var result = _service.BuildAddPreview(
            "medium", "car", 100m, 200m, Categories());

        Assert.False(result.CanProceed);
        Assert.Contains("already exists", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }
}
