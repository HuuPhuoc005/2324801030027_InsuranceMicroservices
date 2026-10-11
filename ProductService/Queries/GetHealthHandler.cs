using MediatR;
using ProductService.Api.Queries;
namespace ProductService.Queries;
public class GetHealthHandler : IRequestHandler<GetHealthQuery, ServiceHealth>
{
    public Task<ServiceHealth> Handle(GetHealthQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new ServiceHealth("ProductService", "UP"));
}
