using AutoMapper;
using LaundryDashAPI_2.DTOs.Dashboard;
using LaundryDashAPI_2.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
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
        public async Task<ActionResult<List<DailySalesReportDTO>>> GetDailySalesReportAsync(Guid id)
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

            // Step 3: Set today's date as startDate and endDate
            var currentDate = DateTime.Now.Date; // Get the current date at midnight (removes time component)

            // Step 4: Query to fetch today's sales report
            var salesReport = await context.BookingLogs
                .Where(b => b.BookingDate >= currentDate
                            && b.BookingDate < currentDate.AddDays(1) // Ensures it includes all bookings for today
                            && !b.IsCanceled
                            && b.LaundryServiceLog.LaundryShop.LaundryShopId == id) // Filter by laundry shop ID
                .Select(b => new
                {
                    b.BookingDate,
                    b.LaundryServiceLog.ServiceIds,
                    b.TotalPrice
                })
                .ToListAsync(); // Fetch all necessary data

            // Step 5: Map service names and aggregate sales data
            var salesReportDTO = salesReport
                .GroupBy(b => new
                {
                    ServiceName = context.Services
                        .Where(service => b.ServiceIds != null && service.ServiceId == b.ServiceIds.FirstOrDefault())
                        .Select(service => service.ServiceName)
                        .FirstOrDefault() ?? "Unknown Service" // Resolve service name or fallback
                })
                .Select(g => new DailySalesReportDTO
                {
                    ServiceName = g.Key.ServiceName,
                    BookingDate = g.Min(b => b.BookingDate).Date, // Ensure it's just the date part
                    NumberOfOrders = g.Count(), // Count the orders in each group
                    AverageOrderValue = g.Average(b => b.TotalPrice ?? 0), // Average price of orders in the group
                    TotalSalesAmount = g.Sum(b => b.TotalPrice ?? 0) // Total sales for the service
                })
                .ToList();

            if (salesReportDTO == null || !salesReportDTO.Any())
            {
                return NotFound("No sales data found for today.");
            }

            return Ok(salesReportDTO);
        }

        [HttpGet("GenerateWeeklySales/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<WeeklySalesReportDTO>>> GetWeeklySalesReportAsync(Guid id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Step 2: Calculate start and end dates for the current week
            var currentDate = DateTime.Now.Date; // Today's date
            var startOfWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek); // Start of the week (Sunday)
            var endOfWeek = startOfWeek.AddDays(7); // End of the week (Saturday midnight)

            // Step 3: Fetch bookings and aggregate sales data for the week
            try
            {
                var bookings = await context.BookingLogs
                    .Where(b => b.BookingDate >= startOfWeek
                                && b.BookingDate < endOfWeek // Ensure bookings fall within the week
                                && !b.IsCanceled
                                && b.LaundryServiceLog.LaundryShop.LaundryShopId == id) // Filter by laundry shop ID
                    .Select(b => new
                    {
                        b.BookingDate,
                        b.LaundryServiceLog.ServiceIds, // Get related services
                        b.TotalPrice
                    })
                    .ToListAsync(); // Fetch all necessary data

                if (bookings == null || !bookings.Any())
                {
                    return NotFound("No bookings found for the specified week.");
                }

                // Step 4: Group bookings by service and aggregate weekly sales data
                var reports = await (from booking in context.BookingLogs
                                     where booking.BookingDate >= startOfWeek && booking.BookingDate < endOfWeek
                                     && booking.LaundryServiceLog.LaundryShop.LaundryShopId == id
                                     group booking by new { booking.LaundryServiceLog.ServiceIds, booking.BookingDate.Date } into g
                                     select new WeeklySalesReportDTO
                                     {
                                         ServiceName = string.Join(", ", g.Key.ServiceIds), // Aggregate service names (if needed)
                                         WeekStartDate = startOfWeek,
                                         WeekEndDate = endOfWeek,
                                         NumberOfOrders = g.Count(),
                                         AverageOrderValue = g.Average(entry => entry.TotalPrice.HasValue ? (double)entry.TotalPrice : 0),  // Cast to double explicitly
                                         TotalSalesAmount = (double)g.Sum(entry => entry.TotalPrice.HasValue ? entry.TotalPrice.Value : 0)  // Sum total sales as double
                                     }).ToListAsync();

                if (reports == null || !reports.Any())
                {
                    return NotFound("No sales data found for this week.");
                }

                return Ok(reports);
            }
            catch (Exception ex)
            {
                // Log the error to help with troubleshooting
                logger.LogError($"Error fetching weekly sales report: {ex.Message}", ex);
                return StatusCode(500, "Internal server error.");
            }
        }








        // Helper method to calculate the start date of the week (you may adjust the start day of the week if needed)
        [ApiExplorerSettings(IgnoreApi = true)]
        private DateTime StartOfWeek(DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            var difference = dayOfWeek - (int)DayOfWeek.Monday; // Adjust to Monday as the start of the week
            return date.AddDays(-difference).Date;
        }


        //download daily
        [HttpGet("GenerateDailySalesPDF/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<IActionResult> GenerateDailySalesReportPDF(Guid id, DateTime startDate, DateTime endDate)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Fetch the daily sales report data
            var salesReport = await context.BookingLogs
                .Where(b => b.BookingDate.Date >= startDate.Date && b.BookingDate.Date <= endDate.Date
                            && !b.IsCanceled
                            && b.LaundryServiceLog.LaundryShop.LaundryShopId == id)
                .GroupBy(b => new { b.BookingDate.Date, b.LaundryServiceLog.ServiceIds })
                .Select(g => new DailySalesReportDTO
                {
                    BookingDate = g.Key.Date,
                    ServiceName = context.Services
                        .Where(s => g.Key.ServiceIds.Contains(s.ServiceId))
                        .Select(s => s.ServiceName)
                        .FirstOrDefault() ?? "Unknown Service",
                    NumberOfOrders = g.Count(),
                    AverageOrderValue = g.Average(b => b.TotalPrice ?? 0),
                    TotalSalesAmount = g.Sum(b => b.TotalPrice ?? 0)
                })
                .ToListAsync();

            if (salesReport == null || !salesReport.Any())
            {
                return NotFound("No sales data found for the specified date range.");
            }

            // Generate PDF
            using var memoryStream = new MemoryStream();
            var pdfDocument = new PdfDocument();
            var page = pdfDocument.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12, XFontStyle.Regular);
            var titleFont = new XFont("Arial", 16, XFontStyle.Bold);
            var yPoint = 40;

            // Add title
            gfx.DrawString($"Daily Sales Report ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})", titleFont, XBrushes.Black,
                new XRect(0, yPoint, page.Width, page.Height), XStringFormats.TopCenter);
            yPoint += 40;

            // Add table headers
            gfx.DrawString("Date", font, XBrushes.Black, 50, yPoint);
            gfx.DrawString("Service Name", font, XBrushes.Black, 150, yPoint);
            gfx.DrawString("Number of Orders", font, XBrushes.Black, 300, yPoint);
            gfx.DrawString("Avg. Order Value", font, XBrushes.Black, 450, yPoint);
            gfx.DrawString("Total Sales", font, XBrushes.Black, 600, yPoint);
            yPoint += 20;

            // Add data rows
            foreach (var report in salesReport)
            {
                gfx.DrawString(report.BookingDate.ToString("yyyy-MM-dd"), font, XBrushes.Black, 50, yPoint);
                gfx.DrawString(report.ServiceName, font, XBrushes.Black, 150, yPoint);
                gfx.DrawString(report.NumberOfOrders.ToString(), font, XBrushes.Black, 300, yPoint);
                gfx.DrawString(report.AverageOrderValue.ToString("C"), font, XBrushes.Black, 450, yPoint);
                gfx.DrawString(report.TotalSalesAmount.ToString("C"), font, XBrushes.Black, 600, yPoint);
                yPoint += 20;

                // Add a new page if content overflows
                if (yPoint > page.Height - 40)
                {
                    page = pdfDocument.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPoint = 40;
                }
            }

            // Save the document to the memory stream
            pdfDocument.Save(memoryStream, false);

            // Return the PDF as a file download
            return File(memoryStream.ToArray(), "application/pdf", $"DailySalesReport_{startDate:yyyyMMdd}_to_{endDate:yyyyMMdd}.pdf");
        }


        //download weekly
        [HttpGet("DownloadWeeklySales/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<WeeklySalesReportDTO>>> GetWeeklySalesReportPDF(Guid id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Step 2: Calculate start and end dates for the current week
            var currentDate = DateTime.Now.Date; // Today's date
            var startOfWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek); // Start of the week (Sunday)
            var endOfWeek = startOfWeek.AddDays(7); // End of the week (Saturday midnight)

            // Step 3: Fetch bookings and services for the week
            try
            {
                var bookings = await context.BookingLogs
                    .Where(b => b.BookingDate >= startOfWeek
                                && b.BookingDate < endOfWeek // Ensure bookings fall within the week
                                && !b.IsCanceled
                                && b.LaundryServiceLog.LaundryShop.LaundryShopId == id) // Filter by laundry shop ID
                    .Select(b => new
                    {
                        b.BookingDate,
                        b.LaundryServiceLog.ServiceIds,
                        b.TotalPrice
                    })
                    .ToListAsync(); // Fetch all necessary data

                if (bookings == null || !bookings.Any())
                {
                    return NotFound("No bookings found for the specified week.");
                }

                // Step 4: Map service names and aggregate weekly sales data
                var salesReportDTO = bookings
                .SelectMany(b => b.ServiceIds?.Select(serviceId => new
                {
                    ServiceName = context.Services
                        .Where(service => service.ServiceId == serviceId)
                        .Select(service => service.ServiceName)
                        .FirstOrDefault() ?? "Unknown Service", // Resolve service name or fallback
                    b.BookingDate,
                    b.TotalPrice
                }) ?? Enumerable.Empty<dynamic>())
                .GroupBy(entry => entry.ServiceName) // Group by service name
                .Select(g => new WeeklySalesReportDTO
                {
                    ServiceName = g.Key,
                    WeekStartDate = startOfWeek,
                    WeekEndDate = endOfWeek,
                    NumberOfOrders = g.Count(), // Count remains as an integer
                    AverageOrderValue = g.Any()
                        ? (double)g.Average(entry => (decimal)(entry.TotalPrice ?? 0)) // Convert average to double
                        : 0.0, // Default to 0.0 if no bookings
                    TotalSalesAmount = g.Sum(entry => (double)(decimal)(entry.TotalPrice ?? 0)) // Convert total to double
                })
                .ToList();

                if (salesReportDTO == null || !salesReportDTO.Any())
                {
                    return NotFound("No sales data found for this week.");
                }

                return Ok(salesReportDTO);
            }
            catch (Exception ex)
            {
                // Log the error to help with troubleshooting
                logger.LogError($"Error fetching weekly sales report: {ex.Message}", ex);
                return StatusCode(500, "Internal server error.");
            }
        }


        //download monthly
        [HttpGet("DownloadMonthlySales/{id}")]
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

