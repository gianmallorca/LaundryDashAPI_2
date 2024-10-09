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

namespace LaundryDashAPI_2.Controllers
{
    //[Authorize]
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

        [HttpGet("{Id:Guid}", Name = "getLaundryServiceLog")]
        public async Task<ActionResult<LaundryServiceLogDTO>> Get(Guid id)
        {
            var laundryServiceLog = await context.LaundryServiceLogs.FirstOrDefaultAsync(x => x.LaundryServiceLogId == id);

            if (laundryServiceLog == null)
            {
                return NotFound();
            }

            return mapper.Map<LaundryServiceLogDTO>(laundryServiceLog);
        }


        //will handle multiple service ids at once
        [HttpPost("create")]
        public async Task<ActionResult> Post([FromBody] LaundryServiceLogCreationDTO laundryServiceLogCreationDTO)
        {

            if (laundryServiceLogCreationDTO == null)
            {
                return BadRequest("Request body cannot be null.");
            }
            // Map DTO to entity
            var laundryServiceLog = mapper.Map<LaundryServiceLog>(laundryServiceLogCreationDTO);

            // Assign the service IDs from the DTO
            laundryServiceLog.ServiceIds = laundryServiceLogCreationDTO.ServiceIds;

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

            laundryServiceLog.AddedById = user.Id;

            // Add the entity to the context
            context.LaundryServiceLogs.Add(laundryServiceLog);

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

        [HttpPut("api/laundryServiceLogs/save-price/{id}")]
        public async Task<ActionResult> SavePrice(Guid id, [FromBody] LaundryServiceLogCreationDTO laundryServiceLogCreationDTO)
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
    }
}
