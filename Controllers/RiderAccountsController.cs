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

        private async Task<AuthenticationResponse> BuildToken(ApplicationUserCredentials riderUserCredentials, ApplicationUser user)
        {
            var claims = new List<Claim>()
            {
                //new Claim ("email", userCredentials.Email)
                 new Claim(ClaimTypes.Email, riderUserCredentials.Email) // Add the email claim
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
        //[HttpPost("create")]
        //public async Task<ActionResult<AuthenticationResponse>> Create([FromBody] ApplicationUserCredentials riderUserCredentials)
        //{
        //    // Create a new LaundryShopUser with the provided credentials
        //    var user = new ApplicationUser
        //    {
        //        FirstName = riderUserCredentials.FirstName, 
        //        LastName = riderUserCredentials.LastName,
        //        UserName = riderUserCredentials.Email,
        //        Email = riderUserCredentials.Email,
        //        UserType = "RiderAccount",
        //        IsApproved = false
        //    };

        //    // Attempt to create the user
        //    var result = await userManager.CreateAsync(user, riderUserCredentials.Password);

        //    if (result.Succeeded)
        //    {
        //        // Add the claim before generating the token
        //        var claimResult = await userManager.AddClaimAsync(user, new Claim("role", "riderAccount"));

        //        if (!claimResult.Succeeded)
        //        {
        //            return BadRequest(claimResult.Errors); // Handle any errors with adding the claim
        //        }

        //        // Generate and return a token for the created user
        //        return await BuildToken(riderUserCredentials, user);
        //    }
        //    else
        //    {
        //        // Return the errors if user creation failed
        //        return BadRequest(result.Errors);
        //    }
        //}

        [HttpPost("create")]
        public async Task<ActionResult> Create([FromBody] ApplicationUserCredentials riderUserCredentials)
        {
            // Create a new ApplicationUser with the provided credentials
            var user = new ApplicationUser
            {
                FirstName = riderUserCredentials.FirstName,
                LastName = riderUserCredentials.LastName,
                UserName = riderUserCredentials.Email,
                Email = riderUserCredentials.Email,
                UserType = "RiderAccount",
                IsApproved = false // User is not approved by default
            };

            // Attempt to create the user
            var result = await userManager.CreateAsync(user, riderUserCredentials.Password);

            if (result.Succeeded)
            {
                // Do not issue a claim or token yet since the user is not approved
                return Ok(new { Message = "User created successfully, but approval is pending." });
            }
            else
            {
                // Return the errors if user creation failed
                return BadRequest(result.Errors);
            }
        }



        //[HttpPost("login")]
        //public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] ApplicationUserLogin login)
        //{
        //    // Attempt to sign in the user with the provided credentials
        //    var result = await signInManager.PasswordSignInAsync(login.Email, login.Password, isPersistent: false, lockoutOnFailure: false);

        //    if (result.Succeeded)
        //    {
        //        // Find the user by their email
        //        var user = await userManager.FindByEmailAsync(login.Email) as ApplicationUser;

        //        // Check if the user is approved
        //        if (user != null && user.IsApproved == true)
        //        {
        //            // Convert LaundryShopUserLogin to LaundryShopUserCredentials
        //            var userCredentials = new ApplicationUserCredentials
        //            {
        //                Email = login.Email,
        //                Password = login.Password
        //            };

        //            // Generate and return a token
        //            return await BuildToken(userCredentials, user);
        //        }
        //        else
        //        {
        //            // User is not approved
        //            return Unauthorized("User account is not approved.");
        //        }
        //    }
        //    else
        //    {
        //        // Login failed, return an error
        //        return BadRequest("Incorrect Login");
        //    }
        //}

        [HttpPost("login")]
        public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] ApplicationUserLogin login)
        {
            // Attempt to sign in the user with the provided credentials
            var result = await signInManager.PasswordSignInAsync(login.Email, login.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Find the user by their email
                var user = await userManager.FindByEmailAsync(login.Email) as ApplicationUser;

                if (user != null)
                {
                    // Check if the user is approved
                    if (user.IsApproved)
                    {
                        // Convert ApplicationUserLogin to ApplicationUserCredentials
                        var userCredentials = new ApplicationUserCredentials
                        {
                            Email = login.Email,
                            Password = login.Password
                        };

                        // Add the role claim if necessary
                        var claimResult = await userManager.AddClaimAsync(user, new Claim("role", "riderAccount"));
                        if (!claimResult.Succeeded)
                        {
                            return BadRequest("Failed to add claim.");
                        }

                        // Generate and return a token for the user
                        return await BuildToken(userCredentials, user);
                    }
                    else
                    {
                        // User is not approved, return unauthorized
                        return Unauthorized("User account is not approved.");
                    }
                }
                else
                {
                    // User not found
                    return NotFound("User not found.");
                }
            }
            else
            {
                // Login failed, return an error
                return BadRequest("Incorrect login credentials.");
            }
        }

        [HttpPut("approveRiderAccount/{id}")]
        public async Task<ActionResult> ApproveRiderAccount([FromBody] Guid id)
        {
            // Retrieve the user account by ID
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Check if the user is not already approved
            if (user.IsApproved)
            {
                return BadRequest("User account is already approved.");
            }

            // Set the user's IsApproved property to true
            user.IsApproved = true;

            // Update the user account in the database
            var result = await userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return NoContent(); // Successfully approved, return 204 No Content
            }
            else
            {
                return BadRequest(result.Errors); // Handle any errors that occurred during update
            }

            
        }

        [HttpGet("listUsers")]
        public async Task<ActionResult<List<ApplicationUserDTO>>> GetListUsers([FromQuery] PaginationDTO paginationDTO)
        {
            // Filter users where UserType equals 'RiderAccount'
            var queryable = context.Users
                .Where(x => x.UserType == "RiderAccount")  // Add a filter for UserType
                .AsQueryable();

            // Apply pagination headers
            await HttpContext.InsertParametersPaginationInHeader(queryable);

            // Order the results by Email and paginate
            var users = await queryable
                .OrderBy(x => x.Email)
                .Paginate(paginationDTO)
                .ToListAsync();

            // Map the result to a list of ApplicationUserDTO and return
            return mapper.Map<List<ApplicationUserDTO>>(users);
        }





    }
}
