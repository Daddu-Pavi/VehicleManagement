using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleManagement.Web.Data;

namespace VehicleManagement.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int TotalVehicles { get; private set; }

    public int TotalCategories { get; private set; }

    public async Task OnGetAsync()
    {
        TotalVehicles = await _context.Vehicles.CountAsync();

        TotalCategories = await _context.VehicleCategories.CountAsync();
    }
}