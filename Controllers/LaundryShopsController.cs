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
        private readonly IFileStorageService fileStorageService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly string containerName = "LaundryShopImages";

        public LaundryShopsController(ILogger<LaundryShopsController> logger, ApplicationDbContext context, IMapper mapper, IFileStorageService fileStorageService, UserManager<ApplicationUser> userManager)
        {
            this.logger = logger;
            this.context = context;
            this.mapper = mapper;
            this.fileStorageService = fileStorageService;
            this.userManager = userManager;
        }

        //[HttpGet("getLaundryShop")]
        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccountOrClientAccount")]
        //public async Task<ActionResult<List<LaundryShopDTO>>> Get([FromQuery] PaginationDTO paginationDTO)
        //{
        //    var email = User.FindFirst(ClaimTypes.Email)?.Value;

        //    // Check if the email is null or empty
        //    if (string.IsNullOrEmpty(email))
        //    {
        //        return BadRequest("User email claim is missing.");
        //    }


        //    var queryable = context.LaundryShops.AsQueryable();
        //    queryable = queryable.Where(x => x.IsVerifiedByAdmin == true);
        //    await HttpContext.InsertParametersPaginationInHeader(queryable);

        //    var laundryShops = await queryable.OrderBy(x => x.LaundryShopName).Paginate(paginationDTO).ToListAsync();

        //    return mapper.Map<List<LaundryShopDTO>>(laundryShops);


        //}

        [HttpGet("getLaundryShop")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccountOrClientAccount")]
        public async Task<ActionResult<List<LaundryShopDTO>>> Get([FromQuery] PaginationDTO paginationDTO)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Query verified laundry shops
            var queryable = context.LaundryShops.AsQueryable();
            queryable = queryable.Where(x => x.IsVerifiedByAdmin == true);
            await HttpContext.InsertParametersPaginationInHeader(queryable);

            // Fetch and map laundry shops
            var laundryShops = await queryable
                .OrderBy(x => x.LaundryShopName)
                .Paginate(paginationDTO)
                .Select(x => new LaundryShopDTO
                {
                    LaundryShopId = x.LaundryShopId,
                    LaundryShopName = x.LaundryShopName,
                    LaundryShopPicture = x.LaundryShopPicture
                })
                .ToListAsync();

            return Ok(laundryShops);
        }


        //gets laundry shop picture
        [HttpGet("images/{id}")]
        public async Task<IActionResult> GetImage(string id)
        {
            // Retrieve the LaundryShop record
            var shop = await context.LaundryShops
                .Where(x => x.LaundryShopPicture == id)
                .FirstOrDefaultAsync();

            if (shop == null)
            {
                return NotFound("Laundry shop or picture not found.");
            }

            // Construct the file path
            var imagePath = Path.Combine("LaundryShopImages", shop.LaundryShopPicture);
            if (!System.IO.File.Exists(imagePath))
            {
                return NotFound("Image not found.");
            }

            var image = System.IO.File.OpenRead(imagePath);
            return File(image, "image/jpeg"); // Adjust content type as needed
        }

