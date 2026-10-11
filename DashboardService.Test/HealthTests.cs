using MediatR;
using Microsoft.Extensions.DependencyInjection;
using DashboardService.Api.Queries;
using DashboardService.Queries;
namespace DashboardService.Test;
public class HealthTests
{
    [Fact]
    public async Task MediatorResolvesTheServiceHandler()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetHealthHandler>());
        using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<ISender>().Send(new GetHealthQuery());
        Assert.Equal("DashboardService", result.Service);
        Assert.Equal("UP", result.Status);
    }
}
