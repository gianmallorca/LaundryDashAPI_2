using AutoMapper;

using LaundryDashAPI_2;
using LaundryDashAPI_2.DTOs;
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
  
    [Route("api/laundryShops")]
    [ApiController]
 
    public class LaundryShopsController : Controller
    {
        private readonly ILogger<LaundryShopsController> logger;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly UserManager<ApplicationUser> userManager;

        public LaundryShopsController(ILogger<LaundryShopsController> logger, ApplicationDbContext context, IMapper mapper, UserManager<ApplicationUser> userManager)
        {
            this.logger = logger;
            this.context = context;
            this.mapper = mapper;
            this.userManager = userManager;
        }

        [HttpGet("geLaundryShop")]
        public async Task<ActionResult<List<LaundryShopDTO>>> Get([FromQuery] PaginationDTO paginationDTO)
        {
            var queryable = context.LaundryShops.AsQueryable();
            await HttpContext.InsertParametersPaginationInHeader(queryable);

            var laundryShops = await queryable.OrderBy(x => x.LaundryShopName).Paginate(paginationDTO).ToListAsync();

            return mapper.Map<List<LaundryShopDTO>>(laundryShops);


        }

        [HttpGet("{Id:Guid}", Name = "getLaundryShopById")]
      
        public async Task<ActionResult<LaundryShopDTO>> Get(Guid id)
        {
            var laundryShop = await context.LaundryShops.FirstOrDefaultAsync(x => x.LaundryShopId == id);

            if (laundryShop == null)
            {
                return NotFound();
            }

            return mapper.Map<LaundryShopDTO>(laundryShop);
        }

        [HttpPost("createLaundryShop")]
     
        public async Task<ActionResult> Post([FromBody] LaundryShopCreationDTO laundryShopCreationDTO)
        {
            var laundryShop = mapper.Map<LaundryShop>(laundryShopCreationDTO);
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            var user = await userManager.FindByEmailAsync(email);


            laundryShop.AddedById = user.Id.ToString();
            context.Add(laundryShop);
            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id:Guid}", Name ="editLaundryShop")]
       
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

        [HttpDelete("{id:Guid}", Name ="deleteLaundryShop")]
        
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
