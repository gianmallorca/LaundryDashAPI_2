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
    [Route("api/laundryShops")]
    [ApiController]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsLaundryShopAccount")]
    public class LaundryShopsController : Controller
    {
        private readonly ILogger<LaundryShopsController> logger;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public LaundryShopsController(ILogger<LaundryShopsController> logger, ApplicationDbContext context, IMapper mapper)
        {
            this.logger = logger;
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<LaundryShopDTO>>> Get([FromQuery] PaginationDTO paginationDTO)
        {
            var queryable = context.LaundryShops.AsQueryable();
            await HttpContext.InsertParametersPaginationInHeader(queryable);

            var laundryShops = await queryable.OrderBy(x => x.LaundryShopName).Paginate(paginationDTO).ToListAsync();

            return mapper.Map<List<LaundryShopDTO>>(laundryShops);


        }

        [HttpGet("{Id:Guid}", Name = "getLaundryShop")]
        public async Task<ActionResult<LaundryShopDTO>> Get(Guid id)
        {
            var laundryShop = await context.LaundryShops.FirstOrDefaultAsync(x => x.LaundryShopId == id);

            if (laundryShop == null)
            {
                return NotFound();
            }

            return mapper.Map<LaundryShopDTO>(laundryShop);
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] LaundryShopCreationDTO laundryShopCreationDTO)
        {
            var laundryShop = mapper.Map<LaundryShop>(laundryShopCreationDTO);
            context.Add(laundryShop);
            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id:Guid}")]
        public async Task<ActionResult> Put(Guid id, [FromBody] LaundryShopCreationDTO laundryShopCreationDTO)
        {
            var laundryShop = await context.LaundryShops.FirstOrDefaultAsync(x => x.LaundryShopId == id);
            if (laundryShop == null)
            {
                return NotFound();
            }

            laundryShop =  mapper.Map(laundryShopCreationDTO, laundryShop);
            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:Guid}")]
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
        public async Task<ActionResult<List<LaundryShopDTO>>> GetCategoriesPostGet()
        {
            var laundryShops = await context.LaundryShops.ToListAsync();

            return mapper.Map<List<LaundryShopDTO>>(laundryShops);
        }

    }
}
