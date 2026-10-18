using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Alba;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace PricingService.IntegrationTest;

public class IntegrationTestsFixture : IAsyncLifetime
{
    private PostgreSqlContainer? pgSqlContainer;
    private string? adminConnectionString;
    private string? testDatabaseName;

    public IAlbaHost SystemUnderTest { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        string connectionString;
        adminConnectionString = Environment.GetEnvironmentVariable("PRICING_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(adminConnectionString))
        {
            // Default: preserve the lecturer's disposable PostgreSQL container.
            pgSqlContainer = new PostgreSqlBuilder().WithDatabase("lab_netmicro_pricing").WithCleanUp(true).Build();
            await pgSqlContainer.StartAsync();
            connectionString = pgSqlContainer.GetConnectionString();
        }
        else
        {
            // Explicit native mode: every run gets a separate database, never the lab/production database.
            testDatabaseName = "pricing_test_" + Guid.NewGuid().ToString("N");
            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{testDatabaseName}\"", connection);
            await create.ExecuteNonQueryAsync();
            connectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
                { Database = testDatabaseName }.ConnectionString;
        }

        var hostBuilder = Program.CreateWebHostBuilder(Array.Empty<string>())
            .ConfigureServices((Action<HostBuilderContext, IServiceCollection>)SetupServices)
            .ConfigureAppConfiguration((ctx, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                    ["eureka:client:shouldRegisterWithEureka"] = "false",
                    ["eureka:client:shouldFetchRegistry"] = "false"
                });
            });

        SystemUnderTest = new AlbaHost(hostBuilder);
    }

    public async Task DisposeAsync()
    {
        if (SystemUnderTest is not null) await SystemUnderTest.DisposeAsync();
        if (pgSqlContainer is not null) await pgSqlContainer.DisposeAsync();
        if (testDatabaseName is not null && adminConnectionString is not null)
        {
            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{testDatabaseName}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }

    protected virtual void SetupServices(HostBuilderContext ctx, IServiceCollection services)
    {
    }

    protected Task SetupData()
    {
        return Task.CompletedTask;
    }
}
