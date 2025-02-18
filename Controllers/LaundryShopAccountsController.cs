﻿using AutoMapper;
using LaundryDashAPI_2.DTOs;
using LaundryDashAPI_2.DTOs.AppUser;
using LaundryDashAPI_2.DTOs.LaundryShop;
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
    [Route("api/laundryShopAccounts")]
    public class LaundryShopAccountsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IFileStorageService fileStorageService;
        private readonly string containerName = "LaundryShopImages";


        public LaundryShopAccountsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IFileStorageService fileStorageService, IConfiguration configuration, ApplicationDbContext context, IMapper mapper)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.context = context;
            this.mapper = mapper;
            this.fileStorageService = fileStorageService;
        }

        private async Task<AuthenticationResponse> BuildToken(ApplicationUserCredentials laundryShopUserCredentials, ApplicationUser user)
        {
            var claims = new List<Claim>()
            {
                //new Claim ("email", userCredentials.Email)
                 new Claim(ClaimTypes.Email, laundryShopUserCredentials.Email) // Add the email claim
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
        [HttpPost("create")]
        public async Task<ActionResult> Create([FromForm] ApplicationUserCredentials laundryShopUserCredentials)
        {
            if (laundryShopUserCredentials.Password != laundryShopUserCredentials.ConfirmPassword)
            {
                return BadRequest("Password and Confirm Password do not match.");
            }
            // Create a new LaundryShopUser with the provided credentials
            var user = new ApplicationUser
            {
                FirstName = laundryShopUserCredentials.FirstName,
                LastName = laundryShopUserCredentials.LastName,
                UserName = laundryShopUserCredentials.Email,
                Email = laundryShopUserCredentials.Email,
                UserType = "LaundryShopAccount",
                IsApproved = false, // User is not approved by default
                Birthday = laundryShopUserCredentials.Birthday,
                Age = laundryShopUserCredentials.Age,
                Gender = laundryShopUserCredentials.Gender,
                City = laundryShopUserCredentials.City,
                Barangay = laundryShopUserCredentials.Barangay,
                BrgyStreet = laundryShopUserCredentials.BrgyStreet,
                PhoneNumber = laundryShopUserCredentials.PhoneNumber 
            };

            if (laundryShopUserCredentials.BusinessPermitsOfOwner != null)
            {
                user.BusinessPermitsOfOwner = await fileStorageService.SaveFile(containerName, laundryShopUserCredentials.BusinessPermitsOfOwner);
            }


            // Attempt to create the user
            var result = await userManager.CreateAsync(user, laundryShopUserCredentials.Password);

            if (result.Succeeded)
            {
                // Do not issue a claim or token yet since the user is not approved
                return Ok(new { Message = "Laundry shop user created successfully, but approval is pending." });
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

            if (!result.Succeeded)
            {
                return BadRequest("Incorrect login credentials.");
            }

            var user = await userManager.FindByEmailAsync(login.Email) as ApplicationUser;

            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!user.IsApproved)
            {
                return Unauthorized("User account is not approved.");
            }

            if (user.UserType != "LaundryShopAccount")
            {
                return Unauthorized("Incorrect User! Please login with a Laundry Shop Account.");
            }

            // Check if the "laundryShopAccount" role claim already exists
            var claims = await userManager.GetClaimsAsync(user);
            if (!claims.Any(c => c.Type == "role" && c.Value == "laundryShopAccount"))
            {
                var claimResult = await userManager.AddClaimAsync(user, new Claim("role", "laundryShopAccount"));
                if (!claimResult.Succeeded)
                {
                    return BadRequest("Failed to add claim.");
                }
            }

            // Build and return the token if all conditions are met
            var userCredentials = new ApplicationUserCredentials
            {
                Email = login.Email,
                Password = login.Password
            };
            return await BuildToken(userCredentials, user);
        }




       

        [HttpGet("listUsers")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
        public async Task<ActionResult<List<ApplicationUserDTO>>> GetListUsers([FromQuery] PaginationDTO paginationDTO)
        {
            // Filter users where UserType equals 'RiderAccount'
            var queryable = context.Users
             .Where(x => x.UserType == "LaundryShopAccount" && x.IsApproved == false)  // Add a filter for UserType and IsApproved
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
