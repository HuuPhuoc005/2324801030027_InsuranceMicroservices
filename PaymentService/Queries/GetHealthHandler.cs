using MediatR;
using PaymentService.Api.Queries;
namespace PaymentService.Queries;
public class GetHealthHandler : IRequestHandler<GetHealthQuery, ServiceHealth>
{
    public Task<ServiceHealth> Handle(GetHealthQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new ServiceHealth("PaymentService", "UP"));
}
