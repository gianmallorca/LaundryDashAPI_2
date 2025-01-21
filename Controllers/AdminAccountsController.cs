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
        private readonly IFileStorageService fileStorageService;
        private readonly string containerName = "LaundryShopImages";

        public AdminAccountsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IFileStorageService fileStorageService, IConfiguration configuration, ApplicationDbContext context, IMapper mapper)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.context = context;
            this.mapper = mapper;
            this.fileStorageService = fileStorageService;
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

        [HttpGet("getUserDetailsById/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AllAccounts")]
        public async Task<ActionResult> GetUserById([FromRoute] Guid id)
        {
            // Retrieve the authenticated user's email
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Retrieve the user by ID
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == id.ToString() && u.Email == email);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Map the user to the ApplicationUserCredentials DTO
            var userDetails = new ApplicationUserCredentials
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Birthday = user.Birthday,
                Gender = user.Gender,
                City = user.City,
                Barangay = user.Barangay,
                BrgyStreet = user.BrgyStreet,
                UserType = user.UserType,
                PhoneNumber = user.PhoneNumber,
            }; // Missing semicolon added here

            //if (user.UserType == "LaundryShopAccount")
            //{
            //    userDetails.TaxIdentificationNumber = user.TaxIdentificationNumber ?? null;
            //    userDetails.BusinessPermitNumber = user.BusinessPermitNumber ?? null;
            //}

            //if (user.UserType == "RiderAccount")
            //{
            //    userDetails.VehicleType = user.VehicleType ?? null;
            //    userDetails.VehicleCapacity = user.VehicleCapacity ?? null;
            //    userDetails.DriversLicenseNumber = user.DriversLicenseNumber ?? null;
            //}

            return Ok(userDetails);
        }


        // Return the user details





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
                    Id = u.Id,
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

        //update user details
        [HttpPut("UpdateUserDetails/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AllAccounts")]
        public async Task<ActionResult> UpdateUserDetails(Guid id, [FromBody] ApplicationUserCredentials adminUserCredentials)
        {
            // Find the existing user by ID
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Update common user details

            var appUser = new ApplicationUser
            {
                FirstName = adminUserCredentials.FirstName,
                LastName = adminUserCredentials.LastName,
                UserName = adminUserCredentials.Email,
                Email = adminUserCredentials.Email,
                Birthday = adminUserCredentials.Birthday,
                Age = adminUserCredentials.Age,
                Gender = adminUserCredentials.Gender,
                City = adminUserCredentials.City,
                Barangay = adminUserCredentials.Barangay,
                BrgyStreet = adminUserCredentials.BrgyStreet,
                PhoneNumber = adminUserCredentials.PhoneNumber
            };

            // Update specific properties based on user type
            if (adminUserCredentials.UserType == "LaundryShopAccount")
            {
                appUser.BusinessPermitsOfOwner = await fileStorageService.SaveFile(containerName, adminUserCredentials.BusinessPermitsOfOwner);
            }
            else if (adminUserCredentials.UserType == "RiderAccount")
            {
                appUser.VehicleType = adminUserCredentials.VehicleType;
                appUser.VehicleCapacity = adminUserCredentials.VehicleCapacity;
                appUser.DriversLicense = await fileStorageService.SaveFile(containerName, adminUserCredentials.DriversLicense);
            }

            // Update approval status and user type


            // Save changes to the user
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors); // Return update errors if any
            }

            // Return a success status code (No Content)
            return NoContent(); // Indicating that the update was successful with no response body
        }




        //view user details for approval
        [HttpGet("ViewUserDetailsById")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
        public async Task<ActionResult<ApplicationUserCredentials>> ViewUserDetails(string id)
        {
            // Step 1: Get the email claim from the current user
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("User email claim is missing.");
            }

            // Step 2: Fetch the logged-in user
            var loggedInUser = await userManager.FindByEmailAsync(email);
            if (loggedInUser == null)
            {
                return NotFound("Logged-in user not found.");
            }

            // Step 3: Retrieve the user details by the given ID
            var userDetails = await context.Users
                .Where(u => !u.IsApproved
                            && (u.UserType == "LaundryShopAccount" || u.UserType == "RiderAccount")
                            && u.Id == id) // Filter by approval, user type, and user ID
                .Select(u => new ApplicationUserCredentials
                {
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber,
                    UserAge = u.Age, // Directly retrieve Age
                    UserAddress = $"{u.BrgyStreet}, {u.Barangay}, {u.City}", // Format address inline
                    VehicleType = u.UserType == "RiderAccount" ? u.VehicleType : null, // Conditional inclusion for RiderAccount
                    VehicleCapacity = u.UserType == "RiderAccount" ? u.VehicleCapacity : null // Conditional inclusion for RiderAccount
                })
                .FirstOrDefaultAsync();

            if (userDetails == null)
            {
                return NotFound("User details not found.");
            }

            return Ok(userDetails);
        }


        [HttpGet("GetPdfByUserId/{userId}")]
        public async Task<IActionResult> GetPdfByUserId(string userId)
        {
            // Define the folder path where PDF files are stored
            var folderPath = @"C:\Users\ADMIN\Desktop\LaundryDash API New\LaundryShopImages";

            // Check if the folder exists
            if (!Directory.Exists(folderPath))
            {
                return NotFound("PDF folder does not exist.");
            }

            // Retrieve the user from the database by the given UserId
            var user = await userManager.FindByIdAsync(userId);

            // Check if the user exists
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Determine the file name based on the UserType
            string pdfFileName = user.UserType switch
            {
                "RiderAccount" => user.DriversLicense, // Get DriversLicense for RiderAccount
                "LaundryShopAccount" => user.BusinessPermitsOfOwner, // Get BusinessPermitsOfOwner for LaundryShopAccount
                _ => null // Handle cases where UserType does not match expected values
            };

            // Ensure the file name is not null or empty
            if (string.IsNullOrEmpty(pdfFileName))
            {
                return NotFound($"No PDF document associated with this user for user type '{user.UserType}'.");
            }

            // Build the full file path by combining the folder path and PDF file name
            var filePath = Path.Combine(folderPath, pdfFileName);

            // Check if the file exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("PDF file not found.");
            }

            // Read the PDF file bytes
            var pdfBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // Set the MIME type for PDF files
            const string contentType = "application/pdf";

            // Return the PDF as a file response with the appropriate MIME type and force download
            return File(pdfBytes, contentType, pdfFileName);
        }


    }
}
