using MediatR;
namespace PolicyService.Api.Queries;
public record GetPolicyDetailsQuery(string PolicyNumber) : IRequest<GetPolicyDetailsQueryResult>;
public class GetPolicyDetailsQueryResult { public PolicyDetailsDto Policy { get; set; } = new(); }
public class PolicyDetailsDto
{
    public string Number { get; set; } = "";
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string PolicyHolder { get; set; } = "";
    public decimal TotalPremium { get; set; }
    public string ProductCode { get; set; } = "";
    public List<string> Covers { get; set; } = new();
}
