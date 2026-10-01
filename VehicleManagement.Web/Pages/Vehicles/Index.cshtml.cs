using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleManagement.Core.Interfaces;
using VehicleManagement.Web.Data;

namespace VehicleManagement.Web.Pages.Vehicles;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ICategoryService _categoryService;

    public IndexModel(
        ApplicationDbContext context,
        ICategoryService categoryService)
    {
        _context = context;
        _categoryService = categoryService;
    }

    public List<VehicleListItem> Vehicles { get; set; } = [];

    public string SortBy { get; set; } = "owner";

    public string SortDirection { get; set; } = "asc";


    public async Task OnGetAsync(
        string sortBy = "owner",
        string sortDirection = "asc")
    {
        // -------------------------------------------------
        // Normalise sorting values
        // -------------------------------------------------

        SortBy = sortBy.ToLowerInvariant();

        SortDirection =
            sortDirection.Equals(
                "desc",
                StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";


        // -------------------------------------------------
        // Start vehicle query
        // -------------------------------------------------

        var query = _context.Vehicles
            .AsNoTracking()
            .Include(v => v.Manufacturer)
            .AsQueryable();


        // -------------------------------------------------
        // Apply sorting
        // -------------------------------------------------

        query = (SortBy, SortDirection) switch
        {
            ("owner", "desc") =>
                query.OrderByDescending(v => v.OwnerName),

            ("manufacturer", "asc") =>
                query.OrderBy(v => v.Manufacturer.Name),

            ("manufacturer", "desc") =>
                query.OrderByDescending(v => v.Manufacturer.Name),

            ("year", "asc") =>
                query.OrderBy(v => v.YearOfManufacture),

            ("year", "desc") =>
                query.OrderByDescending(v => v.YearOfManufacture),

            ("weight", "asc") =>
                query.OrderBy(v => v.WeightKg),

            ("weight", "desc") =>
                query.OrderByDescending(v => v.WeightKg),

            _ =>
                query.OrderBy(v => v.OwnerName)
        };


        // -------------------------------------------------
        // Execute vehicle query
        // -------------------------------------------------

        var vehicles = await query.ToListAsync();


        // -------------------------------------------------
        // Load current categories
        // -------------------------------------------------

        var categories = await _context.VehicleCategories
            .AsNoTracking()
            .OrderBy(c => c.MinWeight)
            .ToListAsync();


        // -------------------------------------------------
        // Build list for UI
        // -------------------------------------------------

        Vehicles = vehicles
            .Select(vehicle =>
            {
                var category =
                    _categoryService.GetCategoryForWeight(
                        vehicle.WeightKg,
                        categories);

                return new VehicleListItem
                {
                    Id = vehicle.Id,

                    OwnerName =
                        vehicle.OwnerName,

                    Manufacturer =
                        vehicle.Manufacturer.Name,

                    YearOfManufacture =
                        vehicle.YearOfManufacture,

                    WeightKg =
                        vehicle.WeightKg,

                    CategoryName =
                        category?.Name ?? "Uncategorised",

                    CategoryIcon =
                        category?.Icon ?? string.Empty
                };
            })
            .ToList();
    }


    public class VehicleListItem
    {
        public int Id { get; set; }

        public string OwnerName { get; set; }
            = string.Empty;

        public string Manufacturer { get; set; }
            = string.Empty;

        public int YearOfManufacture { get; set; }

        public decimal WeightKg { get; set; }

        public string CategoryName { get; set; }
            = string.Empty;

        public string CategoryIcon { get; set; }
            = string.Empty;
    }
}