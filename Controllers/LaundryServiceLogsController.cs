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
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace LaundryDashAPI_2.Controllers
{
    //[Authorize]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/laundryServiceLogs")]
    [ApiController]
    // [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
    public class LaundryServiceLogController : Controller
    {
        private readonly ILogger<LaundryServiceLogController> logger;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly UserManager<ApplicationUser> userManager;

        public LaundryServiceLogController(ILogger<LaundryServiceLogController> logger, ApplicationDbContext context, IMapper mapper, UserManager<ApplicationUser> userManager)
        {
            this.logger = logger;
            this.context = context;
            this.mapper = mapper;
            this.userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<List<LaundryServiceLogDTO>>> Get([FromQuery] PaginationDTO paginationDTO)
        {
            var queryable = context.LaundryServiceLogs.AsQueryable();
            await HttpContext.InsertParametersPaginationInHeader(queryable);

            var laundryServiceLogs = await queryable.OrderBy(x => x.LaundryServiceLogId).Paginate(paginationDTO).ToListAsync();

            return mapper.Map<List<LaundryServiceLogDTO>>(laundryServiceLogs);
        }

        [HttpGet("getLaundryServiceLog/{id}")]
        public async Task<ActionResult<LaundryServiceLogDTO>> Get(Guid id)
        {
            var laundryServiceLog = await context.LaundryServiceLogs.FirstOrDefaultAsync(x => x.LaundryServiceLogId == id);

            if (laundryServiceLog == null)
            {
                return NotFound();
            }

            return mapper.Map<LaundryServiceLogDTO>(laundryServiceLog);
        }


        //new
        [HttpGet("manage-prices-by-LaundryId/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<LaundryServiceLogDTO>>> GetShopToManagePrices(Guid id)
        {
            // Check if the user exists in the database
            var userExists = await userManager.Users.AnyAsync(x => x.Id == userId.ToString());
            if (!userExists)
            {
                return NotFound("User not found.");
            }

            // Fetch laundry service logs by the user ID
            var laundryServiceLogs = await context.LaundryServiceLogs
                .Where(log => log.AddedById == userId.ToString()) // Assuming AddedById is of type string
                .ToListAsync();

            // Check if any logs were found
            if (!laundryServiceLogs.Any())
            {
                return NotFound("No service logs found for this user.");
            }

            // Extract and return distinct service IDs
            var allServiceIds = laundryServiceLogs
                .SelectMany(log => log.ServiceIds ?? Enumerable.Empty<Guid>()) // Safely handling nulls
                .Distinct() // Avoid duplicate service IDs
                .ToList();

            return Ok(allServiceIds);
        }

            // Create and return the combined result (LaundryShop + associated ServiceIds)
            var result = new LaundryServiceLogCreationDTO
            {
                LaundryShopId = laundryShop.LaundryShopId,    // Return the laundry shop information
                ServiceIds = allServiceIds    // Return the associated ServiceIds as a list of Guids
            };

            return Ok(result);
        }




        //will handle multiple service ids at once
        [HttpPost("create")]
        public async Task<ActionResult> Post([FromBody] LaundryServiceLogCreationDTO laundryServiceLogCreationDTO)
        {

            if (laundryServiceLogCreationDTO == null)
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

        
            foreach (var serviceId in laundryServiceLogCreationDTO.ServiceIds)
            {
                var laundryServiceLog = mapper.Map<LaundryServiceLog>(laundryServiceLogCreationDTO);

                laundryServiceLog.ServiceIds = new List<Guid> { serviceId }; // Store only the current service ID

           
                laundryServiceLog.AddedById = user.Id;

                context.LaundryServiceLogs.Add(laundryServiceLog);
            }

            // Save changes to the database
            await context.SaveChangesAsync();

            return NoContent();

        }


        //update only the list of services
        [HttpPut("{id:Guid}")]
        public async Task<ActionResult> Edit(Guid id, [FromBody] LaundryServiceLogCreationDTO laundryServiceLogCreationDTO)
        {
            // Find the existing LaundryServiceLog by ID
            var laundryServiceLog = await context.LaundryServiceLogs
                .FirstOrDefaultAsync(x => x.LaundryServiceLogId == id);

            // Check if the log exists
            if (laundryServiceLog == null)
            {
                return NotFound();
            }

            // Only update the ServiceIds, keeping LaundryShopId unchanged
            laundryServiceLog.ServiceIds = laundryServiceLogCreationDTO.ServiceIds;

            // Save the changes to the context
            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("save-price/{id}")]
        public async Task<ActionResult> SavePrice(Guid id, [FromRoute] LaundryServiceLogCreationDTO laundryServiceLogCreationDTO)
        {
            // Find the existing LaundryServiceLog by ID


            //wala pani sure na part, i change ra nako ni (by Gian)
            var laundryServiceLog = await context.LaundryServiceLogs
                .FirstOrDefaultAsync(x => x.LaundryServiceLogId == id);

            // Check if the log exists
            if (laundryServiceLog == null)
            {
                return NotFound();
            }

            // Only update the ServiceIds, keeping LaundryShopId unchanged
            laundryServiceLog.ServiceIds = laundryServiceLogCreationDTO.ServiceIds;

            // Save the changes to the context
            await context.SaveChangesAsync();

            return NoContent();
        }


        [HttpDelete("{id:Guid}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var exists = await context.LaundryServiceLogs.AnyAsync(x => x.LaundryServiceLogId == id);

            if (!exists)
            {
                return NotFound();
            }

            context.Remove(new LaundryServiceLog() { LaundryServiceLogId = id });
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("PostGet")]
        public async Task<ActionResult<List<LaundryServiceLogDTO>>> GetServiceLogsPostGet()
        {
            var laundryServiceLogs = await context.LaundryServiceLogs.ToListAsync();
            return mapper.Map<List<LaundryServiceLogDTO>>(laundryServiceLogs);
        }

        // Method to update the price of a specific LaundryServiceLog
        [HttpPut("update-price/{id:Guid}")]
        public async Task<ActionResult> UpdatePrice(Guid id, [FromBody] decimal newPrice)
        {
            // Find the existing LaundryServiceLog by ID
            var laundryServiceLog = await context.LaundryServiceLogs
                .FirstOrDefaultAsync(x => x.LaundryServiceLogId == id);

            // Check if the log exists
            if (laundryServiceLog == null)
            {
                return NotFound();
            }







       









    }
}
