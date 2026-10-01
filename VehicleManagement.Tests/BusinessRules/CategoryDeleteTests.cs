using VehicleManagement.Core.Entities;
using VehicleManagement.Core.Services;

namespace VehicleManagement.Tests.BusinessRules;

public class CategoryDeleteTests
{
    private readonly CategoryService _service = new();

    [Fact]
    public void DeleteMiddleCategory_WhenNeighboursReconnect_ResultIsValid()
    {
        var categories = new List<VehicleCategory>
        {
            new() { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 2500m, Icon = "car" },
            new() { Id = 3, Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" }
        };

        Assert.Empty(_service.ValidateConfiguration(categories));
    }

    [Fact]
    public void DeleteMiddleCategory_WithoutReconnectingRange_ResultIsInvalid()
    {
        var categories = new List<VehicleCategory>
        {
            new() { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 500m, Icon = "car" },
            new() { Id = 3, Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" }
        };

        var errors = _service.ValidateConfiguration(categories);

        Assert.Contains(errors, e => e.Contains("gap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DeleteFinalCategory_PreviousCategoryBecomesOpenEnded_ResultIsValid()
    {
        var categories = new List<VehicleCategory>
        {
            new() { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 500m, Icon = "car" },
            new() { Id = 2, Name = "Medium", MinWeight = 500m, MaxWeight = null, Icon = "van" }
        };

        Assert.Empty(_service.ValidateConfiguration(categories));
    }

    [Fact]
    public void DeleteAllCategories_IsRejected()
    {
        var errors = _service.ValidateConfiguration([]);

        Assert.Contains(errors, e => e.Contains("At least one category", StringComparison.OrdinalIgnoreCase));
    }
}
