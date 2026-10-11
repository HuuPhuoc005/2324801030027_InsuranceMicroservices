using MediatR;
using Microsoft.Extensions.DependencyInjection;
using DocumentService.Api.Queries;
using DocumentService.Queries;
namespace DocumentService.Test;
public class HealthTests
{
    [Fact]
    public async Task MediatorResolvesTheServiceHandler()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetHealthHandler>());
        using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<ISender>().Send(new GetHealthQuery());
        Assert.Equal("DocumentService", result.Service);
        Assert.Equal("UP", result.Status);
    }
}
