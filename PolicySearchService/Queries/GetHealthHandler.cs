using MediatR;
using PolicySearchService.Api.Queries;
namespace PolicySearchService.Queries;
public class GetHealthHandler : IRequestHandler<GetHealthQuery, ServiceHealth>
{
    public Task<ServiceHealth> Handle(GetHealthQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new ServiceHealth("PolicySearchService", "UP"));
}