/// <summary>
/// ////////////////////////////////////////////////////
/// </summary>
/// <returns></returns>


        [HttpGet("getLaundryShopByUserId")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult<List<LaundryShopDTO>>> GetByUserAccountId()
        {
            // Get the user's email from the claims
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

            // Query the laundry shops added by the user, filtering only by AddedById
            var laundryShops = await context.LaundryShops
            .Where(x => x.AddedById == user.Id && x.IsVerifiedByAdmin == true)
            .ToListAsync();

            if (laundryShops == null || !laundryShops.Any())
            {
                return NotFound("No laundry shops found.");
            }

            // Return the mapped LaundryShopDTO list
            return Ok(mapper.Map<List<LaundryShopDTO>>(laundryShops));
        }



        

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccount")]
        public async Task<ActionResult> Post([FromForm] LaundryShopCreationDTO laundryShopCreationDTO)
        {
            if (laundryShopCreationDTO == null)
            {
                return BadRequest("Request body cannot be null.");
            }

            var laundryShop = mapper.Map<Entities.LaundryShop>(laundryShopCreationDTO);
            laundryShop.IsVerifiedByAdmin = false;

            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            laundryShop.AddedById = user.Id;

          
            if (laundryShopCreationDTO.LaundryShopPicture != null)
            {
                laundryShop.LaundryShopPicture = await fileStorageService.SaveFile(containerName, laundryShopCreationDTO.LaundryShopPicture);
            }


            context.LaundryShops.Add(laundryShop);
            await context.SaveChangesAsync();

            return NoContent();
        }




        [HttpGet("getPendingLaundryShops")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
        public async Task<ActionResult<List<LaundryShopDTO>>> GetPendingLaundryShops([FromQuery] PaginationDTO paginationDTO)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email is null or empty
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var queryable = context.LaundryShops
                .Where(x => x.IsVerifiedByAdmin == false) // Only retrieve shops where IsApprovedByAdmin is false
                .AsQueryable();

            await HttpContext.InsertParametersPaginationInHeader(queryable);

            var laundryShops = await queryable
                .OrderBy(x => x.LaundryShopName) // You can modify sorting based on your needs
                .Paginate(paginationDTO)
                .ToListAsync();

            return mapper.Map<List<LaundryShopDTO>>(laundryShops);
        }


        [HttpPut("approveLaundryShop/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
        public async Task<ActionResult> ApproveLaundryShop(Guid id)
        {
            // Retrieve the LaundryShop based on the given id
            var laundryShop = await context.LaundryShops.FirstOrDefaultAsync(x => x.LaundryShopId == id);

            // Check if the LaundryShop exists
            if (laundryShop == null)
            {
                return NotFound("Laundry Shop not found.");
            }

            // Update the IsApprovedByAdmin property to true
            laundryShop.IsVerifiedByAdmin = true;

            // Save the changes to the database
            await context.SaveChangesAsync();

            // Return NoContent (successful update)
            return NoContent();
        }



        [HttpPut("{id:Guid}", Name = "editLaundryShop")]

        public async Task<ActionResult> Put(Guid id, [FromBody] LaundryShopCreationDTO laundryShopCreationDTO)
        {
            var laundryShop = await context.LaundryShops.FirstOrDefaultAsync(x => x.LaundryShopId == id);
            if (laundryShop == null)
            {
                return NotFound();
            }

            laundryShop = mapper.Map(laundryShopCreationDTO, laundryShop);
            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:Guid}", Name = "deleteLaundryShop")]

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

        public async Task<ActionResult<List<LaundryShopDTO>>> GetLaundryPostGet()
        {
            var laundryShops = await context.LaundryShops.ToListAsync();

            return mapper.Map<List<LaundryShopDTO>>(laundryShops);
        }

        [HttpGet("PostGetTwo")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<List<LaundryShopDTO>>> LaundryPostGet()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Check if the user is an admin
            var isAdmin = await userManager.IsInRoleAsync(user, "Admin");

            // If the user is an admin, return all laundry shops
            if (isAdmin)
            {
                var allLaundryShops = await context.LaundryShops.ToListAsync();
                return mapper.Map<List<LaundryShopDTO>>(allLaundryShops);
            }

            // If not an admin, return only the laundry shops added by the current user
            var laundryShops = await context.LaundryShops
            .Where(x => x.AddedById == user.Id && x.IsVerifiedByAdmin == true)
            .ToListAsync();

            return mapper.Map<List<LaundryShopDTO>>(laundryShops);
        }

        [HttpGet("getLaundryDetailsById/{id:Guid}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdminOrLaundryShopAccountOrClientAccount")]
        public async Task<ActionResult<LaundryShopDTO>> GetLaundryDetailsById(Guid id)
        {
            // Retrieve the user's email from the claims
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Ensure the email claim exists
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

            // Fetch the laundry shop with the matching ID and ensure it is verified by the admin
            var laundryShop = await context.LaundryShops
                .FirstOrDefaultAsync(x => x.LaundryShopId == id && x.IsVerifiedByAdmin);

            if (laundryShop == null)
            {
                return NotFound("Laundry shop not found or not verified by the admin.");
            }

            // Map and return the laundry shop details as a DTO
            return Ok(mapper.Map<LaundryShopDTO>(laundryShop));
        }



    }
}