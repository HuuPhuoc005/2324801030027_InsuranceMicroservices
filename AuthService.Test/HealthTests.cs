using MediatR;
using Microsoft.Extensions.DependencyInjection;
using AuthService.Api.Queries;
using AuthService.Queries;
namespace AuthService.Test;
public class HealthTests
{
    [Fact]
    public async Task MediatorResolvesTheServiceHandler()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetHealthHandler>());
        using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<ISender>().Send(new GetHealthQuery());
        Assert.Equal("AuthService", result.Service);
        Assert.Equal("UP", result.Status);
    }
}
