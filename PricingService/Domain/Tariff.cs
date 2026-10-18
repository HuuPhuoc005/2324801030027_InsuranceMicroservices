using DynamicExpresso;
using DynamicExpresso.Exceptions;

namespace PricingService.Domain;

public sealed class Tariff(string code)
{
    public string Code { get; } = code;
    public BasePremiumRules BasePremiumRules { get; set; } = new();
    public DiscountMarkupRules DiscountMarkupRules { get; set; } = new();

    public Price CalculatePrice(Calculation calculation)
    {
        if (calculation.ProductCode != Code) throw new PricingInputException("The tariff does not match the product.");
        if (calculation.SelectedCovers.Distinct(StringComparer.Ordinal).Count() != calculation.SelectedCovers.Count)
            throw new PricingInputException("A cover may only be selected once.");

        var interpreter = new Interpreter();
        foreach (var (name, value) in calculation.Subject)
        {
            if (value is not (decimal or string)) throw new PricingInputException($"Unsupported answer for {name}.");
            interpreter.SetVariable(name, value);
        }
        try
        {
            var factor = DiscountMarkupRules.Rules
                .Where(rule => interpreter.Eval<bool>(rule.Condition))
                .Aggregate(1M, (current, rule) => current * rule.Factor);
            var price = new Price();
            foreach (var cover in calculation.SelectedCovers)
            {
                var matchingRules = BasePremiumRules.Rules.Where(rule => rule.CoverCode == cover &&
                    (string.IsNullOrWhiteSpace(rule.Condition) || interpreter.Eval<bool>(rule.Condition))).ToList();
                if (matchingRules.Count == 0) throw new PricingInputException($"No price rule matches cover {cover} and the supplied answers.");
                if (matchingRules.Count > 1) throw new InvalidOperationException($"Multiple price rules match cover {cover}.");
                var amount = interpreter.Eval<decimal>(matchingRules[0].Expression) * factor;
                if (amount < 0) throw new PricingInputException("A calculated premium cannot be negative.");
                price.Covers.Add(cover, new CoverPrice(decimal.Round(amount, 2, MidpointRounding.AwayFromZero)));
            }
            return price;
        }
        catch (DynamicExpressoException error)
        {
            throw new PricingInputException($"The supplied answers cannot be used with this tariff: {error.Message}");
        }
    }
}

public sealed record BasePremiumRule(string CoverCode, string? Condition, string Expression);
public sealed class BasePremiumRules
{
    public List<BasePremiumRule> Rules { get; set; } = new();
    public void AddBasePriceRule(string coverCode, string? condition, string expression)
        => Rules.Add(new BasePremiumRule(coverCode, condition, expression));
}

public sealed record DiscountMarkupRule(string Condition, decimal Factor);
public sealed class DiscountMarkupRules
{
    public List<DiscountMarkupRule> Rules { get; set; } = new();
    public void AddPercentMarkup(string condition, decimal factor)
    {
        if (factor < 0) throw new ArgumentOutOfRangeException(nameof(factor));
        Rules.Add(new DiscountMarkupRule(condition, factor));
    }
}
