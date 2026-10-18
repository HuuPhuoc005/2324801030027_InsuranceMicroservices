using Newtonsoft.Json;
using Npgsql;
using PricingService.Domain;

namespace PricingService.Infrastructure;

public sealed class PostgresDataStore(ITariffRepository tariffs) : IDataStore
{
    public ITariffRepository Tariffs { get; } = tariffs;
}

public sealed class PostgresTariffRepository(NpgsqlDataSource dataSource) : ITariffRepository
{
    public Task<Tariff> this[string productCode] => FindAsync(productCode);

    private async Task<Tariff> FindAsync(string productCode)
    {
        await using var command = dataSource.CreateCommand("SELECT definition::text FROM pricing_tariffs WHERE code = $1");
        command.Parameters.AddWithValue(productCode);
        var json = await command.ExecuteScalarAsync() as string;
        if (json is null) throw new KeyNotFoundException($"No tariff exists for product {productCode}.");
        return JsonConvert.DeserializeObject<Tariff>(json)
            ?? throw new InvalidOperationException("The saved tariff is invalid.");
    }
}

public sealed class TariffDatabaseInitializer(NpgsqlDataSource dataSource) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var create = dataSource.CreateCommand("""
            CREATE TABLE IF NOT EXISTS pricing_tariffs (
                code text PRIMARY KEY,
                definition jsonb NOT NULL
            );
            """);
        await create.ExecuteNonQueryAsync(cancellationToken);
        foreach (var tariff in new[] { TariffSeed.Travel(), TariffSeed.House(), TariffSeed.Farm(), TariffSeed.Car() })
        {
            await using var insert = dataSource.CreateCommand("""
                INSERT INTO pricing_tariffs (code, definition) VALUES ($1, $2::jsonb)
                ON CONFLICT (code) DO NOTHING;
                """);
            insert.Parameters.AddWithValue(tariff.Code);
            insert.Parameters.AddWithValue(JsonConvert.SerializeObject(tariff));
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
