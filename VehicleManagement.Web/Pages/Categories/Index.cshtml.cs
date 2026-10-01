using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleManagement.Core.Entities;
using VehicleManagement.Core.Interfaces;
using VehicleManagement.Core.Validation;
using VehicleManagement.Web.Data;

namespace VehicleManagement.Web.Pages.Categories;

public class IndexModel : PageModel
{
    private const int MaxCategoryNameLength = 20;

    private readonly ApplicationDbContext _context;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ApplicationDbContext context,
        ICategoryService categoryService,
        ILogger<IndexModel> logger)
    {
        _context = context;
        _categoryService = categoryService;
        _logger = logger;
    }

    public List<VehicleCategory> Categories { get; private set; } = [];

    public List<decimal> VehicleWeights { get; private set; } = [];

    [BindProperty]
    public List<CategoryInput> ProposedCategories { get; set; } = [];

    // Existing categories may only disappear from the submitted configuration
    // after the user explicitly confirms a Delete or an affected-category review.
    [BindProperty]
    public List<int> ConfirmedDeletedCategoryIds { get; set; } = [];

    public string? PageErrorMessage { get; private set; }

    public class CategoryInput
    {
        public int Id { get; set; }
        public string ClientKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal MinWeight { get; set; }
        public decimal? MaxWeight { get; set; }
        public string Icon { get; set; } = "other";
    }

    public async Task OnGetAsync()
    {
        try
        {
            await LoadCategoriesAsync();
            ProposedCategories = Categories.Select(ToInput).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load vehicle categories.");
            PageErrorMessage = "The category configuration could not be loaded. Please try again.";
            Categories = [];
            ProposedCategories = [];
        }
    }

    public async Task<IActionResult> OnPostSaveChangesAsync()
    {
        try
        {
            await LoadCategoriesAsync();
            NormalizeSubmittedConfiguration();
            ValidateSubmittedConfiguration();

            var databaseCategories = await _context.VehicleCategories
                .OrderBy(c => c.MinWeight)
                .ToListAsync();

            ValidateSubmittedIds(databaseCategories);

            var proposedConfiguration = ProposedCategories
                .OrderBy(c => c.MinWeight)
                .Select(c => new VehicleCategory
                {
                    Id = c.Id,
                    Name = c.Name,
                    MinWeight = c.MinWeight,
                    MaxWeight = c.MaxWeight,
                    Icon = c.Icon
                })
                .ToList();

            if (ModelState.IsValid)
            {
                foreach (var error in _categoryService.ValidateConfiguration(proposedConfiguration))
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var submittedExistingIds = ProposedCategories
                .Where(c => c.Id > 0)
                .Select(c => c.Id)
                .ToHashSet();

            var categoriesToDelete = databaseCategories
                .Where(c => !submittedExistingIds.Contains(c.Id))
                .ToList();

            if (categoriesToDelete.Count > 0)
            {
                var confirmedDeleteIds = (ConfirmedDeletedCategoryIds ?? [])
                    .Where(id => id > 0)
                    .ToHashSet();

                var unconfirmedDeletes = categoriesToDelete
                    .Where(c => !confirmedDeleteIds.Contains(c.Id))
                    .Select(c => c.Name)
                    .ToList();

                if (unconfirmedDeletes.Count > 0)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "One or more categories would be removed without confirmation. " +
                        "Please review the affected categories and confirm their removal.");

                    return Page();
                }

                _context.VehicleCategories.RemoveRange(categoriesToDelete);
            }

            foreach (var input in ProposedCategories.Where(c => c.Id > 0))
            {
                var existing = databaseCategories.Single(c => c.Id == input.Id);
                existing.Name = input.Name;
                existing.MinWeight = input.MinWeight;
                existing.MaxWeight = input.MaxWeight;
                existing.Icon = input.Icon;
            }

            foreach (var input in ProposedCategories.Where(c => c.Id == 0))
            {
                _context.VehicleCategories.Add(new VehicleCategory
                {
                    Name = input.Name,
                    MinWeight = input.MinWeight,
                    MaxWeight = input.MaxWeight,
                    Icon = input.Icon
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Category configuration saved successfully.";
            return RedirectToPage("./Index");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while saving category configuration.");
            ModelState.AddModelError(
                string.Empty,
                "The category configuration could not be saved. It may have changed since the page was loaded. Please review the configuration and try again.");

            await ReloadCategoriesSafelyAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while saving category configuration.");
            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while saving the category configuration. Please try again.");

            await ReloadCategoriesSafelyAsync();
            return Page();
        }
    }

    private void NormalizeSubmittedConfiguration()
    {
        ProposedCategories ??= [];

        foreach (var category in ProposedCategories)
        {
            category.Name = category.Name?.Trim() ?? string.Empty;
            category.Icon = category.Icon?.Trim().ToLowerInvariant() ?? string.Empty;
            category.ClientKey = category.ClientKey?.Trim() ?? string.Empty;
        }

        ProposedCategories = ProposedCategories
            .OrderBy(c => c.MinWeight)
            .ToList();
    }

    private void ValidateSubmittedConfiguration()
    {
        if (ProposedCategories.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "At least one category is required.");
            return;
        }

        for (var i = 0; i < ProposedCategories.Count; i++)
        {
            var category = ProposedCategories[i];

            if (category.Id < 0)
            {
                ModelState.AddModelError(string.Empty, "The proposed configuration contains an invalid category identifier.");
            }

            var label = string.IsNullOrWhiteSpace(category.Name)
                ? $"Category {i + 1}"
                : $"'{category.Name}'";

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError(string.Empty, $"Category {i + 1} must have a name.");
            }
            else if (category.Name.Length > MaxCategoryNameLength)
            {
                ModelState.AddModelError(string.Empty, $"{label} cannot exceed {MaxCategoryNameLength} characters.");
            }

            if (!AllowedIcons.Contains(category.Icon, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, $"{label} has an invalid icon.");
            }

            if (category.MinWeight < 0)
            {
                ModelState.AddModelError(string.Empty, $"The lower boundary for {label} cannot be negative.");
            }

            if (WeightValidation.HasMoreThanTwoDecimalPlaces(category.MinWeight))
            {
                ModelState.AddModelError(string.Empty, $"The lower boundary for {label} can have no more than two decimal places.");
            }

            if (category.MaxWeight.HasValue)
            {
                if (WeightValidation.HasMoreThanTwoDecimalPlaces(category.MaxWeight.Value))
                {
                    ModelState.AddModelError(string.Empty, $"The upper boundary for {label} can have no more than two decimal places.");
                }

                if (category.MaxWeight.Value <= category.MinWeight)
                {
                    ModelState.AddModelError(string.Empty, $"The upper boundary for {label} must be greater than its lower boundary.");
                }
            }
        }

        var duplicateNames = ProposedCategories
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateName in duplicateNames)
        {
            ModelState.AddModelError(string.Empty, $"A category named '{duplicateName}' already exists.");
        }

        if (ProposedCategories[0].MinWeight != 0)
        {
            ModelState.AddModelError(string.Empty, "The first category must start at 0 kg.");
        }

        for (var i = 0; i < ProposedCategories.Count - 1; i++)
        {
            var current = ProposedCategories[i];
            var next = ProposedCategories[i + 1];

            if (!current.MaxWeight.HasValue)
            {
                ModelState.AddModelError(string.Empty, $"'{current.Name}' cannot be open-ended because another category follows it.");
                continue;
            }

            if (current.MaxWeight.Value != next.MinWeight)
            {
                ModelState.AddModelError(string.Empty, $"The boundary between '{current.Name}' and '{next.Name}' is invalid. Category ranges must have no gaps or overlaps.");
            }
        }

        var final = ProposedCategories[^1];
        if (final.MaxWeight.HasValue)
        {
            ModelState.AddModelError(string.Empty, $"The final category '{final.Name}' must be open-ended.");
        }
    }

    private void ValidateSubmittedIds(IReadOnlyCollection<VehicleCategory> databaseCategories)
    {
        var submittedExistingIds = ProposedCategories
            .Where(c => c.Id > 0)
            .Select(c => c.Id)
            .ToList();

        if (submittedExistingIds.Count != submittedExistingIds.Distinct().Count())
        {
            ModelState.AddModelError(string.Empty, "The proposed configuration contains a duplicated category.");
            return;
        }

        var databaseIds = databaseCategories.Select(c => c.Id).ToHashSet();
        if (submittedExistingIds.Any(id => !databaseIds.Contains(id)))
        {
            ModelState.AddModelError(
                string.Empty,
                "The category configuration has changed since the page was loaded. Reload the page and try again.");
        }
    }

    // =========================================================
