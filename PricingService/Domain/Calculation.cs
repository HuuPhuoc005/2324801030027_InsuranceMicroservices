namespace PricingService.Domain;

public sealed record Calculation(string ProductCode, DateTimeOffset PolicyFrom, DateTimeOffset PolicyTo,
    IList<string> SelectedCovers, IDictionary<string, object> Subject);

public sealed record CoverPrice(decimal Price);
public sealed class Price
{
    public Dictionary<string, CoverPrice> Covers { get; } = new(StringComparer.Ordinal);
    public decimal TotalPremium => Covers.Values.Sum(cover => cover.Price);
}

public sealed class PricingInputException(string message) : Exception(message);
