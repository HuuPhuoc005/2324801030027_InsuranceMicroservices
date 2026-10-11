namespace DashboardService.Api.Queries.Dtos;
public enum TimeUnit { Day, Month, Year }
public class SalesDto
{
    public decimal PremiumAmount { get; set; }
    public long PoliciesCount { get; set; }
}
public class PeriodSalesDto
{
    public string Period { get; set; } = "";
    public SalesDto Sales { get; set; } = new();
}
