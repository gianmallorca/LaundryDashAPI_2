using AutoMapper;
using LaundryDashAPI_2;
using LaundryDashAPI_2.DTOs;
using LaundryDashAPI_2.DTOs.BookingLog;
using LaundryDashAPI_2.DTOs.LaundryServiceLog;
using LaundryDashAPI_2.DTOs.LaundryShop;
using LaundryDashAPI_2.Entities;
using LaundryDashAPI_2.Helpers;
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

 
        [HttpGet("notify-pickup-from-client")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsRiderAccount")]
        public async Task<ActionResult<List<BookingLogDTO>>> NotifyForPickupFromClient(BookingLogDTO bookingLogDTO)
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
               .Where(x => x.booking.IsAcceptedByShop == true && x.booking.HasStartedYourLaundry == false && x.booking.TransactionCompleted == false)
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
        [HttpGet("notify-client-for-pickup")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsClientAccount")]
        public async Task<ActionResult<List<object>>> NotifyClientForPickup(BookingLogDTO bookingLogDTO)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Fetch the client user details directly
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var pendingBookings = await context.BookingLogs
                .Include(booking => booking.LaundryServiceLog)
                    .ThenInclude(log => log.LaundryShop) // Ensure LaundryShop is included
                .Where(booking => booking.PickUpFromClient == true && booking.TransactionCompleted == false)
                .OrderBy(booking => booking.BookingDate)
                .Select(booking => new
                {
                    RiderName = context.Users
                        .Where(rider => rider.Id == booking.PickupRiderId)
                        .Select(rider => $"{rider.FirstName} {rider.LastName}")
                        .FirstOrDefault() ?? "Unassigned", // Handle case where RiderId is null
                    ServiceName = context.Services
                        .Where(service =>
                            booking.LaundryServiceLog.ServiceIds != null &&
                            service.ServiceId == booking.LaundryServiceLog.ServiceIds.FirstOrDefault())
                        .Select(service => service.ServiceName)
                        .FirstOrDefault() ?? "Unknown Service", // Resolve service name or fallback
                    LaundryShopName = booking.LaundryServiceLog.LaundryShop != null
                        ? booking.LaundryServiceLog.LaundryShop.LaundryShopName
                        : "Unknown Shop" // Handle missing LaundryShop
                })
                .ToListAsync();

            return Ok(pendingBookings);
        }


        //start laundry
        [HttpPut("has-started-laundry/{id}")]
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

            bookingLog.HasStartedYourLaundry = true;

            await context.SaveChangesAsync();

            return NoContent();
        }

        //notify client that laundry has started, example : Tidy Bubbles has started your laundry! Service: Regular Wash
        [HttpGet("has-started-laundry-notification")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsClientAccount")]
        public async Task<ActionResult> HasStartedLaundryNotification(BookingLogDTO bookingLogDTO)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Validate the client's email claim
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Fetch the booking log to retrieve related details
            var bookingLog = await context.BookingLogs
                .Include(booking => booking.LaundryServiceLog)
                    .ThenInclude(log => log.LaundryShop) // Include the LaundryShop
                .FirstOrDefaultAsync(booking => booking.BookingLogId == bookingLogDTO.BookingLogId);

            if (bookingLog == null || bookingLog.LaundryServiceLog == null)
            {
                return NotFound("Booking log or related laundry service log not found.");
            }

            // Fetch the service name based on the ServiceIds in the laundry service log
            var serviceName = context.Services
                .Where(service =>
                    bookingLog.LaundryServiceLog.ServiceIds != null &&
                    service.ServiceId == bookingLog.LaundryServiceLog.ServiceIds.FirstOrDefault())
                .Select(service => service.ServiceName)
                .FirstOrDefault() ?? "Unknown Service";

            // Retrieve the laundry shop name
            var laundryShopName = bookingLog.LaundryServiceLog.LaundryShop?.LaundryShopName ?? "Unknown Laundry Shop";

            // Return the notification details
            return Ok(new
            {
                LaundryShopName = laundryShopName,
                ServiceName = serviceName
            });
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

            bookingLog.IsReadyForDelivery = true;

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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccountOrClientAccount")]
        public async Task<ActionResult<List<object>>> GetPendingBookingsForStatusUpdate()
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
                .Where(booking => booking.TransactionCompleted == false)
                .OrderBy(booking => booking.BookingDate)
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
                        .FirstOrDefault() ?? "Unknown Client" // Default if no client is found
                })
                .ToListAsync();

            return Ok(pendingBookings);
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

            bookingLog.DepartedFromShop = true;

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

            bookingLog.IsOutForDelivery = true;

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

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var pendingBookings = await context.BookingLogs
                .Where(x => x.TransactionCompleted == true)
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
