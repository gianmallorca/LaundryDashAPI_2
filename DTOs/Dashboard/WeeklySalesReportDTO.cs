namespace LaundryDashAPI_2.DTOs.Dashboard
{
    public class WeeklySalesReportDTO
    {
        public string ServiceName { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public int NumberOfOrders { get; set; } // Count should be an integer
        public double AverageOrderValue { get; set; } // Average as double
        public double TotalSalesAmount { get; set; } // Total sales amount as double
    }
}
