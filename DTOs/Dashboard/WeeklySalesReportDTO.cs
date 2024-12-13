public class WeeklySalesReportDTO
{
    public string ServiceName { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public int NumberOfOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal TotalSalesAmount { get; set; }
}
