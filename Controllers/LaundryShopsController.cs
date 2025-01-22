using AutoMapper;

using LaundryDashAPI_2;
using LaundryDashAPI_2.DTOs;
using LaundryDashAPI_2.DTOs.LaundryServiceLog;
using LaundryDashAPI_2.DTOs.LaundryShop;
using LaundryDashAPI_2.Entities;
using LaundryDashAPI_2.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LaundryDashAPI_2.Controllers
{

    [Route("api/laundryShops")]
    [ApiController]

    public class LaundryShopsController : Controller
    {
        private readonly ILogger<LaundryShopsController> logger;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IFileStorageService fileStorageService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly string containerName = "LaundryShopImages";

        public LaundryShopsController(ILogger<LaundryShopsController> logger, ApplicationDbContext context, IMapper mapper, IFileStorageService fileStorageService, UserManager<ApplicationUser> userManager)
        {
            this.logger = logger;
            this.context = context;
            this.mapper = mapper;
            this.fileStorageService = fileStorageService;
            this.userManager = userManager;
        }

       

        [HttpGet("getLaundryShop")]
        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccountOrClientAccount")]
        public async Task<ActionResult<List<LaundryShopDTO>>> Get([FromQuery] PaginationDTO paginationDTO)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }


            var queryable = context.LaundryShops.AsQueryable();
            queryable = queryable.Where(x => x.IsVerifiedByAdmin == true);
            await HttpContext.InsertParametersPaginationInHeader(queryable);

            var laundryShops = await queryable.OrderBy(x => x.LaundryShopName).Paginate(paginationDTO).ToListAsync();

            return mapper.Map<List<LaundryShopDTO>>(laundryShops);


        }


        //gets laundry shop picture


        [HttpGet("get-image-by-id/{id:Guid}")]
        public async Task<IActionResult> GetImageById(Guid id)
        {
            // Define the folder path where images are stored (without the "LaundryShopImages" folder part)
            var folderPath = @"C:\Users\ADMIN\Desktop\LaundryDash API New\LaundryShopImages";

            // Check if the folder exists
            if (!Directory.Exists(folderPath))
            {
                return NotFound("Image folder does not exist.");
            }

            // Retrieve the laundry shop from the database by the given ID
            var laundryShop = await context.LaundryShops
                .Where(shop => shop.LaundryShopId == id)
                .FirstOrDefaultAsync();

            // Check if the laundry shop exists
            if (laundryShop == null)
            {
                return NotFound("Laundry shop not found.");
            }

            // Get the image file name from the LaundryShopPicture property (this is where we get the path or filename)
            var imageFileName = laundryShop.LaundryShopPicture;

            // Ensure the image filename is not null or empty
            if (string.IsNullOrEmpty(imageFileName))
            {
                return NotFound("No image associated with this laundry shop.");
            }

            // Build the full file path by combining the folder path and image file name (path is from the database here)
            // Ensure you only append the file name, not the entire folder part
            var filePath = Path.Combine(folderPath, imageFileName);

            // Check if the file exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Image file not found.");
            }

            // Read the image file bytes
            var imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // Use FileExtensionContentTypeProvider to get the MIME type based on the file extension
            var provider = new FileExtensionContentTypeProvider();

            // Get the file extension from the image file
            var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

            // Check if the MIME type is available for the file extension
            if (!provider.TryGetContentType(fileExtension, out var contentType))
            {
                contentType = "application/octet-stream"; // Default to binary if MIME type is not found
            }

            // Return the image as a file response with the detected MIME type
            return File(imageBytes, contentType);
        }





        /// <summary>
        /// ////////////////////////////////////////////////////
        /// </summary>
        /// <returns></returns>


        [HttpGet("getLaundryShopByUserId")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<LaundryShopDTO>>> GetByUserAccountId()
        {
            // Get the user's email from the claims
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Find the user by email
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Query the laundry shops added by the user, filtering only by AddedById
            var laundryShops = await context.LaundryShops
            .Where(x => x.AddedById == user.Id && x.IsVerifiedByAdmin == true)
            .ToListAsync();

            if (laundryShops == null || !laundryShops.Any())
            {
                return NotFound("No laundry shops found.");
            }

            // Return the mapped LaundryShopDTO list
            return Ok(mapper.Map<List<LaundryShopDTO>>(laundryShops));
        }





        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult> Post([FromForm] LaundryShopCreationDTO laundryShopCreationDTO)
        {
            if (laundryShopCreationDTO == null)
            {
                return BadRequest("Request body cannot be null.");
            }

            var laundryShop = mapper.Map<Entities.LaundryShop>(laundryShopCreationDTO);
            laundryShop.IsVerifiedByAdmin = false;

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

            laundryShop.AddedById = user.Id;


            if (laundryShopCreationDTO.LaundryShopPicture != null)
            {
                laundryShop.LaundryShopPicture = await fileStorageService.SaveFile(containerName, laundryShopCreationDTO.LaundryShopPicture);
            }

            if (laundryShopCreationDTO.BusinessPermitsPDF != null)
            {
                laundryShop.BusinessPermitsPDF = await fileStorageService.SaveFile(containerName, laundryShopCreationDTO.BusinessPermitsPDF);
            }


            context.LaundryShops.Add(laundryShop);
            await context.SaveChangesAsync();

            return NoContent();
        }

        //get to populate component
        [HttpGet("getLaundryShopDetailsForEditById/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<LaundryShopDTO>> getLaundryShopDetailsForEditById([FromRoute] Guid id)
        {
            // Retrieve the laundry shop by ID
            var laundryShop = await context.LaundryShops.FirstOrDefaultAsync(shop => shop.LaundryShopId == id);

            if (laundryShop == null)
            {
                return NotFound("Laundry shop not found.");
            }

            // Map the laundry shop entity to LaundryShopDTO
            var shopDetails = new LaundryShopDTO
            {
                LaundryShopId = laundryShop.LaundryShopId,
                LaundryShopName = laundryShop.LaundryShopName,

                City = laundryShop.City,
                Barangay = laundryShop.Barangay,
                BrgyStreet = laundryShop.BrgyStreet,
                ContactNum = laundryShop.ContactNum,
                TimeOpen = laundryShop.TimeOpen,
                TimeClose = laundryShop.TimeClose,
                Monday = laundryShop.Monday,
                Tuesday = laundryShop.Tuesday,
                Wednesday = laundryShop.Wednesday,
                Thursday = laundryShop.Thursday,
                Friday = laundryShop.Friday,
                Saturday = laundryShop.Saturday,
                Sunday = laundryShop.Sunday,

                //BusinessPermitId = laundryShop.BusinessPermitId,
                //DTIPermitId = laundryShop.DTIPermitId,
                //TaxIdentificationNumber = laundryShop.TaxIdentificationNumber,
                //EnvironmentalPermit = laundryShop.EnvironmentalPermit,
                //SanitaryPermit = laundryShop.SanitaryPermit,
                LaundryShopPicture = laundryShop.LaundryShopPicture
            };

            return Ok(shopDetails);
        }


        //edit shop
        [HttpPut("EditShopDetails/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult> EditShopDetails(Guid id, [FromForm] LaundryShopUpdateDTO laundryShopUpdateDTO)
        {
            if (laundryShopUpdateDTO == null)
            {
                return BadRequest("Request body cannot be null.");
            }

            var laundryShop = await context.LaundryShops.FirstOrDefaultAsync(ls => ls.LaundryShopId == id);
            if (laundryShop == null)
            {
                return NotFound("Laundry shop not found.");
            }

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

            // Map the updated properties from the DTO to the existing entity, excluding the picture
            mapper.Map(laundryShopUpdateDTO, laundryShop);

            laundryShop.AddedById = user.Id;

            // Handle laundry shop picture update only if a new picture is provided
            if (laundryShopUpdateDTO.LaundryShopPicture != null)
            {
                laundryShop.LaundryShopPicture = await fileStorageService.SaveFile(containerName, laundryShopUpdateDTO.LaundryShopPicture);
            }

            // Save changes to the database
            await context.SaveChangesAsync();

            return NoContent();
        }



        [HttpGet("getPendingLaundryShops")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
        public async Task<ActionResult<List<LaundryShopDTO>>> GetPendingLaundryShops([FromQuery] PaginationDTO paginationDTO)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var queryable = context.LaundryShops
                .Where(x => x.IsVerifiedByAdmin == false) // Only retrieve shops where IsApprovedByAdmin is false
                .AsQueryable();

            await HttpContext.InsertParametersPaginationInHeader(queryable);

            var laundryShops = await queryable
                .OrderBy(x => x.LaundryShopName) // You can modify sorting based on your needs
                .Paginate(paginationDTO)
                .ToListAsync();

            return mapper.Map<List<LaundryShopDTO>>(laundryShops);
        }


        [HttpPut("approveLaundryShop/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
        public async Task<ActionResult> ApproveLaundryShop(Guid id)
        {
            // Retrieve the LaundryShop based on the given id
            var laundryShop = await context.LaundryShops.FirstOrDefaultAsync(x => x.LaundryShopId == id);

            // Check if the LaundryShop exists
            if (laundryShop == null)
            {
                return NotFound("Laundry Shop not found.");
            }

            // Update the IsApprovedByAdmin property to true
            laundryShop.IsVerifiedByAdmin = true;

            // Save the changes to the database
            await context.SaveChangesAsync();

            // Return NoContent (successful update)
            return NoContent();
        }



        //[HttpPut("{id:Guid}", Name = "editLaundryShop")]

        //public async Task<ActionResult> EditLaund(Guid id, [FromBody] LaundryShopCreationDTO laundryShopCreationDTO)
        //{
        //    var laundryShop = await context.LaundryShops.FirstOrDefaultAsync(x => x.LaundryShopId == id);
        //    if (laundryShop == null)
        //    {
        //        return NotFound();
        //    }

        //    laundryShop = mapper.Map(laundryShopCreationDTO, laundryShop);
        //    await context.SaveChangesAsync();

        //    return NoContent();
        //}

        [HttpDelete("{id:Guid}", Name = "deleteLaundryShop")]

        public async Task<ActionResult> Delete(Guid id)
        {
            var exists = await context.LaundryShops.AnyAsync(x => x.LaundryShopId == id);

            if (!exists)
            {
                return NotFound();
            }

            context.Remove(new LaundryShop() { LaundryShopId = id });
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("PostGet")]
        [AllowAnonymous]

        public async Task<ActionResult<List<LaundryShopDTO>>> GetLaundryPostGet()
        {
            var laundryShops = await context.LaundryShops.ToListAsync();

            return mapper.Map<List<LaundryShopDTO>>(laundryShops);
        }

        [HttpGet("PostGetTwo")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<List<LaundryShopDTO>>> LaundryPostGet()
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

            // Check if the user is an admin
            var isAdmin = await userManager.IsInRoleAsync(user, "Admin");

            // If the user is an admin, return all laundry shops
            if (isAdmin)
            {
                var allLaundryShops = await context.LaundryShops.ToListAsync();
                return mapper.Map<List<LaundryShopDTO>>(allLaundryShops);
            }

            // If not an admin, return only the laundry shops added by the current user
            var laundryShops = await context.LaundryShops
            .Where(x => x.AddedById == user.Id && x.IsVerifiedByAdmin == true)
            .ToListAsync();

            return mapper.Map<List<LaundryShopDTO>>(laundryShops);
        }

        [HttpGet("getLaundryDetailsById/{id:Guid}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccountOrClientAccount")]
        public async Task<ActionResult<LaundryShopDTO>> GetLaundryDetailsById(Guid id)
        {
            // Retrieve the user's email from the claims
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Ensure the email claim exists
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Find the user by email
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Fetch the laundry shop with the matching ID and ensure it is verified by the admin
            var laundryShop = await context.LaundryShops
                .FirstOrDefaultAsync(x => x.LaundryShopId == id && x.IsVerifiedByAdmin);

            if (laundryShop == null)
            {
                return NotFound("Laundry shop not found or not verified by the admin.");
            }

            // Map and return the laundry shop details as a DTO
            return Ok(mapper.Map<LaundryShopDTO>(laundryShop));
        }


        //test if working, get weekly sales laundry shop dashboard
        [HttpGet("weekly-sales-by-shop")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<Dictionary<string, object>>> GetWeeklySalesByShop()
        {
            // Initialize a dictionary to hold sales grouped by laundry shop name and day of the week
            var salesByShop = new Dictionary<string, object>();

            // Calculate the start and end of the current week
            var startOfWeek = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek + 1); // Start on Monday
            var endOfWeek = startOfWeek.AddDays(7); // End on Sunday

            // Get all bookings in the current week
            var bookings = await context.BookingLogs
                .Include(b => b.LaundryServiceLog.LaundryShop) // Include related laundry shop
                .Where(b => b.BookingDate >= startOfWeek && b.BookingDate < endOfWeek)
                .ToListAsync();

            // Iterate through each booking to group sales
            foreach (var booking in bookings)
            {
                var shopName = booking.LaundryServiceLog.LaundryShop.LaundryShopName;
                var shopId = booking.LaundryServiceLog.LaundryShop.LaundryShopId; // Get the shop ID
                var dayOfWeek = booking.BookingDate.DayOfWeek.ToString().Substring(0, 3); // Get the first 3 letters (Mon, Tue, etc.)
                var totalPrice = booking.TotalPrice ?? 0; // Handle null values for total price

                // Ensure the shop is in the dictionary
                if (!salesByShop.ContainsKey(shopName))
                {
                    salesByShop[shopName] = new
                    {
                        shopId = shopId, // Add the shopId
                        sales = new Dictionary<string, decimal>
        {
            { "Mon", 0m },
            { "Tue", 0m },
            { "Wed", 0m },
            { "Thu", 0m },
            { "Fri", 0m },
            { "Sat", 0m },
            { "Sun", 0m }
        }
                    };
                }

                // Add the sales to the corresponding day
                var shopSales = (dynamic)salesByShop[shopName]; // Cast to dynamic to access shopId and sales data
                if (shopSales.sales.ContainsKey(dayOfWeek))
                {
                    shopSales.sales[dayOfWeek] += totalPrice;
                }
            }

            return Ok(salesByShop);
        }


        [HttpGet("GetActiveUsers")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<object>> GetActiveUsers()
        {
            // Define the start of the current and last month
            var currentDate = DateTime.UtcNow;
            var firstDayOfCurrentMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var firstDayOfLastMonth = firstDayOfCurrentMonth.AddMonths(-1);

            // Count completed bookings for the current month
            var completedBookingsCurrentMonth = await context.BookingLogs
                .Where(b => b.TransactionCompleted && b.BookingDate >= firstDayOfCurrentMonth)
                .CountAsync();

            // Count completed bookings for the last month
            var completedBookingsLastMonth = await context.BookingLogs
                .Where(b => b.TransactionCompleted && b.BookingDate >= firstDayOfLastMonth && b.BookingDate < firstDayOfCurrentMonth)
                .CountAsync();

            // Calculate the percentage change
            decimal percentageChange = 0;
            if (completedBookingsLastMonth > 0)
            {
                percentageChange = ((decimal)(completedBookingsCurrentMonth - completedBookingsLastMonth) / completedBookingsLastMonth) * 100;
            }

            return Ok(new
            {
                CompletedBookings = completedBookingsCurrentMonth,
                PercentageChange = Math.Round(percentageChange, 1) // Rounded to one decimal place
            });
        }


        [HttpGet("GetGrowthPercentage")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<object>> GetGrowthPercentage()
        {
            // Example: Replace this with your specific data source and logic
            var currentDate = DateTime.UtcNow;
            var firstDayOfCurrentMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var firstDayOfLastMonth = firstDayOfCurrentMonth.AddMonths(-1);

            // Replace with your actual logic for growth calculation
            var metricCurrentMonth = await context.BookingLogs
                .Where(b => b.BookingDate >= firstDayOfCurrentMonth)
                .CountAsync();

            var metricLastMonth = await context.BookingLogs
                .Where(b => b.BookingDate >= firstDayOfLastMonth && b.BookingDate < firstDayOfCurrentMonth)
                .CountAsync();

            // Calculate percentage growth
            decimal growthPercentage = 0;
            if (metricLastMonth > 0)
            {
                growthPercentage = ((decimal)(metricCurrentMonth - metricLastMonth) / metricLastMonth) * 100;
            }

            return Ok(new
            {
                Growth = Math.Round(growthPercentage, 1),
                MetricCurrentMonth = metricCurrentMonth,
                MetricLastMonth = metricLastMonth
            });
        }





    }
}