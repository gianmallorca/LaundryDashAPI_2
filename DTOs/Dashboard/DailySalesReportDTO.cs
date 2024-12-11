namespace LaundryDashAPI_2.DTOs.Dashboard
{
    public class DailySalesReportDTO
    {   
        public DateTime BookingDate { get; set; }               // The booking date 
        public string ServiceName { get; set; }               // The name or type of service
        public int NumberOfOrders { get; set; }           // Number of orders for the service on that day
        public decimal AverageOrderValue { get; set; }   // Average value per order
        public decimal TotalSalesAmount { get; set; }    // Total sales amount for the service

    }
}
