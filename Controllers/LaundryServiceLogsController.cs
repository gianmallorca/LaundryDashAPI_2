using AutoMapper;
using LaundryDashAPI_2;
using LaundryDashAPI_2.DTOs;
using LaundryDashAPI_2.Entities;
using LaundryDashAPI_2.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        public LaundryServiceLogController(ILogger<LaundryServiceLogController> logger, ApplicationDbContext context, IMapper mapper)
        {
            this.logger = logger;
            this.context = context;
            this.mapper = mapper;
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

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] LaundryServiceLogCreationDTO laundryServiceLogCreationDTO)
        {
            var laundryServiceLog = mapper.Map<LaundryServiceLog>(laundryServiceLogCreationDTO);
            context.Add(laundryServiceLog);
            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id:Guid}")]
        public async Task<ActionResult> Put(Guid id, [FromBody] LaundryServiceLogCreationDTO laundryServiceLogCreationDTO)
        {
            var laundryServiceLog = await context.LaundryServiceLogs.FirstOrDefaultAsync(x => x.LaundryServiceLogId == id);
            if (laundryServiceLog == null)
            {
                return NotFound();
            }

            laundryServiceLog = mapper.Map(laundryServiceLogCreationDTO, laundryServiceLog);
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
        [AllowAnonymous]
        public async Task<ActionResult<List<LaundryServiceLogDTO>>> GetServiceLogsPostGet()
        {
            var laundryServiceLogs = await context.LaundryServiceLogs.ToListAsync();

            return mapper.Map<List<LaundryServiceLogDTO>>(laundryServiceLogs);
        }
    }
}
