using FluentValidation;
using MediatR;
using PricingService.Api.Commands;
using PricingService.Domain;

namespace PricingService.Commands;

public sealed class CalculatePriceHandler(IDataStore dataStore) : IRequestHandler<CalculatePriceCommand, CalculatePriceResult>
{
    public async Task<CalculatePriceResult> Handle(CalculatePriceCommand request, CancellationToken cancellationToken)
    {
        await new CalculatePriceCommandValidator().ValidateAndThrowAsync(request, cancellationToken);
        var answers = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var answer in request.Answers!)
            if (!answers.TryAdd(answer.QuestionCode, answer.GetAnswer()))
                throw new PricingInputException($"Duplicate answer for question {answer.QuestionCode}.");

        cancellationToken.ThrowIfCancellationRequested();
        var tariff = await dataStore.Tariffs[request.ProductCode];
        var price = tariff.CalculatePrice(new Calculation(request.ProductCode, request.PolicyFrom, request.PolicyTo,
            request.SelectedCovers!, answers));
        return new CalculatePriceResult
        {
            TotalPrice = price.TotalPremium,
            CoverPrices = price.Covers.ToDictionary(cover => cover.Key, cover => cover.Value.Price)
        };
    }
}
