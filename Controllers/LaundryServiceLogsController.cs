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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
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
            // Retrieve all service logs associated with the given LaundryShopId
            try
            {
                var laundryServiceLogs = await context.LaundryServiceLogs
                    .Where(x => x.LaundryShopId == id)
                    .Include(x => x.LaundryShop)
                    .ToListAsync();

                if (!laundryServiceLogs.Any())
                {
                    return NotFound("No service logs found for this laundry shop.");
                }

                var result = mapper.Map<List<LaundryServiceLogDTO>>(laundryServiceLogs);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }

        }





        //to be fixed if errors on FE, temporary method
        [HttpGet("getLogByLaundryId/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<LaundryServiceLogDTO>> GetLogByLaundryId(Guid id)
        {
            // Find the laundry shop by the given ID
            var laundryShop = await context.LaundryShops.FirstOrDefaultAsync(x => x.LaundryShopId == id);

            if (laundryShop == null)
            {
                return NotFound("Laundry shop not found.");
            }

            // Retrieve all service logs for the specified laundry shop
            var laundryServiceLogs = await context.LaundryServiceLogs
                .Where(log => log.LaundryShopId == laundryShop.LaundryShopId)
                .ToListAsync();

            // Create a list to store all ServiceIds (as Guids)
            List<Guid> allServiceIds = new List<Guid>();

            // Loop through each service log and add the ServiceIds to the list
            foreach (var log in laundryServiceLogs)
            {
                if (log.ServiceIds != null && log.ServiceIds.Any())
                {
                    allServiceIds.AddRange(log.ServiceIds); // Add the ServiceIds directly
                }
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
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

            // Fetch existing LaundryServiceLogs to check if serviceId already exists
            var existingServiceLogs = await context.LaundryServiceLogs
                .Where(log => log.LaundryShopId == laundryServiceLogCreationDTO.LaundryShopId)
                .ToListAsync();

            foreach (var serviceId in laundryServiceLogCreationDTO.ServiceIds)
            {
                // Check if the serviceId already exists in the current LaundryServiceLogs
                if (existingServiceLogs.Any(log => log.ServiceIds.Contains(serviceId)))
                {
                    // If the serviceId already exists, skip this iteration
                    continue;
                }

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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult> SavePrice(Guid id, [FromBody] LaundryServiceLogCreationDTO laundryServiceLogCreationDTO)
        {
            var laundryServiceLog = await context.LaundryServiceLogs
                .FirstOrDefaultAsync(x => x.LaundryServiceLogId == id);

            if (laundryServiceLog == null)
            {
                return NotFound("Laundry service log not found.");
            }

            laundryServiceLog.Price = laundryServiceLogCreationDTO.Price;

            await context.SaveChangesAsync();

            return NoContent();
        }



        [HttpDelete("{id:Guid}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<LaundryServiceLogDTO>>> GetServiceLogsPostGet()
        {
            var laundryServiceLogs = await context.LaundryServiceLogs.ToListAsync();
            return mapper.Map<List<LaundryServiceLogDTO>>(laundryServiceLogs);

        }











    }
}
