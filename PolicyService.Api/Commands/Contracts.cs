using MediatR;
using PolicyService.Api.Commands.Dtos;
namespace PolicyService.Api.Commands;
public class CreateOfferCommand : IRequest<CreateOfferResult>
{
    public string ProductCode { get; set; } = "";
    public DateTime PolicyFrom { get; set; }
    public DateTime PolicyTo { get; set; }
    public List<string> SelectedCovers { get; set; } = new();
    public List<QuestionAnswer> Answers { get; set; } = new();
}
public class CreateOfferResult
{
    public string OfferNumber { get; set; } = "";
    public decimal TotalPrice { get; set; }
}
public class CreatePolicyCommand : IRequest<CreatePolicyResult>
{
    public string OfferNumber { get; set; } = "";
    public PersonDto PolicyHolder { get; set; } = new();
    public AddressDto PolicyHolderAddress { get; set; } = new();
}
public class CreatePolicyResult { public string PolicyNumber { get; set; } = ""; }
