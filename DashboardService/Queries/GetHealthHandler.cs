using MediatR;
using DashboardService.Api.Queries;
namespace DashboardService.Queries;
public class GetHealthHandler : IRequestHandler<GetHealthQuery, ServiceHealth>
{
    public Task<ServiceHealth> Handle(GetHealthQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new ServiceHealth("DashboardService", "UP"));
}
