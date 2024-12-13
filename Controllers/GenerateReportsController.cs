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
                    TotalSalesAmount = g.Sum(b => b.TotalPrice ?? 0), // Total sales for the service
                    TotalRevenue = salesReport.Sum(b => b.TotalPrice ?? 0) // Total revenue for all sales for the day
                })
                .ToList();

            if (salesReportDTO == null || !salesReportDTO.Any())
            {
                return NotFound("No sales data found for today.");
            }

            return Ok(salesReportDTO);
        }



        //get weekly sales report
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
                        b.BookingLogId, // Add BookingLogId here
                        b.BookingDate,
                        b.LaundryServiceLog.ServiceIds, // Get related services
                        b.TotalPrice
                    })
                    .ToListAsync(); // Fetch all necessary data

                if (bookings == null || !bookings.Any())
                {
                    return NotFound("No bookings found for the specified week.");
                }

                // Step 4: Fetch the service names for the serviceIds (after retrieving the bookings)
                var allServiceIds = bookings.SelectMany(b => b.ServiceIds).Distinct().ToList();
                var serviceNames = await context.Services
                    .Where(s => allServiceIds.Contains(s.ServiceId))
                    .Select(s => new { s.ServiceId, s.ServiceName })
                    .ToDictionaryAsync(s => s.ServiceId, s => s.ServiceName);

                // Step 5: Group bookings by service name and aggregate weekly sales data
                var groupedReports = bookings
                    .SelectMany(b => b.ServiceIds.Select(serviceId => new
                    {
                        ServiceName = serviceNames.ContainsKey(serviceId) ? serviceNames[serviceId] : "Unknown Service",
                        b.TotalPrice
                    }))
                    .GroupBy(g => g.ServiceName)
                    .Select(g => new WeeklySalesReportDTO
                    {
                        ServiceName = g.Key,
                        WeekStartDate = startOfWeek,
                        WeekEndDate = endOfWeek,
                        NumberOfOrders = g.Count(),
                        AverageOrderValue = g.Average(entry => entry.TotalPrice.HasValue ? (double)entry.TotalPrice : 0),
                        TotalSalesAmount = (double)g.Sum(entry => entry.TotalPrice.HasValue ? entry.TotalPrice.Value : 0)
                    })
                    .ToList();

                if (groupedReports == null || !groupedReports.Any())
                {
                    return NotFound("No sales data found for this week.");
                }

                return Ok(groupedReports);
            }
            catch (Exception ex)
            {
                // Log the error to help with troubleshooting
                logger.LogError($"Error fetching weekly sales report: {ex.Message}", ex);
                return StatusCode(500, "Internal server error.");
            }
        }


        [HttpGet("GenerateMonthlySales/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<WeeklySalesReportDTO>>> GenerateMonthlySalesReportAsync(Guid id)
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
            var endOfWeek = startOfWeek.AddDays(30); // End of the week (Saturday midnight)

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
                        b.BookingLogId, // Add BookingLogId here
                        b.BookingDate,
                        b.LaundryServiceLog.ServiceIds, // Get related services
                        b.TotalPrice
                    })
                    .ToListAsync(); // Fetch all necessary data

                if (bookings == null || !bookings.Any())
                {
                    return NotFound("No bookings found for the specified week.");
                }

                // Step 4: Fetch the service names for the serviceIds (after retrieving the bookings)
                var allServiceIds = bookings.SelectMany(b => b.ServiceIds).Distinct().ToList();
                var serviceNames = await context.Services
                    .Where(s => allServiceIds.Contains(s.ServiceId))
                    .Select(s => new { s.ServiceId, s.ServiceName })
                    .ToDictionaryAsync(s => s.ServiceId, s => s.ServiceName);

                // Step 5: Group bookings by service name and aggregate weekly sales data
                var groupedReports = bookings
                    .SelectMany(b => b.ServiceIds.Select(serviceId => new
                    {
                        ServiceName = serviceNames.ContainsKey(serviceId) ? serviceNames[serviceId] : "Unknown Service",
                        b.TotalPrice
                    }))
                    .GroupBy(g => g.ServiceName)
                    .Select(g => new WeeklySalesReportDTO
                    {
                        ServiceName = g.Key,
                        WeekStartDate = startOfWeek,
                        WeekEndDate = endOfWeek,
                        NumberOfOrders = g.Count(),
                        AverageOrderValue = g.Average(entry => entry.TotalPrice.HasValue ? (double)entry.TotalPrice : 0),
                        TotalSalesAmount = (double)g.Sum(entry => entry.TotalPrice.HasValue ? entry.TotalPrice.Value : 0)
                    })
                    .ToList();

                if (groupedReports == null || !groupedReports.Any())
                {
                    return NotFound("No sales data found for this week.");
                }

                return Ok(groupedReports);
            }
            catch (Exception ex)
            {
                // Log the error to help with troubleshooting
                logger.LogError($"Error fetching weekly sales report: {ex.Message}", ex);
                return StatusCode(500, "Internal server error.");
            }
        }









        //download daily
        [HttpGet("DownloadDailySalesPDF/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<IActionResult> DownloadDailySalesReportPDF(Guid id)
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

            // Step 2: Set today's date as startDate and endDate
            var currentDate = DateTime.Now.Date; // Get the current date at midnight (removes time component)

            // Step 3: Query to fetch today's sales report
            var salesReport = await context.BookingLogs
                .Where(b => b.BookingDate >= currentDate
                            && b.BookingDate < currentDate.AddDays(1)
                            && !b.IsCanceled
                            && b.LaundryServiceLog.LaundryShop.LaundryShopId == id)
                .Select(b => new
                {
                    b.BookingDate,
                    b.LaundryServiceLog.ServiceIds,
                    b.TotalPrice
                })
                .ToListAsync();

            if (salesReport == null || !salesReport.Any())
            {
                return NotFound("No sales data found for today.");
            }

            // Step 4: Create a PDF document
            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Step 5: Set up font and layout for the report
            var font = new XFont("Arial", 12);
            var titleFont = new XFont("Arial", 16, XFontStyle.Bold);
            var currentY = 40;

            // Title of the report
            gfx.DrawString("Daily Sales Report", titleFont, XBrushes.Black, new XRect(0, currentY, page.Width, page.Height), XStringFormats.TopCenter);
            currentY += 40;

            // Table header
            gfx.DrawString("Service Name", font, XBrushes.Black, 40, currentY);
            gfx.DrawString("Number of Orders", font, XBrushes.Black, 200, currentY);
            gfx.DrawString("Total Sales Amount", font, XBrushes.Black, 400, currentY);
            gfx.DrawString("Average Order Value", font, XBrushes.Black, 550, currentY);
            currentY += 30;

            // Step 6: Aggregate and write the data to PDF
            var salesReportDTO = salesReport
                .GroupBy(b => new
                {
                    ServiceName = context.Services
                        .Where(service => b.ServiceIds != null && service.ServiceId == b.ServiceIds.FirstOrDefault())
                        .Select(service => service.ServiceName)
                        .FirstOrDefault() ?? "Unknown Service"
                })
                .Select(g => new
                {
                    ServiceName = g.Key.ServiceName,
                    NumberOfOrders = g.Count(),
                    TotalSalesAmount = g.Sum(b => b.TotalPrice ?? 0),
                    AverageOrderValue = g.Average(b => b.TotalPrice ?? 0)
                })
                .ToList();

            // Add data to the PDF
            foreach (var item in salesReportDTO)
            {
                gfx.DrawString(item.ServiceName, font, XBrushes.Black, 40, currentY);
                gfx.DrawString(item.NumberOfOrders.ToString(), font, XBrushes.Black, 200, currentY);
                gfx.DrawString(item.TotalSalesAmount.ToString("C"), font, XBrushes.Black, 400, currentY);
                gfx.DrawString(item.AverageOrderValue.ToString("C"), font, XBrushes.Black, 550, currentY);
                currentY += 20;
            }

            // Step 7: Save the PDF to a memory stream
            using (var ms = new MemoryStream())
            {
                document.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);

                // Step 8: Return the PDF as a downloadable file
                return File(ms.ToArray(), "application/pdf", "DailySalesReport.pdf");
            }
        }

        // Similar method for weekly and monthly sales reports can be created...
    }



    
}

