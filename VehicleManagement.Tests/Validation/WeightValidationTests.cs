using VehicleManagement.Core.Validation;

namespace VehicleManagement.Tests.Validation;

public class WeightValidationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(1.1)]
    [InlineData(1.12)]
    [InlineData(500.00)]
    [InlineData(1850.75)]
    public void HasMoreThanTwoDecimalPlaces_ValidPrecision_ReturnsFalse(decimal value)
    {
        Assert.False(WeightValidation.HasMoreThanTwoDecimalPlaces(value));
    }

    [Theory]
    [InlineData(1.123)]
    [InlineData(500.001)]
    [InlineData(1850.751)]
    public void HasMoreThanTwoDecimalPlaces_TooPrecise_ReturnsTrue(decimal value)
    {
        Assert.True(WeightValidation.HasMoreThanTwoDecimalPlaces(value));
    }
}
