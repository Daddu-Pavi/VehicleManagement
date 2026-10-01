namespace VehicleManagement.Core.Validation;

public static class WeightValidation
{
    private const int MaximumDecimalPlaces = 2;

    /// <summary>
    /// Determines whether a weight value contains more than
    /// the maximum supported number of decimal places.
    /// </summary>
    public static bool HasMoreThanTwoDecimalPlaces(
        decimal value)
    {
        return decimal.Round(
            value,
            MaximumDecimalPlaces) != value;
    }
}