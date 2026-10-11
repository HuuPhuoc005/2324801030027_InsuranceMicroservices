using MediatR;
using DashboardService.Api.Queries.Dtos;
namespace DashboardService.Api.Queries;
public class GetTotalSalesQuery : IRequest<GetTotalSalesResult>
{
    public string? ProductCode { get; set; }
    public DateTime SalesDateFrom { get; set; }
    public DateTime SalesDateTo { get; set; }
}
public class GetAgentsSalesQuery : IRequest<GetAgentsSalesResult>
{
    public DateTime SalesDateFrom { get; set; }
    public DateTime SalesDateTo { get; set; }
}
public class GetSalesTrendsQuery : IRequest<GetSalesTrendsResult>
{
    public DateTime SalesDateFrom { get; set; }
    public DateTime SalesDateTo { get; set; }
    public TimeUnit Unit { get; set; }
}
public class GetTotalSalesResult
{
    public SalesDto Total { get; set; } = new();
    public Dictionary<string, SalesDto> PerProductTotal { get; set; } = new();
}
public class GetAgentsSalesResult { public Dictionary<string, SalesDto> PerAgentTotal { get; set; } = new(); }
public class GetSalesTrendsResult { public List<PeriodSalesDto> PeriodsSales { get; set; } = new(); }
