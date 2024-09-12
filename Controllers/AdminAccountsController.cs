using AutoMapper;
using LaundryDashAPI_2.DTOs;
using LaundryDashAPI_2.Entities;
using LaundryDashAPI_2.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LaundryDashAPI_2.Controllers
{
    [ApiController]
    [Route("api/adminAccounts")]
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
                new Claim ("role", "admin")
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

        [HttpPost("create")]
        public async Task<ActionResult<AuthenticationResponse>> Create([FromBody] ApplicationUserCredentials adminUserCredentials)
        {
            // Create a new LaundryShopUser with the provided credentials
            var user = new ApplicationUser
            {
                FirstName = adminUserCredentials.FirstName,
                LastName = adminUserCredentials.LastName,
                UserName = adminUserCredentials.Email,
                Email = adminUserCredentials.Email,
                IsApproved = true // Set default approval status to false
            };

            // Attempt to create the user
            var result = await userManager.CreateAsync(user, adminUserCredentials.Password);

            if (result.Succeeded)
            {
                // Find the created user to check the IsApproved status
                var createdUser = await userManager.FindByEmailAsync(adminUserCredentials.Email) as ApplicationUser;

                // Check if the user is approved
                if (createdUser != null && createdUser.IsApproved == true)
                {
                    // User is approved, generate and return a token
                    return await BuildToken(adminUserCredentials, user);
                }
                else
                {
                    // User is not approved
                    return Unauthorized("User account is not approved.");
                }
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
            // Attempt to sign in the user with the provided credentials
            var result = await signInManager.PasswordSignInAsync(login.Email, login.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Find the user by their email
                var user = await userManager.FindByEmailAsync(login.Email) as ApplicationUser;

                // Check if the user is approved
                if (user != null && user.IsApproved == true)
                {
                    // Convert LaundryShopUserLogin to LaundryShopUserCredentials
                    var userCredentials = new ApplicationUserCredentials
                    {
                        Email = login.Email,
                        Password = login.Password
                    };

                    // Generate and return a token
                    return await BuildToken(userCredentials, user);
                }
                else
                {
                    // User is not approved
                    return Unauthorized("User account is not approved.");
                }
            }
            else
            {
                // Login failed, return an error
                return BadRequest("Incorrect Login");
            }
        }

        [HttpGet("listUsers")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
        public async Task<ActionResult<List<UserDTO>>> GetListUsers([FromQuery] PaginationDTO paginationDTO)
        {
            var queryable = context.Users.AsQueryable();
            await HttpContext.InsertParametersPaginationInHeader(queryable);
            var users = await queryable.OrderBy(x => x.Email).Paginate(paginationDTO).ToListAsync();
            return mapper.Map<List<UserDTO>>(users);
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
