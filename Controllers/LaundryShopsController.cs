using AutoMapper;

using LaundryDashAPI_2;
using LaundryDashAPI_2.DTOs;
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

        [HttpGet("getLaundryShop")]
        public async Task<ActionResult<List<LaundryShopDTO>>> Get([FromQuery] PaginationDTO paginationDTO)
        {
            var queryable = context.LaundryShops.AsQueryable();
            await HttpContext.InsertParametersPaginationInHeader(queryable);

            var laundryShops = await queryable.OrderBy(x => x.LaundryShopName).Paginate(paginationDTO).ToListAsync();

            return mapper.Map<List<LaundryShopDTO>>(laundryShops);


        }



        [HttpGet("getLaundryShopByUserAccountId")]
        public async Task<ActionResult<LaundryShopDTO>> GetByUserAccountId([FromBody] LaundryShopCreationDTO laundryShopCreationDTO)
        {
            // Get the user's email or ID from the claims
            // Retrieve the user's email from the claims
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Find the user by email
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Query the laundry shop added by the user, filtering only by AddedById
            var laundryShop = await context.LaundryShops
                .FirstOrDefaultAsync(x => x.AddedById == user.Id);

            if (laundryShop == null)
            {
                return NotFound("Laundry shop not found.");
            }

            // Return the mapped LaundryShopDTO
            return Ok(mapper.Map<LaundryShopDTO>(laundryShop));

        }



        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult> Post([FromBody] LaundryShopCreationDTO laundryShopCreationDTO)
        {
            if (laundryShopCreationDTO == null)
            {
                return BadRequest("Request body cannot be null.");
            }

            // Map the DTO to the entity
            var laundryShop = mapper.Map<Entities.LaundryShop>(laundryShopCreationDTO);

            // Retrieve the email from the current user's claims
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

            // Set the AddedById property and save the entity
            laundryShop.AddedById = user.Id;
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
