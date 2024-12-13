namespace LaundryDashAPI_2.DTOs.Dashboard
{
    public class WeeklySalesReportDTO
    {
        public string ServiceName { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public int NumberOfOrders { get; set; } // Count should be an integer
        public decimal AverageOrderValue { get; set; } // Average should be a decimal
        public decimal TotalSalesAmount { get; set; } // Total sales amount should be decimal
    }

}
