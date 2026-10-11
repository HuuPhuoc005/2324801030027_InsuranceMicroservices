using MediatR;
using ChatService.Api.Queries;
namespace ChatService.Queries;
public class GetHealthHandler : IRequestHandler<GetHealthQuery, ServiceHealth>
{
    public Task<ServiceHealth> Handle(GetHealthQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new ServiceHealth("ChatService", "UP"));
}
