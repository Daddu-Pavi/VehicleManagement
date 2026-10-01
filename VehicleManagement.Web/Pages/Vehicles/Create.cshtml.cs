using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VehicleManagement.Core.Entities;
using VehicleManagement.Core.Interfaces;
using VehicleManagement.Core.Validation;
using VehicleManagement.Web.Data;

namespace VehicleManagement.Web.Pages.Vehicles;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ICategoryService _categoryService;
    private readonly VehicleValidator _vehicleValidator;

    public CreateModel(
        ApplicationDbContext context,
        ICategoryService categoryService,
        VehicleValidator vehicleValidator)
    {
        _context = context;
        _categoryService = categoryService;
        _vehicleValidator = vehicleValidator;
    }


    [BindProperty]
    public Vehicle Vehicle { get; set; } = new();


    public SelectList ManufacturerOptions { get; set; } =
        null!;

    public List<VehicleCategory> Categories { get; private set; } = [];


    // =====================================================
    // GET
    // =====================================================

    public async Task OnGetAsync()
    {
        // Default the year picker to the current year.
        Vehicle.YearOfManufacture =
            DateTime.UtcNow.Year;

        await LoadManufacturersAsync();
        await LoadCategoriesAsync();
    }


    // =====================================================
    // POST
    // =====================================================

    public async Task<IActionResult> OnPostAsync()
    {
        // Reload manufacturers because the page may need
        // to be displayed again when validation fails.
        await LoadManufacturersAsync();


        /*
         * Vehicle.Manufacturer is an EF navigation property.
         *
         * The form sends ManufacturerId, not a complete
         * Manufacturer object.
         *
         * Therefore the navigation property should not prevent
         * the form from being submitted.
         */

        ModelState.Remove(
            "Vehicle.Manufacturer"
        );


        // -------------------------------------------------
        // Validate manufacturer
        // -------------------------------------------------

        var manufacturerExists =
            await _context.Manufacturers
                .AsNoTracking()
                .AnyAsync(
                    manufacturer =>
                        manufacturer.Id ==
                        Vehicle.ManufacturerId
                );


        // -------------------------------------------------
        // Validate vehicle
        // -------------------------------------------------

        var vehicleValidationErrors =
            _vehicleValidator.Validate(
                Vehicle,
                manufacturerExists,
                DateTime.UtcNow.Year
            );


        foreach (
            var error in vehicleValidationErrors
        )
        {
            ModelState.AddModelError(
                string.Empty,
                error
            );
        }


        // -------------------------------------------------
        // Load current category configuration
        // -------------------------------------------------

        await LoadCategoriesAsync();

        var categories = Categories;


        // -------------------------------------------------
        // Validate category configuration
        // -------------------------------------------------

        var categoryValidationErrors =
            _categoryService
                .ValidateConfiguration(
                    categories
                );


        if (
            categoryValidationErrors.Count > 0
        )
        {
            ModelState.AddModelError(
                string.Empty,
                "Vehicle categories are not configured correctly."
            );
        }


        // -------------------------------------------------
        // Make sure vehicle resolves to a category
        // -------------------------------------------------

        if (
            categoryValidationErrors.Count == 0 &&
            Vehicle.WeightKg > 0
        )
        {
            var category =
                _categoryService
                    .GetCategoryForWeight(
                        Vehicle.WeightKg,
                        categories
                    );


            if (category is null)
            {
                ModelState.AddModelError(
                    "Vehicle.WeightKg",
                    "The vehicle weight does not match a valid category."
                );
            }
        }


        // -------------------------------------------------
        // Stop if validation failed
        // -------------------------------------------------

        if (!ModelState.IsValid)
        {
            return Page();
        }


        // -------------------------------------------------
        // Normalise data
        // -------------------------------------------------

        Vehicle.OwnerName =
            Vehicle.OwnerName.Trim();


        // -------------------------------------------------
        // Save vehicle
        // -------------------------------------------------

        _context.Vehicles.Add(
            Vehicle
        );

        await _context
            .SaveChangesAsync();


        // -------------------------------------------------
        // Success message
        // -------------------------------------------------

        TempData["SuccessMessage"] =
            "Vehicle added successfully.";


        // -------------------------------------------------
        // Stay on Add Vehicle page
        //
        // Redirect after POST so refreshing the browser
        // does not submit the same vehicle again.
        // -------------------------------------------------

        return RedirectToPage(
            "./Create"
        );
    }


    // =====================================================
    // CATEGORY CONFIGURATION
    // =====================================================

    private async Task LoadCategoriesAsync()
    {
        Categories =
            await _context.VehicleCategories
                .AsNoTracking()
                .OrderBy(category => category.MinWeight)
                .ToListAsync();
    }


    // =====================================================
    // MANUFACTURER DROPDOWN
    // =====================================================

    private async Task
        LoadManufacturersAsync()
    {
        var manufacturers =
            await _context.Manufacturers
                .AsNoTracking()
                .OrderBy(
                    manufacturer =>
                        manufacturer.Name
                )
                .ToListAsync();


        ManufacturerOptions =
            new SelectList(
                manufacturers,
                nameof(Manufacturer.Id),
                nameof(Manufacturer.Name)
            );
    }
}