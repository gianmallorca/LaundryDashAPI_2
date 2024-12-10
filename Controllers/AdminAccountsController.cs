using AutoMapper;
using LaundryDashAPI_2.DTOs;
using LaundryDashAPI_2.DTOs.AppUser;
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
                //new Claim ("email", userCredentials.Email)
                 new Claim(ClaimTypes.Email, userCredentials.Email) // Add the email claim
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




        //delete after
        [HttpPost("create")]
        public async Task<ActionResult<AuthenticationResponse>> Create([FromBody] ApplicationUserCredentials adminUserCredentials)
        {
            if (adminUserCredentials.Password != adminUserCredentials.ConfirmPassword)
            {
                return BadRequest("Password and Confirm Password do not match.");
            }
            // Create a new ApplicationUser with the provided credentials
            var user = new ApplicationUser
            {
                FirstName = adminUserCredentials.FirstName,
                LastName = adminUserCredentials.LastName,
                UserName = adminUserCredentials.Email,
                Email = adminUserCredentials.Email,
                UserType = "Admin",
                IsApproved = true, // Set default approval status to true
                Birthday = adminUserCredentials.Birthday,
                Age = adminUserCredentials.Age,
                Gender = adminUserCredentials.Gender,
                City = adminUserCredentials.City,
                Barangay = adminUserCredentials.Barangay,
                BrgyStreet = adminUserCredentials.BrgyStreet,
                PhoneNumber = adminUserCredentials.PhoneNumber
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


        //dislay user profile
        [HttpGet("GetUserProfile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AllAccounts")]
        public async Task<ActionResult<UserTypeDTO>> GetUserProfile()
        {
            // Get the email claim from the current user
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Step 2: Fetch the logged-in user
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Step 3: Retrieve user profile details (assuming these are in the ApplicationUser entity)
            var userProfile = await context.AppUsers
                .Where(u => u.Email == email)
                .Select(u => new ApplicationUserCredentials
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    UserType = context.Users.Where(x => x.Id == user.Id)
                            .Select(x => x.UserType).FirstOrDefault(),
                    Email = u.Email,
                    UserAddress = context.Users.Where(x => x.Id == user.Id)
                            .Select(x => $"{x.BrgyStreet}, {x.Barangay}, {x.City}").FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (userProfile == null)
            {
                return NotFound("User profile not found.");
            }

            return Ok(userProfile);
        }

        [HttpGet("GetUserType")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AllAccounts")]
        public async Task<ActionResult<UserTypeDTO>> GetUserType()
        {
            // Step 1: Get the email claim from the current user
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Step 2: Fetch the logged-in user
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Step 3: Retrieve the user type
            var userType = await context.Users
                .Where(u => u.Id == user.Id)
                .Select(u => new UserTypeDTO
                {
                    UserType = u.UserType,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName
                })
                .FirstOrDefaultAsync();

            if (userType == null)
            {
                return NotFound("User type not found.");
            }

            return Ok(userType);
        }

    }
}