// LOAD CURRENT CATEGORY + VEHICLE DATA
// =========================================================

private async Task LoadCategoriesAsync()
{
    Categories = await _context.VehicleCategories
        .AsNoTracking()
        .OrderBy(c => c.MinWeight)
        .ToListAsync();

    // Only the vehicle weight is required for the
    // affected-category impact preview.
    VehicleWeights = await _context.Vehicles
        .AsNoTracking()
        .Select(v => v.WeightKg)
        .ToListAsync();
}

    private async Task ReloadCategoriesSafelyAsync()
    {
        try
        {
            await LoadCategoriesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to reload categories after a save failure.");
            Categories = [];
        }
    }

    private static CategoryInput ToInput(VehicleCategory category) => new()
    {
        Id = category.Id,
        ClientKey = $"existing-{category.Id}",
        Name = category.Name,
        MinWeight = category.MinWeight,
        MaxWeight = category.MaxWeight,
        Icon = category.Icon
    };

    public static string FormatWeight(decimal value) =>
        value % 1 == 0 ? value.ToString("0") : value.ToString("0.##");

    public string GetIcon(string? icon) => icon?.ToLowerInvariant() switch
    {
        "car" => "🚗",
        "suv" => "🚙",
        "van" => "🚐",
        "pickup" => "🛻",
        "truck" => "🚚",
        "heavy-truck" => "🚛",
        "bus" => "🚌",
        "motorcycle" => "🏍️",
        "tractor" => "🚜",
        "sports-car" => "🏎️",
        _ => "◆"
    };

    private static readonly string[] AllowedIcons =
    [
        "car", "suv", "van", "pickup", "truck", "heavy-truck",
        "bus", "motorcycle", "tractor", "sports-car", "other"
    ];
}
