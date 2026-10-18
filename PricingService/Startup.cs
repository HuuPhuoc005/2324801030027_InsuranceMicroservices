using MediatR;
using Npgsql;
using PricingService.Api.Queries;
using PricingService.Infrastructure;
using Steeltoe.Discovery.Client;

namespace PricingService;

public sealed class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddNewtonsoftJson();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());
        // Steeltoe 3.x manages registration/heartbeat/shutdown as hosted services.
        services.AddDiscoveryClient(Configuration);
        services.AddSingleton(_ => NpgsqlDataSource.Create(Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.")));
        services.AddSingleton<ITariffRepository, PostgresTariffRepository>();
        services.AddSingleton<IDataStore, PostgresDataStore>();
        services.AddHostedService<TariffDatabaseInitializer>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<PricingExceptionMiddleware>();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/info", () => new { service = "PricingService", description = "Insurance premium calculation", health = "/health" });
            endpoints.MapGet("/health", (ISender sender, CancellationToken ct) => sender.Send(new GetHealthQuery(), ct));
            endpoints.MapGet("/api/pricing/health", (ISender sender, CancellationToken ct) => sender.Send(new GetHealthQuery(), ct));
        });
    }
}
