namespace LaundryDashAPI_2.DTOs.Dashboard
{
    public class MonthlySalesReportDTO
    {
        public DateTime MonthStartDate { get; set; } // Represents the start date of the month
        public string ServiceName { get; set; } // The name of the service
        public int NumberOfOrders { get; set; } // Number of orders for that service in the month
        public decimal AverageOrderValue { get; set; } // Average order value for the service
        public decimal TotalSalesAmount { get; set; } // Total sales amount for the service
        public decimal TotalRevenue { get; set; }
    }
}
