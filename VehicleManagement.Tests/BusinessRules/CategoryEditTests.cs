using VehicleManagement.Core.Entities;
using VehicleManagement.Core.Services;

namespace VehicleManagement.Tests.BusinessRules;

public class CategoryEditTests
{
    private readonly CategoryService _service = new();

    [Fact]
    public void EditedConfiguration_AdjacentBoundaryMovesTogether_IsValid()
    {
        var categories = new List<VehicleCategory>
        {
            new() { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 600m, Icon = "car" },
            new() { Id = 2, Name = "Medium", MinWeight = 600m, MaxWeight = 2500m, Icon = "van" },
            new() { Id = 3, Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" }
        };

        Assert.Empty(_service.ValidateConfiguration(categories));
    }

    [Fact]
    public void EditedConfiguration_OnlyOneSideOfBoundaryMoves_CreatesGapAndIsRejected()
    {
        var categories = new List<VehicleCategory>
        {
            new() { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 500m, Icon = "car" },
            new() { Id = 2, Name = "Medium", MinWeight = 600m, MaxWeight = 2500m, Icon = "van" },
            new() { Id = 3, Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" }
        };

        var errors = _service.ValidateConfiguration(categories);

        Assert.Contains(errors, e => e.Contains("gap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EditedConfiguration_OnlyOneSideOfBoundaryMoves_CreatesOverlapAndIsRejected()
    {
        var categories = new List<VehicleCategory>
        {
            new() { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 700m, Icon = "car" },
            new() { Id = 2, Name = "Medium", MinWeight = 600m, MaxWeight = 2500m, Icon = "van" },
            new() { Id = 3, Name = "Heavy", MinWeight = 2500m, MaxWeight = null, Icon = "truck" }
        };

        var errors = _service.ValidateConfiguration(categories);

        Assert.Contains(errors, e => e.Contains("overlap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EditedFinalCategory_RemainsOpenEnded_IsValid()
    {
        var categories = new List<VehicleCategory>
        {
            new() { Id = 1, Name = "Light", MinWeight = 0m, MaxWeight = 500m, Icon = "car" },
            new() { Id = 2, Name = "Medium", MinWeight = 500m, MaxWeight = 2000m, Icon = "van" },
            new() { Id = 3, Name = "Very Heavy", MinWeight = 2000m, MaxWeight = null, Icon = "heavy-truck" }
        };

        Assert.Empty(_service.ValidateConfiguration(categories));
    }
}
