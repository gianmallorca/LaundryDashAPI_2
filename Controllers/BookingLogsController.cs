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
                IsAccepted = false // Set default as false
            };


            // Add the new booking log to the context
            context.BookingLogs.Add(bookingLog);

            // Save changes to the database
            await context.SaveChangesAsync();

            // Return a NoContent response (status code 204)
            return NoContent();
        }





        //get pending bookings
        [HttpGet("getPendingBookings")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
        public async Task<ActionResult<List<BookingLogDTO>>> GetPendingBookings()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var pendingBookings = await context.BookingLogs
                .Where(x => x.IsAccepted == false) 
                .OrderBy(x => x.BookingDate)      
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
