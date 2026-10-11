namespace PolicySearchService.Api.Queries.Dtos;
public class PolicyDto
{
    public string PolicyNumber { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public DateTime PolicyStartDate { get; set; }
    public DateTime PolicyEndDate { get; set; }
    public string PolicyHolder { get; set; } = "";
    public decimal PremiumAmount { get; set; }
}
