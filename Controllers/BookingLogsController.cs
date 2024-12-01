using AutoMapper;
using LaundryDashAPI_2;
using LaundryDashAPI_2.DTOs;
using LaundryDashAPI_2.DTOs.BookingLog;
using LaundryDashAPI_2.DTOs.LaundryServiceLog;
using LaundryDashAPI_2.DTOs.LaundryShop;
using LaundryDashAPI_2.Entities;
using LaundryDashAPI_2.Helpers;
using LaundryDashAPI_2.Migrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace LaundryDashAPI_2.Controllers
{
    //[Authorize]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/bookingLogs")]
    [ApiController]
    // [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
    public class BookingLogsController : Controller
    {
        private readonly ILogger<BookingLogsController> logger;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly UserManager<ApplicationUser> userManager;

        public BookingLogsController(ILogger<BookingLogsController> logger, ApplicationDbContext context, IMapper mapper, UserManager<ApplicationUser> userManager)
        {
            this.logger = logger;
            this.context = context;
            this.mapper = mapper;
            this.userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<List<BookingLogDTO>>> Get([FromQuery] PaginationDTO paginationDTO)
        {
            var queryable = context.BookingLogs.AsQueryable();
            await HttpContext.InsertParametersPaginationInHeader(queryable);

            var bookingLogs = await queryable.OrderBy(x => x.BookingLogId).Paginate(paginationDTO).ToListAsync();

            return mapper.Map<List<BookingLogDTO>>(bookingLogs);
        }


        [HttpPost("create")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsClientAccount")]
        public async Task<ActionResult> Post([FromBody] BookingLogCreationDTO bookingLogCreationDTO)
        {
            if (bookingLogCreationDTO == null)
            {
                return BadRequest("Request body cannot be null.");
            }

            // Get the email claim from the current user
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Find the user by email
            var user = await userManager.FindByEmailAsync(email);

            // Check if the user was found
            if (user == null)
            {
                return NotFound("User not found.");
            }


            var bookingLog = new BookingLog
            {
                BookingLogId = Guid.NewGuid(), // Generate new BookingLogId
                LaundryServiceLogId = bookingLogCreationDTO.LaundryServiceLogId, // Set the associated LaundryServiceLogId
                BookingDate = DateTime.UtcNow, // Automatically set the BookingDate to the current UTC time
                PickupAddress = bookingLogCreationDTO.PickupAddress,
                DeliveryAddress = bookingLogCreationDTO.DeliveryAddress,
                Note = bookingLogCreationDTO.Note,
                ClientId = user.Id, // Set the current user as the ClientId

            };



            // Add the new booking log to the context
            context.BookingLogs.Add(bookingLog);

            // Save changes to the database
            await context.SaveChangesAsync();

            // Return a NoContent response (status code 204)
            return NoContent();
        }



        [HttpGet("getPendingBookings")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<BookingLogDTO>>> GetPendingBookings()
        {
            // Step 1: Get the logged-in user's email
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

            // Query pending bookings where IsAcceptedByShop is false and AddedById matches the current user
            var pendingBookings = await context.BookingLogs
                .Include(booking => booking.LaundryServiceLog)
                .ThenInclude(log => log.LaundryShop) // Include LaundryShop for details
                .Where(booking =>
                    booking.IsAcceptedByShop == false && // Pending bookings
                    booking.LaundryServiceLog.AddedById == user.Id && booking.TransactionCompleted == false) // Match AddedById with logged-in user
                .Select(booking => new BookingLogDTO
                {
                    BookingLogId = booking.BookingLogId,
                    LaundryServiceLogId = booking.LaundryServiceLogId,
                    LaundryShopName = booking.LaundryServiceLog.LaundryShop.LaundryShopName,
                    ServiceName = context.Services
                        .Where(service =>
                            booking.LaundryServiceLog.ServiceIds != null &&
                            service.ServiceId == booking.LaundryServiceLog.ServiceIds.FirstOrDefault())
                        .Select(service => service.ServiceName)
                        .FirstOrDefault() ?? "Unknown Service", // Resolve service name or fallback
                    BookingDate = booking.BookingDate,
                    TotalPrice = booking.TotalPrice,
                    Weight = booking.Weight,
                    PickupAddress = booking.PickupAddress,
                    DeliveryAddress = booking.DeliveryAddress,
                    Note = booking.Note,
                    ClientName = context.Users
                        .Where(client => client.Id == booking.ClientId)
                        .Select(client => $"{client.FirstName} {client.LastName}")
                        .FirstOrDefault() ?? "Unknown Client" // Resolve client name or fallback
                })
                .OrderBy(booking => booking.BookingDate)
                .ToListAsync();

            return Ok(pendingBookings);
        }


        [HttpGet("getBookingById/{id:Guid}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<BookingLogDTO>> GetBookingById(Guid id)
        {
            // Step 1: Get the logged-in user's email
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

            // Query a specific booking by BookingLogId where IsAcceptedByShop is false and AddedById matches the current user
            var booking = await context.BookingLogs
                .Include(b => b.LaundryServiceLog)
                .ThenInclude(log => log.LaundryShop) // Include LaundryShop for details
                .Where(b =>
                    b.BookingLogId == id && // Match BookingLogId
                    b.IsAcceptedByShop == false && // Ensure it's pending
                    b.LaundryServiceLog.AddedById == user.Id) // Match AddedById with logged-in user
                .Select(b => new BookingLogDTO
                {
                    BookingLogId = b.BookingLogId,
                    LaundryServiceLogId = b.LaundryServiceLogId,
                    LaundryShopName = b.LaundryServiceLog.LaundryShop.LaundryShopName,
                    ServiceName = context.Services
                        .Where(service =>
                            b.LaundryServiceLog.ServiceIds != null &&
                            service.ServiceId == b.LaundryServiceLog.ServiceIds.FirstOrDefault())
                        .Select(service => service.ServiceName)
                        .FirstOrDefault() ?? "Unknown Service", // Resolve service name or fallback
                    BookingDate = b.BookingDate,
                    TotalPrice = b.TotalPrice,
                    Weight = b.Weight,
                    PickupAddress = b.PickupAddress,
                    DeliveryAddress = b.DeliveryAddress,
                    Note = b.Note,
                    ClientName = context.Users
                        .Where(client => client.Id == b.ClientId)
                        .Select(client => $"{client.FirstName} {client.LastName}")
                        .FirstOrDefault() ?? "Unknown Client" // Resolve client name or fallback
                })
                .FirstOrDefaultAsync();

            if (booking == null)
            {
                return NotFound("Booking not found or not authorized to view.");
            }

            return Ok(booking);
        }



        [HttpPut("accept-booking/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult> AcceptBooking(Guid id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var bookingLog = await context.BookingLogs
                .FirstOrDefaultAsync(x => x.BookingLogId == id);

            if (bookingLog == null)
            {
                return NotFound("Booking log not found.");
            }

            bookingLog.IsAcceptedByShop = true;


            await context.SaveChangesAsync();

            return NoContent();
        }


        [HttpGet("NotifyPickupFromClient")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsRiderAccount")]
        public async Task<ActionResult<List<BookingLogDTO>>> NotifyForPickupFromClient()
        {
            // Step 1: Get the logged-in user's email
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

            // Query pending bookings where IsAcceptedByShop is false and AddedById matches the current user
            var pendingBookings = await context.BookingLogs
                .Include(booking => booking.LaundryServiceLog)
                .ThenInclude(log => log.LaundryShop) // Include LaundryShop for details
                .Where(booking =>
                    booking.IsAcceptedByShop == true && booking.PickUpFromClient == false &&// Pending bookings
                    booking.TransactionCompleted == false) // Match AddedById with logged-in user
                .Select(booking => new BookingLogDTO
                {
                    BookingLogId = booking.BookingLogId,
                    LaundryServiceLogId = booking.LaundryServiceLogId,
                    LaundryShopName = booking.LaundryServiceLog.LaundryShop.LaundryShopName,
                    LaundryShopAddress = booking.LaundryServiceLog.LaundryShop.Address,
                    ServiceName = context.Services
                        .Where(service =>
                            booking.LaundryServiceLog.ServiceIds != null &&
                            service.ServiceId == booking.LaundryServiceLog.ServiceIds.FirstOrDefault())
                        .Select(service => service.ServiceName)
                        .FirstOrDefault() ?? "Unknown Service", // Resolve service name or fallback
                    BookingDate = booking.BookingDate,
                    TotalPrice = booking.TotalPrice,
                    Weight = booking.Weight,
                    PickupAddress = booking.PickupAddress,
                    DeliveryAddress = booking.DeliveryAddress,
                    Note = booking.Note,
                    ClientName = context.Users
                        .Where(client => client.Id == booking.ClientId)
                        .Select(client => $"{client.FirstName} {client.LastName}")
                        .FirstOrDefault() ?? "Unknown Client" // Resolve client name or fallback
                })
                .OrderBy(booking => booking.BookingDate)
                .ToListAsync();

            return Ok(pendingBookings);
        }

        [HttpGet("GetPickupNotificationById/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsRiderAccount")]
        public async Task<ActionResult<BookingLogDTO>> GetPickupNotifById(Guid id)
        {
            // Step 1: Get the logged-in user's email
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

            // Step 3: Fetch the specific booking by ID
            var booking = await context.BookingLogs
                .Include(b => b.LaundryServiceLog)
                .ThenInclude(log => log.LaundryShop) // Include LaundryShop details
                .Where(b => b.BookingLogId == id && b.IsAcceptedByShop == true && b.PickUpFromClient == false && !b.TransactionCompleted)
                .Select(b => new BookingLogDTO
                {
                    BookingLogId = id,
                    LaundryServiceLogId = b.LaundryServiceLogId,
                    LaundryShopName = b.LaundryServiceLog.LaundryShop.LaundryShopName,
                    LaundryShopAddress = b.LaundryServiceLog.LaundryShop.Address,
                    ServiceName = context.Services
                        .Where(service => b.LaundryServiceLog.ServiceIds != null &&
                                          service.ServiceId == b.LaundryServiceLog.ServiceIds.FirstOrDefault())
                        .Select(service => service.ServiceName)
                        .FirstOrDefault() ?? "Unknown Service", // Resolve service name or fallback
                    BookingDate = b.BookingDate,
                    TotalPrice = b.TotalPrice,
                    Weight = b.Weight,
                    PickupAddress = b.PickupAddress,
                    DeliveryAddress = b.DeliveryAddress,
                    Note = b.Note,
                    ClientName = context.Users
                        .Where(client => client.Id == b.ClientId)
                        .Select(client => $"{client.FirstName} {client.LastName}")
                        .FirstOrDefault() ?? "Unknown Client" // Resolve client name or fallback
                })
                .FirstOrDefaultAsync();

            // Step 4: Check if booking exists
            if (booking == null)
            {
                return NotFound("Booking not found or not eligible for pickup notification.");
            }

            // Step 5: Return the booking details
            return Ok(booking);
        }







        //accept pickup by rider
        [HttpPut("accept-pickup/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsRiderAccount")]
        public async Task<ActionResult> AcceptPickup(Guid id)
        {
            // Retrieve the email claim from the authenticated user
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("The email claim is missing or invalid.");
            }

            // Attempt to retrieve the user by their email
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("The rider associated with this account does not exist.");
            }

            // Attempt to retrieve the booking log by its ID
            var bookingLog = await context.BookingLogs.FirstOrDefaultAsync(x => x.BookingLogId == id);
            if (bookingLog == null)
            {
                return NotFound("The specified booking log does not exist.");
            }

            // Update the booking log with the rider's information
            bookingLog.PickUpFromClient = true;
            bookingLog.PickupRiderId = user.Id;

            // Save the changes to the database
            await context.SaveChangesAsync();

            return NoContent();
        }

        //notification for client, example: Kian Javellana is on his way to pick up your laundry for Regular Wash Service at Tidy Bubbles!
        [HttpGet("notifyClientForPickup")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsClientAccount")]
        public async Task<ActionResult<List<object>>> NotifyClientForPickup()
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

            var pendingBookings = await context.BookingLogs
                .Include(booking => booking.LaundryServiceLog)
                    .ThenInclude(log => log.LaundryShop)
                .Where(booking => booking.PickUpFromClient == true && booking.TransactionCompleted == false
                            && booking.ClientId == user.Id)
                .OrderBy(booking => booking.BookingDate)
                .Select(booking => new
                {
                    BookingLogId = booking.BookingLogId, // Include the BookingLogId
                    RiderName = context.Users
                        .Where(rider => rider.Id == booking.PickupRiderId)
                        .Select(rider => $"{rider.FirstName} {rider.LastName}")
                        .FirstOrDefault() ?? "Unassigned",
                    ServiceName = context.Services
                        .Where(service =>
                            booking.LaundryServiceLog.ServiceIds != null &&
                            service.ServiceId == booking.LaundryServiceLog.ServiceIds.FirstOrDefault())
                        .Select(service => service.ServiceName)
                        .FirstOrDefault() ?? "Unknown Service",
                    LaundryShopName = booking.LaundryServiceLog.LaundryShop != null
                        ? booking.LaundryServiceLog.LaundryShop.LaundryShopName
                        : "Unknown Shop"
                })
                .ToListAsync();

            return Ok(pendingBookings);
        }


        //see full notif details
        [HttpGet("getClientPickupNotificationById/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsClientAccount")]
        public async Task<ActionResult> GetClientPickupNotificationById(Guid id)
        {
            // Validate user email claim
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

            var bookingNotif = await context.BookingLogs
                .Include(booking => booking.LaundryServiceLog)
                    .ThenInclude(log => log.LaundryShop)
                .Where(booking => booking.PickUpFromClient == true && booking.BookingLogId == id && booking.TransactionCompleted == false)
                .OrderBy(booking => booking.BookingDate)
                .Select(booking => new
                {
                    BookingLogId = id, // Include the BookingLogId
                    RiderName = context.Users
                        .Where(rider => rider.Id == booking.PickupRiderId)
                        .Select(rider => $"{rider.FirstName} {rider.LastName}")
                        .FirstOrDefault() ?? "Unassigned",
                    ServiceName = context.Services
                        .Where(service =>
                            booking.LaundryServiceLog.ServiceIds != null &&
                            service.ServiceId == booking.LaundryServiceLog.ServiceIds.FirstOrDefault())
                        .Select(service => service.ServiceName)
                        .FirstOrDefault() ?? "Unknown Service",
                    LaundryShopName = booking.LaundryServiceLog.LaundryShop != null
                        ? booking.LaundryServiceLog.LaundryShop.LaundryShopName
                        : "Unknown Shop"
                })
                .ToListAsync();

            return Ok(bookingNotif);
        }


        //fixed, dec 1 11:24
        //input weight to calculate total price, laundry shop interface
        [HttpPut("inputWeight/{id:Guid}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult> InputWeight(Guid id, [FromBody] BookingLogCreationDTO bookingLogCreationDTO)
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

            var existingBookingLog = await context.BookingLogs
                .FirstOrDefaultAsync(log => log.BookingLogId == id);

            if (existingBookingLog == null)
            {
                return NotFound("Booking log not found.");
            }

            var laundryServiceLog = await context.LaundryServiceLogs
                .FirstOrDefaultAsync(log => log.LaundryServiceLogId == bookingLogCreationDTO.LaundryServiceLogId);

            if (laundryServiceLog == null)
            {
                return NotFound($"Laundry service log with ID {bookingLogCreationDTO.LaundryServiceLogId} not found.");
            }

            if (!laundryServiceLog.Price.HasValue)
            {
                return BadRequest("The price for the laundry service log is not set.");
            }

            if (!bookingLogCreationDTO.Weight.HasValue)
            {
                return BadRequest("Weight is required to calculate the total price.");
            }

            // Update the existing booking log
            existingBookingLog.Weight = bookingLogCreationDTO.Weight;
            existingBookingLog.TotalPrice = laundryServiceLog.Price.Value * bookingLogCreationDTO.Weight.Value;

            await context.SaveChangesAsync();

            return NoContent();
        }



        //update as of December 1, 2024
        //notify client for laundry weight and total price
        [HttpGet("NotifyClientForWeightAndTotalPrice")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsClientAccount")]
        public async Task<ActionResult> NotifyClientForWeightAndTotalPrice(Guid id)
        {
            // Retrieve user email from JWT claims
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Fetch the user using email
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Fetch the BookingLog along with LaundryShop and Service details
            var bookingDetails = await context.BookingLogs
                .Where(b => b.ClientId == user.Id && b.BookingLogId == id) // Filter by client and booking ID
                .Select(b => new
                {
                    BookingLogId = b.BookingLogId,
                    LaundryShopName = b.LaundryServiceLog.LaundryShop.LaundryShopName, // Get laundry shop name
                    ServiceName = context.Services
                        .Where(s => b.LaundryServiceLog.ServiceIds != null && s.ServiceId == b.LaundryServiceLog.ServiceIds.FirstOrDefault())
                        .Select(s => s.ServiceName)
                        .FirstOrDefault(), // Get service name
                    Weight = b.Weight,
                    TotalPrice = b.TotalPrice
                })
                .FirstOrDefaultAsync();

            if (bookingDetails == null)
            {
                return NotFound("No booking found for the provided ID.");
            }

            return Ok(bookingDetails);
        }



        //start laundry
        [HttpPut("hasStartedLaundry/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult> HasStartedLaundry(Guid id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var bookingLog = await context.BookingLogs
                .FirstOrDefaultAsync(x => x.BookingLogId == id);

            if (bookingLog == null)
            {
                return NotFound("Booking log not found.");
            }

            bookingLog.HasStartedYourLaundry = !bookingLog.HasStartedYourLaundry;


            await context.SaveChangesAsync();

            return NoContent();
        }

        //notify client that laundry has started, example : Tidy Bubbles has started your laundry! Service: Regular Wash
        [HttpGet("has-started-laundry-notification")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsClientAccount")]
public async Task<ActionResult<List<object>>> HasStartedLaundryNotification()
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

    var startedLaundryLogs = await context.BookingLogs
        .Include(booking => booking.LaundryServiceLog)
            .ThenInclude(log => log.LaundryShop)
        .Where(booking => booking.ClientId == user.Id && booking.HasStartedYourLaundry)
        .OrderBy(booking => booking.BookingDate)
        .Select(booking => new
        {
            BookingLogId = booking.BookingLogId, // Include the BookingLogId for frontend use
            LaundryShopName = booking.LaundryServiceLog.LaundryShop != null
                ? booking.LaundryServiceLog.LaundryShop.LaundryShopName
                : "Unknown Shop",
            ServiceName = context.Services
                .Where(service =>
                    booking.LaundryServiceLog.ServiceIds != null &&
                    service.ServiceId == booking.LaundryServiceLog.ServiceIds.FirstOrDefault())
                .Select(service => service.ServiceName)
                .FirstOrDefault() ?? "Unknown Service"
        })
        .ToListAsync();

    return Ok(startedLaundryLogs);
}





        [HttpPut("is-ready-for-delivery/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult> IsReadyForDelivery(Guid id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var bookingLog = await context.BookingLogs
                .FirstOrDefaultAsync(x => x.BookingLogId == id);

            if (bookingLog == null)
            {
                return NotFound("Booking log not found.");
            }

            bookingLog.IsReadyForDelivery = !bookingLog.IsReadyForDelivery;


            await context.SaveChangesAsync();

            return NoContent();
        }


        // notify the rider the laundry is ready to be picked up for delivery 
        [HttpGet("notify-pickup-from-shop")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsRiderAccount")]
        public async Task<ActionResult<List<BookingLogDTO>>> NotifyForPickupFromShop(BookingLogDTO bookingLogDTO)
        {

            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var laundryServiceLog = await context.LaundryServiceLogs
              .Include(log => log.LaundryShop) // Ensure related LaundryShop is included
              .FirstOrDefaultAsync(log => log.LaundryServiceLogId == bookingLogDTO.LaundryServiceLogId);

            if (laundryServiceLog == null || laundryServiceLog.LaundryShop == null)
            {
                return NotFound("Laundry service log or related laundry shop not found.");
            }


            var pendingBookings = await context.BookingLogs
               .Include(booking => booking.LaundryServiceLog)
                   .ThenInclude(laundryServiceLog => laundryServiceLog.LaundryShop)
               .Join(context.Users, // Join with ApplicationUser
                     booking => booking.ClientId, // Foreign Key in BookingLog
                     user => user.Id, // Primary Key in ApplicationUser
                     (booking, user) => new { booking, user }) // Combine both tables
               .Where(x => x.booking.IsReadyForDelivery == true && x.booking.TransactionCompleted == false)
               .OrderBy(x => x.booking.BookingDate)
               .Select(x => new BookingLogDTO
               {
                   BookingLogId = x.booking.BookingLogId,
                   LaundryShopName = x.booking.LaundryServiceLog.LaundryShop.LaundryShopName,
                   BookingDate = x.booking.BookingDate,
                   PickupAddress = x.booking.PickupAddress,
                   DeliveryAddress = x.booking.DeliveryAddress,
                   Note = x.booking.Note,
                   ClientName = x.user.FirstName + " " + x.user.LastName // Get full name
               })
               .ToListAsync();


            return Ok(mapper.Map<List<BookingLogDTO>>(pendingBookings));
        }


        //get delivery notif by id
        [HttpGet("NotifyPickupFromShopById/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsRiderAccount")]
        public async Task<ActionResult<BookingLogDTO>> NotifyForPickupFromShop(Guid id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Fetch the booking log by ID
            var bookingLog = await context.BookingLogs
                .Include(booking => booking.LaundryServiceLog)
                    .ThenInclude(laundryServiceLog => laundryServiceLog.LaundryShop)
                .FirstOrDefaultAsync(booking => booking.BookingLogId == id &&
                                                 booking.IsReadyForDelivery == true &&
                                                 booking.TransactionCompleted == false);

            if (bookingLog == null)
            {
                return NotFound("Booking log not found or does not meet the criteria.");
            }

            // Fetch the client associated with the booking
            var client = await context.Users
                .FirstOrDefaultAsync(user => user.Id == bookingLog.ClientId);

            if (client == null)
            {
                return NotFound("Client associated with the booking log not found.");
            }

            // Create the DTO
            var bookingLogDTO = new BookingLogDTO
            {
                BookingLogId = bookingLog.BookingLogId,
                LaundryShopName = bookingLog.LaundryServiceLog.LaundryShop.LaundryShopName,
                BookingDate = bookingLog.BookingDate,
                PickupAddress = bookingLog.PickupAddress,
                DeliveryAddress = bookingLog.DeliveryAddress,
                Note = bookingLog.Note,
                ClientName = client.FirstName + " " + client.LastName
            };

            return Ok(bookingLogDTO);
        }


        // Kian Javellana has accepted the available delivery
        [HttpGet("NotifyDeliveryIsAccepted")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<BookingLogDTO>>> NotifyDeliveryIsAccepted()
        {
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

            // Fetch the pending bookings where the pickup is from the shop and transaction is not completed
            var delivery = await context.BookingLogs
                .Include(booking => booking.LaundryServiceLog)
                    .ThenInclude(log => log.LaundryShop)
                .Where(booking => booking.PickUpFromShop == true && booking.TransactionCompleted == false)
                .OrderBy(booking => booking.DeliveryDate)
                .Select(booking => new BookingLogDTO
                {
                    BookingLogId = booking.BookingLogId, // Include the BookingLogId
                    RiderName = context.Users
                        .Where(rider => rider.Id == booking.PickupRiderId)
                        .Select(rider => $"{rider.FirstName} {rider.LastName}")
                        .FirstOrDefault() ?? "Unassigned", // If no rider assigned, set as "Unassigned"
                    DeliveryDate = DateTime.UtcNow // Set the current delivery date as the system time
                })
                .ToListAsync();

            return Ok(delivery);
        }


        [HttpPut("accept-delivery/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsRiderAccount")]
        public async Task<ActionResult> AcceptDelivery(Guid id)
        {
            // Retrieve the email claim from the authenticated user
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("The email claim is missing or invalid.");
            }

            // Attempt to retrieve the user by their email
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("The rider associated with this account does not exist.");
            }

            // Attempt to retrieve the booking log by its ID
            var bookingLog = await context.BookingLogs.FirstOrDefaultAsync(x => x.BookingLogId == id);
            if (bookingLog == null)
            {
                return NotFound("The specified booking log does not exist.");
            }

            // Update the booking log with delivery information
            bookingLog.PickUpFromShop = true;
            bookingLog.DeliveryRiderId = user.Id;

            // Save the changes to the database
            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("pending-bookings-for-status-update")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccountOrRiderAccount")]
        public async Task<ActionResult> GetPendingBookingsForStatusUpdate()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Ensure user exists
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Query for pending bookings where TransactionCompleted == false
            var pendingBookings = await context.BookingLogs
                .Where(booking =>
                    booking.TransactionCompleted == false &&
                    (booking.LaundryServiceLog.LaundryShop.AddedById == user.Id ||
                     (booking.DeliveryRiderId != null && booking.DeliveryRiderId == user.Id)))
                .OrderBy(booking => booking.BookingDate)
                .Select(booking => new
                {
                    BookingLogId = booking.BookingLogId,
                    LaundryShopName = booking.LaundryServiceLog.LaundryShop.LaundryShopName,
                    ServiceName = booking.LaundryServiceLog.ServiceIds != null
                        ? context.Services
                            .Where(service => service.ServiceId == booking.LaundryServiceLog.ServiceIds.FirstOrDefault())
                            .Select(service => service.ServiceName)
                            .FirstOrDefault() ?? "Unknown Service"
                        : "Unknown Service",
                    BookingDate = booking.BookingDate,
                    ClientName = context.Users
                        .Where(client => client.Id == booking.ClientId)
                        .Select(client => $"{client.FirstName} {client.LastName}")
                        .FirstOrDefault() ?? "Unknown Client",
                    PickupAddress = booking.PickupAddress,
                    DeliveryAddress = booking.DeliveryAddress,
                    Weight = booking.Weight,
                    TotalPrice = booking.TotalPrice
                })
                .ToListAsync();



            return Ok(pendingBookings);
        }


        [HttpGet("booking-log-details/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccountOrClientAccount")]
        public async Task<ActionResult> GetBookingLogDetailsById(Guid id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Ensure user exists
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Query for the booking log with the specific ID
            var bookingLog = await context.BookingLogs
                .Where(booking => booking.BookingLogId == id)
                .Select(booking => new
                {
                    LaundryShopName = booking.LaundryServiceLog.LaundryShop.LaundryShopName,
                    ServiceName = context.Services
                        .Where(service =>
                            booking.LaundryServiceLog.ServiceIds != null &&
                            service.ServiceId == booking.LaundryServiceLog.ServiceIds.FirstOrDefault())
                        .Select(service => service.ServiceName)
                        .FirstOrDefault() ?? "Unknown Service", // Default if no service is found
                    BookingDate = booking.BookingDate,
                    ClientName = context.Users
                        .Where(client => client.Id == booking.ClientId)
                        .Select(client => $"{client.FirstName} {client.LastName}")
                        .FirstOrDefault() ?? "Unknown Client", // Default if no client is found
                    PickupAddress = booking.PickupAddress,
                    DeliveryAddress = booking.DeliveryAddress,
                    Weight = booking.Weight, // Assuming this is part of the BookingLog entity
                    TotalPrice = booking.TotalPrice, // Assuming this is part of the BookingLog entity

                })
                .FirstOrDefaultAsync();

            if (bookingLog == null)
            {
                return NotFound("Booking log not found.");
            }

            return Ok(bookingLog);
        }



        //view accepted pickups, rider
        [HttpGet("GetAcceptedPickupsById/{id:Guid}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsRiderAccount")]
        public async Task<ActionResult<List<BookingLogDTO>>> GetAcceptedPickupsById()
        {
            // Step 1: Fetch the logged-in user's email for validation (optional)
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("Logged-in user not found.");
            }

            // Step 2: Query bookings accepted by the user with the provided ID
            var acceptedPickups = await context.BookingLogs
                .Include(b => b.LaundryServiceLog)
                .ThenInclude(log => log.LaundryShop) // Include related LaundryShop
                .Where(b => b.IsAcceptedByShop == true && // Booking is accepted by the shop
                            b.PickupRiderId == user.Id && b.PickUpFromClient == true &&           // Match the provided userId (RiderId)
                            !b.TransactionCompleted)      // Ensure the transaction isn't completed
                .Select(b => new BookingLogDTO
                {
                    BookingLogId = b.BookingLogId,
                    LaundryServiceLogId = b.LaundryServiceLogId,
                    LaundryShopName = b.LaundryServiceLog.LaundryShop.LaundryShopName,
                    ServiceName = context.Services
                        .Where(service => b.LaundryServiceLog.ServiceIds != null &&
                                          service.ServiceId == b.LaundryServiceLog.ServiceIds.FirstOrDefault())
                        .Select(service => service.ServiceName)
                        .FirstOrDefault() ?? "Unknown Service", // Resolve service name or fallback
                    BookingDate = b.BookingDate,
                    TotalPrice = b.TotalPrice,
                    Weight = b.Weight,
                    PickupAddress = b.PickupAddress,
                    DeliveryAddress = b.DeliveryAddress,
                    Note = b.Note,
                    ClientName = context.Users
                        .Where(client => client.Id == b.ClientId)
                        .Select(client => $"{client.FirstName} {client.LastName}")
                        .FirstOrDefault() ?? "Unknown Client" // Resolve client name or fallback
                })
                .OrderBy(b => b.BookingDate) // Sort by booking date
                .ToListAsync();

            // Step 3: Handle empty result
            if (!acceptedPickups.Any())
            {
                return NotFound("No accepted pickups found for the specified user.");
            }

            // Step 4: Return the list of accepted pickups
            return Ok(acceptedPickups);
        }


        //sent out for delivery
        [HttpPut("departed-from-shop/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult> DepartedFromShop(Guid id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var bookingLog = await context.BookingLogs
                .FirstOrDefaultAsync(x => x.BookingLogId == id);

            if (bookingLog == null)
            {
                return NotFound("Booking log not found.");
            }

            bookingLog.DepartedFromShop = !bookingLog.DepartedFromShop;

            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("out-for-delivery/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsRiderAccount")]
        public async Task<ActionResult> IsOutForDelivery(Guid id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var bookingLog = await context.BookingLogs
                .FirstOrDefaultAsync(x => x.BookingLogId == id);

            if (bookingLog == null)
            {
                return NotFound("Booking log not found.");
            }

            bookingLog.IsOutForDelivery = !bookingLog.IsOutForDelivery;

            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("received-by-client/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsClientAccount")]
        public async Task<ActionResult> IsReceivedByClient(Guid id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var bookingLog = await context.BookingLogs
                .FirstOrDefaultAsync(x => x.BookingLogId == id);

            if (bookingLog == null)
            {
                return NotFound("Booking log not found.");
            }

            bookingLog.ReceivedByClient = true;

            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("transaction-completed/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsRiderAccount")]
        public async Task<ActionResult> TransactionCompleted(Guid id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var bookingLog = await context.BookingLogs
                .FirstOrDefaultAsync(x => x.BookingLogId == id);

            if (bookingLog == null)
            {
                return NotFound("Booking log not found.");
            }

            bookingLog.TransactionCompleted = true;

            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("notify-transaction-completed")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<BookingLogDTO>>> NotifyTransactionCompleted()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;


            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return NotFound("Logged-in user not found.");
            }
            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var pendingBookings = await context.BookingLogs
                .Include(b => b.LaundryServiceLog) // Include LaundryServiceLog
                    .ThenInclude(log => log.LaundryShop) // Include LaundryShop
                .Where(x => x.TransactionCompleted == true &&
                            x.LaundryServiceLog.LaundryShop.AddedById == user.Id) // Add the condition
                .ToListAsync();


            return Ok(mapper.Map<List<BookingLogDTO>>(pendingBookings));
        }



        [HttpDelete("{id:Guid}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsClientAccount")]
        public async Task<ActionResult> Delete(Guid id)
        {

            return NoContent();
        }

        [HttpGet("PostGet")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsClientAccount")]
        public async Task<ActionResult<List<BookingLogDTO>>> GetBookingLogsPostGet()
        {
            var bookingLogs = await context.LaundryServiceLogs.ToListAsync();
            return mapper.Map<List<BookingLogDTO>>(bookingLogs);

        }



    }
}
