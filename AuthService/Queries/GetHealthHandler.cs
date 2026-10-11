using MediatR;
using AuthService.Api.Queries;
namespace AuthService.Queries;
public class GetHealthHandler : IRequestHandler<GetHealthQuery, ServiceHealth>
{
    public Task<ServiceHealth> Handle(GetHealthQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new ServiceHealth("AuthService", "UP"));
}
