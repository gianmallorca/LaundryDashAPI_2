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

            // Retrieve the LaundryServiceLog associated with the given LaundryServiceLogId
            var laundryServiceLog = await context.LaundryServiceLogs
                .Include(log => log.LaundryShop) // Ensure related LaundryShop is included
                .FirstOrDefaultAsync(log => log.LaundryServiceLogId == bookingLogCreationDTO.LaundryServiceLogId);

            if (laundryServiceLog == null || laundryServiceLog.LaundryShop == null)
            {
                return NotFound("Laundry service log or related laundry shop not found.");
            }

            // Map the incoming DTO to the BookingLog entity
            var bookingLog = new BookingLog
            {
                BookingLogId = Guid.NewGuid(), // Generate new BookingLogId
                LaundryServiceLogId = bookingLogCreationDTO.LaundryServiceLogId, // Set the associated LaundryServiceLogId
                LaundryShopName = laundryServiceLog.LaundryShop.LaundryShopName, // Extract LaundryShopName from related LaundryShop
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



        //get pending bookings
        //[HttpGet("getPendingBookings")]
        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
        //public async Task<ActionResult<List<BookingLogDTO>>> GetPendingBookings()
        //{
        //    var email = User.FindFirst(ClaimTypes.Email)?.Value;

        //    // Check if the email is null or empty
        //    if (string.IsNullOrEmpty(email))
        //    {
        //        return BadRequest("User email claim is missing.");
        //    }

        //    var pendingBookings = await context.BookingLogs
        //        .Where(x => x.IsAcceptedByShop == false)
        //        .OrderBy(x => x.BookingDate)      
        //        .ToListAsync();                   

        //    return Ok(mapper.Map<List<BookingLogDTO>>(pendingBookings));
        //}

        [HttpGet("get-pending-bookings")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<BookingLogDTO>>> GetPendingBookings()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var pendingBookings = await context.BookingLogs
                .Include(booking => booking.LaundryServiceLog)
                .Where(x => x.IsAcceptedByShop == false)
                .OrderBy(x => x.BookingDate)
                .Select(booking => new BookingLogDTO
                {
                    BookingLogId = booking.BookingLogId,
                    LaundryShopName = booking.LaundryShopName,
                    BookingDate = booking.BookingDate,
                    PickupAddress = booking.PickupAddress,
                    DeliveryAddress = booking.DeliveryAddress,
                    Note = booking.Note,
                    ClientName = context.Users
                        .Where(user => user.Id == booking.ClientId)
                        .Select(user => $"{user.FirstName} {user.LastName}")
                        .FirstOrDefault(), // Return a single ClientName
                    ServiceName = context.Services
                        .Where(service => booking.LaundryServiceLog.ServiceIds.Contains(service.ServiceId)) // Match any ServiceId
                        .Select(service => service.ServiceName) // Select the ServiceName
                        .FirstOrDefault() // Fetch just one ServiceName (not a list)
                })
                .ToListAsync();

            return Ok(pendingBookings); // Directly return the result without remapping
        }






        //laundry shop accepts booking
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

        //notify rider for pickup
        [HttpGet("notify-pickup-from-client")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsRiderAccount")]
        public async Task<ActionResult<List<BookingLogDTO>>> NotifyForPickupFromClient()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var pendingBookings = await context.BookingLogs
                .Where(x => x.IsAcceptedByShop == true)
                .OrderBy(x => x.BookingDate)
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

        [HttpGet("notify-pickup-from-shop")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsRiderAccount")]
        public async Task<ActionResult<List<BookingLogDTO>>> NotifyForPickupFromShop()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var pendingBookings = await context.BookingLogs
                .Where(x => x.IsReadyForDelivery == true)
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
