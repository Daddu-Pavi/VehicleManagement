using VehicleManagement.Core.Entities;
using VehicleManagement.Core.Validation;

namespace VehicleManagement.Tests.Validation;

public class VehicleValidatorTests
{
    private readonly VehicleValidator _validator = new();

    private static Vehicle ValidVehicle() => new()
    {
        OwnerName = "John Smith",
        ManufacturerId = 1,
        YearOfManufacture = 2024,
        WeightKg = 1500.50m
    };

    [Fact]
    public void Validate_ValidVehicle_ReturnsNoErrors()
    {
        Assert.Empty(_validator.Validate(ValidVehicle(), true, 2026));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Validate_MissingOwner_ReturnsError(string owner)
    {
        var vehicle = ValidVehicle();
        vehicle.OwnerName = owner;

        var errors = _validator.Validate(vehicle, true, 2026);

        Assert.Contains(errors, e => e.Contains("Owner", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_MissingManufacturer_ReturnsError()
    {
        var vehicle = ValidVehicle();
        vehicle.ManufacturerId = 0;

        var errors = _validator.Validate(vehicle, false, 2026);

        Assert.Contains(errors, e => e.Contains("Manufacturer is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ManufacturerDoesNotExist_ReturnsError()
    {
        var errors = _validator.Validate(ValidVehicle(), false, 2026);

        Assert.Contains(errors, e => e.Contains("invalid", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(1885)]
    [InlineData(2028)]
    public void Validate_UnsensibleYear_ReturnsError(int year)
    {
        var vehicle = ValidVehicle();
        vehicle.YearOfManufacture = year;

        var errors = _validator.Validate(vehicle, true, 2026);

        Assert.Contains(errors, e => e.Contains("Year of manufacture", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(1886)]
    [InlineData(2026)]
    [InlineData(2027)]
    public void Validate_SensibleYear_DoesNotReturnYearError(int year)
    {
        var vehicle = ValidVehicle();
        vehicle.YearOfManufacture = year;

        var errors = _validator.Validate(vehicle, true, 2026);

        Assert.DoesNotContain(errors, e => e.Contains("Year of manufacture", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500)]
    public void Validate_NonPositiveWeight_ReturnsError(decimal weight)
    {
        var vehicle = ValidVehicle();
        vehicle.WeightKg = weight;

        var errors = _validator.Validate(vehicle, true, 2026);

        Assert.Contains(errors, e => e.Contains("greater than 0", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(500)]
    [InlineData(500.1)]
    [InlineData(500.12)]
    [InlineData(999999.99)]
    public void Validate_PositiveWeightWithAtMostTwoDecimals_HasNoWeightError(decimal weight)
    {
        var vehicle = ValidVehicle();
        vehicle.WeightKg = weight;

        var errors = _validator.Validate(vehicle, true, 2026);

        Assert.DoesNotContain(errors, e =>
            e.Contains("greater than 0", StringComparison.OrdinalIgnoreCase) ||
            e.Contains("decimal places", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(1.123)]
    [InlineData(500.001)]
    [InlineData(1500.123)]
    public void Validate_MoreThanTwoDecimalPlaces_ReturnsError(decimal weight)
    {
        var vehicle = ValidVehicle();
        vehicle.WeightKg = weight;

        var errors = _validator.Validate(vehicle, true, 2026);

        Assert.Contains(errors, e => e.Contains("two decimal places", StringComparison.OrdinalIgnoreCase));
    }
}
