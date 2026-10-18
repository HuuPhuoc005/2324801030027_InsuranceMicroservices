using PricingService.Domain;
using Xunit;

namespace PricingService.Test.Domain;

public class TariffEdgeCases
{
    [Fact]
    public void WorldTravelAppliesTheMarkupToEverySelectedCover()
    {
        var calculation = new Calculation("TRI", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(5),
            new List<string> { "C1", "C2", "C3" }, new Dictionary<string, object>
            { ["NUM_OF_ADULTS"] = 1M, ["NUM_OF_CHILDREN"] = 1M, ["DESTINATION"] = "WORLD" });
        var price = TariffFactory.Travel().CalculatePrice(calculation);
        Assert.Equal(159M, price.TotalPremium);
        Assert.Equal(51M, price.Covers["C1"].Price);
        Assert.Equal(78M, price.Covers["C2"].Price);
        Assert.Equal(30M, price.Covers["C3"].Price);
    }

    [Fact]
    public void MultipleHouseMarkupsAreMultipliedAndRoundedPerCover()
    {
        var calculation = new Calculation("HSI", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1),
            new List<string> { "C1", "C2", "C3" }, new Dictionary<string, object>
            { ["TYP"] = "APT", ["AREA"] = 95M, ["NUM_OF_CLAIM"] = 2M, ["FLOOD"] = "YES" });
        Assert.Equal(323.44M, TariffFactory.House().CalculatePrice(calculation).TotalPremium);
    }

    [Theory]
    [InlineData("C1", "C1")]
    [InlineData("UNKNOWN", "C1")]
    public void InvalidOrRepeatedCoversAreRejected(string first, string second)
    {
        var calculation = new Calculation("CAR", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1),
            new List<string> { first, second }, new Dictionary<string, object> { ["NUM_OF_CLAIM"] = 1M });
        Assert.Throws<PricingInputException>(() => TariffFactory.Car().CalculatePrice(calculation));
    }
}
