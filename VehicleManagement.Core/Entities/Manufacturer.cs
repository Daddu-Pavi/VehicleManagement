namespace VehicleManagement.Core.Entities;

public class Manufacturer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Vehicle> Vehicles { get; set; }
        = new List<Vehicle>();
}