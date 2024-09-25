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
    [Route("api/services")]
    [ApiController]
    
    public class ServicesController : Controller
    {
        private readonly ILogger<ServicesController> logger;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public ServicesController(ILogger<ServicesController> logger, ApplicationDbContext context, IMapper mapper)
        {
            this.logger = logger;
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet("getServices")]
        public async Task<ActionResult<List<ServiceDTO>>> Get([FromQuery] PaginationDTO paginationDTO)
        {
            var queryable = context.Services.AsQueryable();
            await HttpContext.InsertParametersPaginationInHeader(queryable);

            var services = await queryable.OrderBy(x => x.ServiceName).Paginate(paginationDTO).ToListAsync();

            return mapper.Map<List<ServiceDTO>>(services);
        }

        [HttpGet("{Id:Guid}", Name = "getServiceById")]
        public async Task<ActionResult<ServiceDTO>> Get(Guid id)
        {
            var service = await context.Services.FirstOrDefaultAsync(x => x.ServiceId == id);

            if (service == null)
            {
                return NotFound();
            }

            return mapper.Map<ServiceDTO>(service);
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] ServiceCreationDTO serviceCreationDTO)
        {
            var service = mapper.Map<Service>(serviceCreationDTO);
            context.Add(service);
            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id:Guid}", Name = "editService")]
        public async Task<ActionResult> Put(Guid id, [FromBody] ServiceCreationDTO serviceCreationDTO)
        {
            var service = await context.Services.FirstOrDefaultAsync(x => x.ServiceId == id);
            if (service == null)
            {
                return NotFound();
            }

            service = mapper.Map(serviceCreationDTO, service);
            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:Guid}", Name = "deleteService")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var exists = await context.Services.AnyAsync(x => x.ServiceId == id);

            if (!exists)
            {
                return NotFound();
            }

            context.Remove(new Service() { ServiceId = id });
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<ServiceDTO>>> GetServicesPostGet()
        {
            var services = await context.Services.ToListAsync();

            return mapper.Map<List<ServiceDTO>>(services);
        }
    }
}
