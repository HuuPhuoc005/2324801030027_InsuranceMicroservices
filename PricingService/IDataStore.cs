using PricingService.Domain;

namespace PricingService;

public interface IDataStore
{
    ITariffRepository Tariffs { get; }
}

public interface ITariffRepository
{
    Task<Tariff> this[string productCode] { get; }
}
