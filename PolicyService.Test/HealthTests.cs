using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PolicyService.Api.Queries;
using PolicyService.Queries;
namespace PolicyService.Test;
public class HealthTests
{
    [Fact]
    public async Task MediatorResolvesTheServiceHandler()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetHealthHandler>());
        using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<ISender>().Send(new GetHealthQuery());
        Assert.Equal("PolicyService", result.Service);
        Assert.Equal("UP", result.Status);
    }
}
