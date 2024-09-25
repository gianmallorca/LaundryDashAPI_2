using AutoMapper;
using LaundryDashAPI_2.DTOs;
using LaundryDashAPI_2.Entities;
using LaundryDashAPI_2.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LaundryDashAPI_2.Controllers
{

    [Route("api/adminAccounts")]
    [ApiController]

    public class AdminAccountsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public AdminAccountsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, ApplicationDbContext context, IMapper mapper)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.context = context;
            this.mapper = mapper;
        }
        //Token
        private async Task<AuthenticationResponse> BuildToken(ApplicationUserCredentials userCredentials, ApplicationUser user)
        {
            var claims = new List<Claim>()
            {
                new Claim ("email", userCredentials.Email)
            };

            // Add claims from AspNetUserClaims
            var userClaims = await userManager.GetClaimsAsync(user);
            claims.AddRange(userClaims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiration = DateTime.UtcNow.AddYears(1);

            var token = new JwtSecurityToken(issuer: null, audience: null, claims: claims, expires: expiration, signingCredentials: creds);

            return new AuthenticationResponse()
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = expiration
            };
        }

        //[HttpPost("create")]
        //public async Task<ActionResult<AuthenticationResponse>> Create([FromBody] ApplicationUserCredentials adminUserCredentials)
        //{
        //    // Create a new ApplicationUser with the provided credentials
        //    var user = new ApplicationUser
        //    {
        //        FirstName = adminUserCredentials.FirstName,
        //        LastName = adminUserCredentials.LastName,
        //        UserName = adminUserCredentials.Email,
        //        Email = adminUserCredentials.Email,
        //        UserType = "Admin",
        //        IsApproved = true // Set default approval status to true
        //    };

        //    // Attempt to create the user
        //    var result = await userManager.CreateAsync(user, adminUserCredentials.Password);

        //    if (result.Succeeded)
        //    {
        //        // Generate and return a token for the created user
        //        return await BuildToken(adminUserCredentials, user);
        //        await userManager.AddClaimAsync(user, new Claim("role", "admin"));
        //    }
        //    else
        //    {

        //        // Return the errors if user creation failed
        //        return BadRequest(result.Errors);
        //    }
        //}



        //delete after
        [HttpPost("create")]
        public async Task<ActionResult<AuthenticationResponse>> Create([FromBody] ApplicationUserCredentials adminUserCredentials)
        {
            // Create a new ApplicationUser with the provided credentials
            var user = new ApplicationUser
            {
                FirstName = adminUserCredentials.FirstName,
                LastName = adminUserCredentials.LastName,
                UserName = adminUserCredentials.Email,
                Email = adminUserCredentials.Email,
                UserType = "Admin",
                IsApproved = true // Set default approval status to true
            };

            // Attempt to create the user
            var result = await userManager.CreateAsync(user, adminUserCredentials.Password);

            if (result.Succeeded)
            {
                // Add the claim before generating the token
                var claimResult = await userManager.AddClaimAsync(user, new Claim("role", "admin"));

                if (!claimResult.Succeeded)
                {
                    return BadRequest(claimResult.Errors); // Handle any errors with adding the claim
                }

                // Generate and return a token for the created user
                return await BuildToken(adminUserCredentials, user);
            }
            else
            {
                // Return the errors if user creation failed
                return BadRequest(result.Errors);
            }
        }




        [HttpPost("login")]
        public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] ApplicationUserLogin login)
        {
            var result = await signInManager.PasswordSignInAsync(login.Email, login.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await userManager.FindByEmailAsync(login.Email) as ApplicationUser;

                if (user != null)
                {
                    var userCredentials = new ApplicationUserCredentials
                    {
                        Email = login.Email,
                        Password = login.Password
                    };

                    return await BuildToken(userCredentials, user);
                }
                else
                {
                    return NotFound("User not found.");
                }
            }
            else
            {
                return BadRequest("Incorrect Login");
            }
        }



        [HttpGet("listUsers")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
        public async Task<ActionResult<List<ApplicationUserDTO>>> GetListUsers([FromQuery] PaginationDTO paginationDTO)
        {
            var queryable = context.Users.AsQueryable();
            await HttpContext.InsertParametersPaginationInHeader(queryable);
            var users = await queryable.OrderBy(x => x.Email).Paginate(paginationDTO).ToListAsync();
            return mapper.Map<List<ApplicationUserDTO>>(users);
        }

        [HttpPost("makeAdmin")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
        public async Task<ActionResult> MakeAdmin([FromBody] string userId)
        {
            var user = await userManager.FindByIdAsync(userId);

            await userManager.AddClaimAsync(user, new Claim("role", "admin"));

            return NoContent();


        }
        [HttpPost("removeAdmin")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
        public async Task<ActionResult> RemoveAdmin([FromBody] string userId)
        {
            var user = await userManager.FindByIdAsync(userId);

            await userManager.RemoveClaimAsync(user, new Claim("role", "admin"));

            return NoContent();

        }
    }
}
