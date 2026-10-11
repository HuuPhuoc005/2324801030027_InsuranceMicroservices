using MediatR;
using PolicySearchService.Api.Queries.Dtos;
namespace PolicySearchService.Api.Queries;
public record FindPolicyQuery(string Query) : IRequest<FindPolicyResult>;
public class FindPolicyResult { public List<PolicyDto> Policies { get; set; } = new(); }
