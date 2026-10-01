using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleManagement.Core.Entities;
using VehicleManagement.Web.Data;

namespace VehicleManagement.Web.Pages.Categories;

public class ViewAllModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ViewAllModel(ApplicationDbContext context)
    {
        _context = context;
    }


    // =====================================================
    // PAGE DATA
    // =====================================================

    public List<VehicleCategory> Categories { get; set; } = [];

    public Dictionary<int, int> VehicleCounts { get; set; } = [];


    // =====================================================
    // LOAD PAGE
    // =====================================================

    public async Task OnGetAsync()
    {
        // -------------------------------------------------
        // Load categories in weight-range order
        // -------------------------------------------------

        Categories = await _context.VehicleCategories
            .AsNoTracking()
            .OrderBy(c => c.MinWeight)
            .ToListAsync();


        // -------------------------------------------------
        // Load only vehicle weights needed for counting
        // -------------------------------------------------

        var vehicleWeights = await _context.Vehicles
            .AsNoTracking()
            .Select(v => v.WeightKg)
            .ToListAsync();


        // -------------------------------------------------
        // Count vehicles belonging to each category
        //
        // Category rule:
        // MinWeight <= WeightKg < MaxWeight
        //
        // Final category:
        // WeightKg >= MinWeight
        // -------------------------------------------------

        foreach (var category in Categories)
        {
            var count = vehicleWeights.Count(weight =>
                weight >= category.MinWeight &&
                (
                    !category.MaxWeight.HasValue ||
                    weight < category.MaxWeight.Value
                ));

            VehicleCounts[category.Id] = count;
        }
    }
}