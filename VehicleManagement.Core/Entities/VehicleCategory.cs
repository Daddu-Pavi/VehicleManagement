namespace VehicleManagement.Core.Entities;

public class VehicleCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal MinWeight { get; set; }

    public decimal? MaxWeight { get; set; }

    public string Icon { get; set; } = string.Empty;
}