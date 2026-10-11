using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Api.Queries;
using PaymentService.Queries;
namespace PaymentService.Test;
public class HealthTests
{
    [Fact]
    public async Task MediatorResolvesTheServiceHandler()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetHealthHandler>());
        using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<ISender>().Send(new GetHealthQuery());
        Assert.Equal("PaymentService", result.Service);
        Assert.Equal("UP", result.Status);
    }
}
