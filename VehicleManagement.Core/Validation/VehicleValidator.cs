using VehicleManagement.Core.Entities;

namespace VehicleManagement.Core.Validation;

public class VehicleValidator
{
    private const int EarliestSensibleYear = 1886;

    public IReadOnlyList<string> Validate(
        Vehicle vehicle,
        bool manufacturerExists,
        int currentYear)
    {
        var errors =
            new List<string>();


        // ============================================
        // OWNER
        // ============================================

        if (string.IsNullOrWhiteSpace(
                vehicle.OwnerName))
        {
            errors.Add(
                "Owner's name is required.");
        }


        // ============================================
        // MANUFACTURER
        // ============================================

        if (vehicle.ManufacturerId <= 0)
        {
            errors.Add(
                "Manufacturer is required.");
        }
        else if (!manufacturerExists)
        {
            errors.Add(
                "The selected manufacturer is invalid.");
        }


        // ============================================
        // YEAR OF MANUFACTURE
        // ============================================

        if (
            vehicle.YearOfManufacture <
                EarliestSensibleYear ||
            vehicle.YearOfManufacture >
                currentYear + 1)
        {
            errors.Add(
                $"Year of manufacture must be between " +
                $"{EarliestSensibleYear} and " +
                $"{currentYear + 1}.");
        }


        // ============================================
        // WEIGHT
        // ============================================

        if (vehicle.WeightKg <= 0)
        {
            errors.Add(
                "Weight must be greater than 0 kg.");
        }


        if (
            WeightValidation
                .HasMoreThanTwoDecimalPlaces(
                    vehicle.WeightKg))
        {
            errors.Add(
                "Weight must have no more than " +
                "two decimal places.");
        }


        return errors;
    }
}