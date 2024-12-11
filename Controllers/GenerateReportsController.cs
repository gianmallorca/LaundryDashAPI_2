using AutoMapper;
using LaundryDashAPI_2.DTOs.Dashboard;
using LaundryDashAPI_2.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LaundryDashAPI_2.Controllers
{
    [Route("api/generateReports")]
    [ApiController]
    public class GenerateReportsController : Controller
    {
        private readonly ILogger<LaundryServiceLogController> logger;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly UserManager<ApplicationUser> userManager;

        public GenerateReportsController(ILogger<LaundryServiceLogController> logger, ApplicationDbContext context, IMapper mapper, UserManager<ApplicationUser> userManager)
        {
            this.logger = logger;
            this.context = context;
            this.mapper = mapper;
            this.userManager = userManager;
        }

        [HttpGet("GenerateDailySales/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<DailySalesReportDTO>>> GetDailySalesReportAsync(Guid id, DateTime startDate, DateTime endDate)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Step 2: Fetch the logged-in user
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Step 3: Query to fetch the daily sales report
            var salesReport = await context.BookingLogs
                .Where(b => b.BookingDate >= startDate && b.BookingDate <= endDate
                        && !b.IsCanceled
                        && b.LaundryServiceLog.LaundryShop.LaundryShopId == id) // Filter by date and non-canceled bookings
                .GroupBy(b => new { b.BookingDate, b.LaundryServiceLog.Service.ServiceName }) // Group by BookingDate and ServiceName
                .Select(g => new DailySalesReportDTO
                {
                    BookingDate = g.Key.BookingDate.Date, // Ensure it's just the date part
                    ServiceName = g.Key.ServiceName,
                    NumberOfOrders = g.Count(), // Count the orders in each group
                    AverageOrderValue = g.Average(b => b.TotalPrice ?? 0), // Average price of orders in the group
                    TotalSalesAmount = g.Sum(b => b.TotalPrice ?? 0) // Total sales for the service
                })
                .ToListAsync(); // Fetch the entire list

            if (salesReport == null || !salesReport.Any())
            {
                return NotFound("No sales data found for the specified date range.");
            }

            return Ok(salesReport);
        }

        [HttpGet("GenerateWeeklySales/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<WeeklySalesReportDTO>>> GetWeeklySalesReportAsync(Guid id, DateTime startDate, DateTime endDate)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Step 2: Fetch the logged-in user
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Step 3: Query to fetch the weekly sales report
            var salesReport = await context.BookingLogs
                .Where(b => b.BookingDate >= startDate && b.BookingDate <= endDate
                        && !b.IsCanceled
                        && b.LaundryServiceLog.LaundryShop.LaundryShopId == id) // Filter by date and non-canceled bookings
                .GroupBy(b => new
                {
                    WeekStartDate = StartOfWeek(b.BookingDate), // Calculate the start of the week
                    b.LaundryServiceLog.Service.ServiceName
                }) // Group by WeekStartDate and ServiceName
                .Select(g => new WeeklySalesReportDTO
                {
                    WeekStartDate = g.Key.WeekStartDate, // Start of the week
                    ServiceName = g.Key.ServiceName,
                    NumberOfOrders = g.Count(), // Count the orders in each group
                    AverageOrderValue = g.Average(b => b.TotalPrice ?? 0), // Average price of orders in the group
                    TotalSalesAmount = g.Sum(b => b.TotalPrice ?? 0) // Total sales for the service
                })
                .ToListAsync(); // Fetch the entire list

            if (salesReport == null || !salesReport.Any())
            {
                return NotFound("No sales data found for the specified date range.");
            }

            return Ok(salesReport);
        }

        // Helper method to calculate the start date of the week (you may adjust the start day of the week if needed)
        [ApiExplorerSettings(IgnoreApi = true)]
        private DateTime StartOfWeek(DateTime date)
        {   
            var dayOfWeek = (int)date.DayOfWeek;
            var difference = dayOfWeek - (int)DayOfWeek.Monday; // Adjust to Monday as the start of the week
            return date.AddDays(-difference).Date;
        }

        [HttpGet("GenerateMonthlySales/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<MonthlySalesReportDTO>>> GetMonthlySalesReportAsync(Guid id, DateTime startDate, DateTime endDate)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Step 2: Fetch the logged-in user
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Step 3: Query to fetch the monthly sales report
            var salesReport = await context.BookingLogs
                .Where(b => b.BookingDate >= startDate && b.BookingDate <= endDate
                            && !b.IsCanceled
                            && b.LaundryServiceLog.LaundryShop.LaundryShopId == id) // Filter by date range, non-canceled bookings, and the specific laundry shop
                .GroupBy(b => new
                {
                    Year = b.BookingDate.Year,
                    Month = b.BookingDate.Month,
                    ServiceName = b.LaundryServiceLog.Service.ServiceName
                }) // Group by Year, Month, and ServiceName
                .Select(g => new MonthlySalesReportDTO
                {
                    MonthStartDate = new DateTime(g.Key.Year, g.Key.Month, 1), // Set to the first date of the month
                    ServiceName = g.Key.ServiceName,
                    NumberOfOrders = g.Count(), // Count the orders in the group
                    AverageOrderValue = g.Average(b => b.TotalPrice ?? 0), // Average price of orders in the group
                    TotalSalesAmount = g.Sum(b => b.TotalPrice ?? 0) // Total sales for the service
                })
                .ToListAsync(); // Fetch the entire list

            if (salesReport == null || !salesReport.Any())
            {
                return NotFound("No sales data found for the specified date range.");
            }

            return Ok(salesReport);
        }
    }
}

