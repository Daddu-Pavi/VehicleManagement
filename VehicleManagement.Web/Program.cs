using Microsoft.EntityFrameworkCore;
using VehicleManagement.Core.Interfaces;
using VehicleManagement.Core.Services;
using VehicleManagement.Core.Validation;
using VehicleManagement.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSession();


// Configure Entity Framework Core with SQL Server.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services.
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<VehicleValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseSession();


app.UseAuthorization();

app.MapStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();