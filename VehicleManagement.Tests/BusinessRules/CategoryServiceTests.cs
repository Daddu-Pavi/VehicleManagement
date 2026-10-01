using VehicleManagement.Core.Entities;
using VehicleManagement.Core.Services;

namespace VehicleManagement.Tests.BusinessRules;

public class CategoryServiceTests
{
    private readonly CategoryService _service = new();

    private static List<VehicleCategory> ValidCategories() =>
    [
        new() { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 500m, Icon = "car" },
        new() { Id = 2, Name = "Medium", MinWeight = 500m, MaxWeight = 2500m, Icon = "van" },
        new() { Id = 3, Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" }
    ];

    [Theory]
    [InlineData(0.01, "Light")]
    [InlineData(100, "Light")]
    [InlineData(499.99, "Light")]
    [InlineData(500, "Medium")]
    [InlineData(500.01, "Medium")]
    [InlineData(2499.99, "Medium")]
    [InlineData(2500, "Heavy")]
    [InlineData(2500.01, "Heavy")]
    [InlineData(100000, "Heavy")]
    public void GetCategoryForWeight_ValidWeight_ReturnsExpectedCategory(
        decimal weight, string expected)
    {
        var result = _service.GetCategoryForWeight(weight, ValidCategories());

        Assert.NotNull(result);
        Assert.Equal(expected, result.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500)]
    public void GetCategoryForWeight_NonPositiveWeight_ReturnsNull(decimal weight)
    {
        Assert.Null(_service.GetCategoryForWeight(weight, ValidCategories()));
    }

    [Fact]
    public void GetCategoryForWeight_UnorderedInput_StillReturnsCorrectCategory()
    {
        var categories = ValidCategories();
        categories.Reverse();

        var result = _service.GetCategoryForWeight(500m, categories);

        Assert.NotNull(result);
        Assert.Equal("Medium", result.Name);
    }

    [Fact]
    public void ValidateConfiguration_ValidConfiguration_ReturnsNoErrors()
    {
        Assert.Empty(_service.ValidateConfiguration(ValidCategories()));
    }

    [Fact]
    public void ValidateConfiguration_EmptyConfiguration_ReturnsError()
    {
        var errors = _service.ValidateConfiguration([]);

        Assert.Contains(errors, e => e.Contains("At least one category", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateConfiguration_Gap_ReturnsError()
    {
        var categories = ValidCategories();
        categories[1].MinWeight = 500.01m;

        var errors = _service.ValidateConfiguration(categories);

        Assert.Contains(errors, e => e.Contains("gap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateConfiguration_Overlap_ReturnsError()
    {
        var categories = ValidCategories();
        categories[1].MinWeight = 499.99m;

        var errors = _service.ValidateConfiguration(categories);

        Assert.Contains(errors, e => e.Contains("overlap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateConfiguration_FirstCategoryDoesNotStartAtZero_ReturnsError()
    {
        var categories = ValidCategories();
        categories[0].MinWeight = 1m;

        var errors = _service.ValidateConfiguration(categories);

        Assert.Contains(errors, e => e.Contains("start at 0", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateConfiguration_NegativeMinimum_ReturnsError()
    {
        var categories = ValidCategories();
        categories[0].MinWeight = -1m;

        var errors = _service.ValidateConfiguration(categories);

        Assert.Contains(errors, e => e.Contains("invalid minimum", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateConfiguration_MaxEqualsMin_ReturnsError()
    {
        var categories = ValidCategories();
        categories[1].MaxWeight = categories[1].MinWeight;

        var errors = _service.ValidateConfiguration(categories);

        Assert.Contains(errors, e => e.Contains("greater than", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateConfiguration_MaxLessThanMin_ReturnsError()
    {
        var categories = ValidCategories();
        categories[1].MaxWeight = 400m;

        var errors = _service.ValidateConfiguration(categories);

        Assert.Contains(errors, e => e.Contains("greater than", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateConfiguration_NonFinalCategoryIsOpenEnded_ReturnsError()
    {
        var categories = ValidCategories();
        categories[1].MaxWeight = null;

        var errors = _service.ValidateConfiguration(categories);

        Assert.Contains(errors, e => e.Contains("cannot be unbounded", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateConfiguration_FinalCategoryHasMaximum_ReturnsError()
    {
        var categories = ValidCategories();
        categories[2].MaxWeight = 5000m;

        var errors = _service.ValidateConfiguration(categories);

        Assert.Contains(errors, e => e.Contains("no maximum", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateConfiguration_MissingName_ReturnsError()
    {
        var categories = ValidCategories();
        categories[1].Name = " ";

        var errors = _service.ValidateConfiguration(categories);

        Assert.Contains(errors, e => e.Contains("name is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateConfiguration_MissingIcon_ReturnsError()
    {
        var categories = ValidCategories();
        categories[1].Icon = "";

        var errors = _service.ValidateConfiguration(categories);

        Assert.Contains(errors, e => e.Contains("icon", StringComparison.OrdinalIgnoreCase));
    }
}
