using AutoMapper;
using LaundryDashAPI_2.DTOs;
using LaundryDashAPI_2.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LaundryDashAPI_2.Controllers
{
    [ApiController]
    [Route("api/riderAccounts")]
    public class RiderAccountsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public RiderAccountsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, ApplicationDbContext context, IMapper mapper)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.context = context;
            this.mapper = mapper;
        }

        private async Task<AuthenticationResponse> BuildToken(ApplicationUserCredentials laundryShopUserCredentials, ApplicationUser user)
        {
            var claims = new List<Claim>()
            {
                new Claim ("role", "riderAccount")
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
        //fix
        [HttpPost("createRiderAccount")]
        public async Task<ActionResult<AuthenticationResponse>> Create([FromBody] ApplicationUserCredentials laundryShopUserCredentials)
        {
            // Create a new LaundryShopUser with the provided credentials
            var user = new ApplicationUser
            {
                FirstName = laundryShopUserCredentials.FirstName, 
                LastName = laundryShopUserCredentials.LastName,
                UserName = laundryShopUserCredentials.Email,
                Email = laundryShopUserCredentials.Email,
                UserType = "RiderAccount",
                IsApproved = false
            };

            // Attempt to create the user
            var result = await userManager.CreateAsync(user, laundryShopUserCredentials.Password);

            if (result.Succeeded)
            {
                // Find the created user to check the IsApproved status
                var createdUser = await userManager.FindByEmailAsync(laundryShopUserCredentials.Email) as ApplicationUser;

                // Check if the user is approved
                if (createdUser != null && createdUser.IsApproved == true)
                {
                    // User is approved, generate and return a token
                    var claimResult = await userManager.AddClaimAsync(user, new Claim("role", "riderAccount"));
                    return await BuildToken(laundryShopUserCredentials, user);
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


        [HttpPost("loginRiderAccount")]
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


    }
}
