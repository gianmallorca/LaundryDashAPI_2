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
                    TotalRevenue = salesReport.Sum(b => b.TotalPrice ?? 0) // Total revenue for all bookings on the day
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
                        TotalSalesAmount = (double)g.Sum(entry => entry.TotalPrice.HasValue ? entry.TotalPrice.Value : 0),
                        TotalRevenue = (double)bookings.Sum(b => b.TotalPrice ?? 0) // Sum for all bookings to get total revenue
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
                        TotalSalesAmount = (double)g.Sum(entry => entry.TotalPrice.HasValue ? entry.TotalPrice.Value : 0),
                        TotalRevenue = (double)bookings.Sum(b => b.TotalPrice ?? 0) // Sum for all bookings to get total revenue
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

            // Today's date
            var currentDate = DateTime.Now.Date;

            // Fetch sales report
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

            // Create a PDF document
            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Set up font and layout
            var font = new XFont("Arial", 8); // Smaller font size for better fit
            var titleFont = new XFont("Arial", 12, XFontStyle.Bold);
            var headerFont = new XFont("Arial", 9, XFontStyle.Bold);
            var margin = 20; // Left and right margins
            var currentY = margin;

            // Title of the report - Centered
            var titleWidth = gfx.MeasureString("Daily Sales Report", titleFont).Width;
            var titleX = (page.Width - titleWidth) / 2;
            gfx.DrawString("Daily Sales Report", titleFont, XBrushes.Black, titleX, currentY);
            currentY += 30;

            // Table header - Centered
            var tableWidth = 450; // Total width of the table
            var startX = (page.Width - tableWidth) / 2; // Start X position for centering the table
            var columnOffsets = new[] { 0, 80, 200, 300, 400 }; // Offsets for each column within the table

            gfx.DrawString("Date", headerFont, XBrushes.Black, startX + columnOffsets[0], currentY);
            gfx.DrawString("Service", headerFont, XBrushes.Black, startX + columnOffsets[1], currentY);
            gfx.DrawString("Orders", headerFont, XBrushes.Black, startX + columnOffsets[2], currentY);
            gfx.DrawString("Avg Order Value", headerFont, XBrushes.Black, startX + columnOffsets[3], currentY);
            gfx.DrawString("Total Sales", headerFont, XBrushes.Black, startX + columnOffsets[4], currentY);
            currentY += 20;

            // Aggregate sales data
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

            // Calculate total revenue
            var totalRevenue = salesReportDTO.Sum(item => item.TotalSalesAmount);

            // Add data rows to the PDF
            foreach (var item in salesReportDTO)
            {
                if (currentY > page.Height - margin * 2) // Check for page overflow
                {
                    page = document.AddPage(); // Add new page
                    gfx = XGraphics.FromPdfPage(page);
                    currentY = margin; // Reset currentY for the new page
                }

                gfx.DrawString(currentDate.ToString("MM/dd/yyyy"), font, XBrushes.Black, startX + columnOffsets[0], currentY);
                gfx.DrawString(item.ServiceName, font, XBrushes.Black, startX + columnOffsets[1], currentY);
                gfx.DrawString(item.NumberOfOrders.ToString(), font, XBrushes.Black, startX + columnOffsets[2], currentY);
                gfx.DrawString(item.AverageOrderValue.ToString("C"), font, XBrushes.Black, startX + columnOffsets[3], currentY);
                gfx.DrawString(item.TotalSalesAmount.ToString("C"), font, XBrushes.Black, startX + columnOffsets[4], currentY);
                currentY += 15; // Adjusted row height
            }

            // Add space before Total Revenue
            currentY += 20;
            gfx.DrawString("Total Revenue", headerFont, XBrushes.Black, startX + columnOffsets[3], currentY);
            gfx.DrawString(totalRevenue.ToString("C"), headerFont, XBrushes.Black, startX + columnOffsets[4], currentY);

            // Save and return PDF
            using (var ms = new MemoryStream())
            {
                document.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms.ToArray(), "application/pdf", "DailySalesReport.pdf");
            }
        }


        [HttpGet("DownloadWeeklySalesPDF/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<IActionResult> DownloadWeeklySalesReportPDF(Guid id)
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

            // Get the date range for the past 7 days (current week)
            var currentDate = DateTime.Now.Date;
            var startOfWeek = currentDate.AddDays(-((int)currentDate.DayOfWeek)); // Start of the current week (Sunday)
            var endOfWeek = startOfWeek.AddDays(7); // End of the current week (Saturday)

            // Fetch sales report for the last 7 days
            var salesReport = await context.BookingLogs
                .Where(b => b.BookingDate >= startOfWeek
                            && b.BookingDate < endOfWeek
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
                return NotFound("No sales data found for this week.");
            }

            // Create a PDF document
            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Set up font and layout
            var font = new XFont("Arial", 8); // Smaller font size for better fit
            var titleFont = new XFont("Arial", 12, XFontStyle.Bold);
            var headerFont = new XFont("Arial", 9, XFontStyle.Bold);
            var margin = 20; // Left and right margins
            var currentY = margin;

            // Title of the report - Centered
            var titleWidth = gfx.MeasureString("Weekly Sales Report", titleFont).Width;
            var titleX = (page.Width - titleWidth) / 2;
            gfx.DrawString("Weekly Sales Report", titleFont, XBrushes.Black, titleX, currentY);
            currentY += 30;

            // Add date header below the title
            var dateHeader = $"{startOfWeek.ToString("MM/dd/yyyy")} - {endOfWeek.ToString("MM/dd/yyyy")}";
            var dateHeaderWidth = gfx.MeasureString(dateHeader, headerFont).Width;
            var dateHeaderX = (page.Width - dateHeaderWidth) / 2;
            gfx.DrawString(dateHeader, headerFont, XBrushes.Black, dateHeaderX, currentY);
            currentY += 20; // Add space after the date header

            // Table header - Centered
            var tableWidth = 450; // Total width of the table
            var startX = (page.Width - tableWidth) / 2; // Start X position for centering the table
            var columnOffsets = new[] { 0, 80, 200, 300, 400 }; // Offsets for each column within the table

            // Center alignment for the header
            var centerFormat = new XStringFormat { Alignment = XStringAlignment.Center };

            // Draw Table Header - Centered
            gfx.DrawString("Service", headerFont, XBrushes.Black, startX + columnOffsets[1], currentY, centerFormat);
            gfx.DrawString("Orders", headerFont, XBrushes.Black, startX + columnOffsets[2], currentY, centerFormat);
            gfx.DrawString("Avg Order Value", headerFont, XBrushes.Black, startX + columnOffsets[3], currentY, centerFormat);
            gfx.DrawString("Total Sales", headerFont, XBrushes.Black, startX + columnOffsets[4], currentY, centerFormat);
            currentY += 20;

            // Aggregate sales data
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

            // Calculate total revenue
            var totalRevenue = salesReportDTO.Sum(item => item.TotalSalesAmount);

            // Add data rows to the PDF (Zebra table effect)
            bool isAlternateRow = false; // Variable to alternate row colors

            foreach (var item in salesReportDTO)
            {
                if (currentY > page.Height - margin * 2) // Check for page overflow
                {
                    page = document.AddPage(); // Add new page
                    gfx = XGraphics.FromPdfPage(page);
                    currentY = margin; // Reset currentY for the new page
                }

                // Alternate row color (zebra table effect)
                var rowColor = isAlternateRow ? XBrushes.LightGray : XBrushes.White;

                // Draw row with alternating colors
                gfx.DrawRectangle(rowColor, startX, currentY, tableWidth, 15); // Draw the row with background color

                // Draw centered data
                gfx.DrawString(item.ServiceName, font, XBrushes.Black, startX + columnOffsets[1], currentY, centerFormat);
                gfx.DrawString(item.NumberOfOrders.ToString(), font, XBrushes.Black, startX + columnOffsets[2], currentY, centerFormat);
                gfx.DrawString(item.AverageOrderValue.ToString("C"), font, XBrushes.Black, startX + columnOffsets[3], currentY, centerFormat);
                gfx.DrawString(item.TotalSalesAmount.ToString("C"), font, XBrushes.Black, startX + columnOffsets[4], currentY, centerFormat);

                currentY += 15; // Adjusted row height
                isAlternateRow = !isAlternateRow; // Toggle row color
            }

            // Add space before Total Revenue
            currentY += 20;
            gfx.DrawString("Total Revenue", headerFont, XBrushes.Black, startX + columnOffsets[3], currentY, centerFormat);
            gfx.DrawString(totalRevenue.ToString("C"), headerFont, XBrushes.Black, startX + columnOffsets[4], currentY, centerFormat);

            // Save and return PDF
            using (var ms = new MemoryStream())
            {
                document.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms.ToArray(), "application/pdf", "WeeklySalesReport.pdf");
            }
        }


        [HttpGet("DownloadMonthlySalesPDF/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<IActionResult> DownloadMonthlySalesReportPDF(Guid id)
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

            // Get the first day of the current month and the last day of the current month
            var currentDate = DateTime.Now;
            var startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Fetch sales report for the current month
            var salesReport = await context.BookingLogs
                .Where(b => b.BookingDate >= startOfMonth
                            && b.BookingDate <= endOfMonth
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
                return NotFound("No sales data found for the month.");
            }

            // Create a PDF document
            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Set up font and layout
            var font = new XFont("Arial", 8); // Smaller font size for better fit
            var titleFont = new XFont("Arial", 12, XFontStyle.Bold);
            var headerFont = new XFont("Arial", 9, XFontStyle.Bold);
            var margin = 20; // Left and right margins
            var currentY = margin;

            // Title of the report - Centered
            var titleWidth = gfx.MeasureString("Monthly Sales Report", titleFont).Width;
            var titleX = (page.Width - titleWidth) / 2;
            gfx.DrawString("Monthly Sales Report", titleFont, XBrushes.Black, titleX, currentY);
            currentY += 30;

            // Date of the report - Centered
            var dateText = $"For the month of {currentDate.ToString("MMMM yyyy")}";
            var dateWidth = gfx.MeasureString(dateText, headerFont).Width;
            var dateX = (page.Width - dateWidth) / 2;
            gfx.DrawString(dateText, headerFont, XBrushes.Black, dateX, currentY);
            currentY += 30;

            // Table header - Centered
            var tableWidth = 450; // Total width of the table
            var startX = (page.Width - tableWidth) / 2; // Start X position for centering the table
            var columnWidth = tableWidth / 4; // Width for each column, assuming 4 columns

            // Draw column headers and center text
            gfx.DrawString("Service", headerFont, XBrushes.Black, startX + columnWidth / 2 - gfx.MeasureString("Service", headerFont).Width / 2, currentY);
            gfx.DrawString("Orders", headerFont, XBrushes.Black, startX + columnWidth + columnWidth / 2 - gfx.MeasureString("Orders", headerFont).Width / 2, currentY);
            gfx.DrawString("Avg Order Value", headerFont, XBrushes.Black, startX + 2 * columnWidth + columnWidth / 2 - gfx.MeasureString("Avg Order Value", headerFont).Width / 2, currentY);
            gfx.DrawString("Total Sales", headerFont, XBrushes.Black, startX + 3 * columnWidth + columnWidth / 2 - gfx.MeasureString("Total Sales", headerFont).Width / 2, currentY);
            currentY += 20;

            // Aggregate sales data for the current month
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

            // Calculate total revenue for the month
            var totalRevenue = salesReportDTO.Sum(item => item.TotalSalesAmount);

            // Add data rows to the PDF
            foreach (var item in salesReportDTO)
            {
                if (currentY > page.Height - margin * 2) // Check for page overflow
                {
                    page = document.AddPage(); // Add new page
                    gfx = XGraphics.FromPdfPage(page);
                    currentY = margin; // Reset currentY for the new page
                }

                // Center data in columns
                gfx.DrawString(item.ServiceName, font, XBrushes.Black, startX + columnWidth / 2 - gfx.MeasureString(item.ServiceName, font).Width / 2, currentY);
                gfx.DrawString(item.NumberOfOrders.ToString(), font, XBrushes.Black, startX + columnWidth + columnWidth / 2 - gfx.MeasureString(item.NumberOfOrders.ToString(), font).Width / 2, currentY);
                gfx.DrawString(item.AverageOrderValue.ToString("C"), font, XBrushes.Black, startX + 2 * columnWidth + columnWidth / 2 - gfx.MeasureString(item.AverageOrderValue.ToString("C"), font).Width / 2, currentY);
                gfx.DrawString(item.TotalSalesAmount.ToString("C"), font, XBrushes.Black, startX + 3 * columnWidth + columnWidth / 2 - gfx.MeasureString(item.TotalSalesAmount.ToString("C"), font).Width / 2, currentY);
                currentY += 15; // Adjusted row height
            }

            // Add space before Total Revenue
            currentY += 20;
            gfx.DrawString("Total Revenue", headerFont, XBrushes.Black, startX + 2 * columnWidth + columnWidth / 2 - gfx.MeasureString("Total Revenue", headerFont).Width / 2, currentY);
            gfx.DrawString(totalRevenue.ToString("C"), headerFont, XBrushes.Black, startX + 3 * columnWidth + columnWidth / 2 - gfx.MeasureString(totalRevenue.ToString("C"), headerFont).Width / 2, currentY);

            // Save and return PDF
            using (var ms = new MemoryStream())
            {
                document.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms.ToArray(), "application/pdf", "MonthlySalesReport.pdf");
            }
        }









    }
}

