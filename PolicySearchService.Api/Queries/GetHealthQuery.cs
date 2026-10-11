using MediatR;
namespace PolicySearchService.Api.Queries;
public record GetHealthQuery : IRequest<ServiceHealth>;
public record ServiceHealth(string Service, string Status);
