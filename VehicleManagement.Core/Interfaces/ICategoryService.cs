using VehicleManagement.Core.Entities;

namespace VehicleManagement.Core.Interfaces;

public interface ICategoryService
{
    VehicleCategory? GetCategoryForWeight(
        decimal weight,
        IEnumerable<VehicleCategory> categories);

    IReadOnlyList<string> ValidateConfiguration(
        IEnumerable<VehicleCategory> categories);

    CategoryAddPreview BuildAddPreview(
        string name,
        string icon,
        decimal minWeight,
        decimal? maxWeight,
        IEnumerable<VehicleCategory> categories);
}

public class CategoryAddPreview
{
    public bool CanProceed { get; set; }

    public string? ErrorMessage { get; set; }

    public List<VehicleCategory> ProposedCategories { get; set; } = [];

    public List<string> Changes { get; set; } = [];

    // Categories completely covered by the new range.
    public List<VehicleCategory> CategoriesToDelete { get; set; } = [];

    public bool RequiresDeletion =>
        CategoriesToDelete.Count > 0;
}