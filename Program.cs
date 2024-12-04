using Hangfire;
using LaundryDashAPI_2;
using LaundryDashAPI_2.APIBehavior;
using LaundryDashAPI_2.Entities;
using LaundryDashAPI_2.Filters;
using LaundryDashAPI_2.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LaundryDashAPI_2
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            // Configure database context
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            );

            // Configure Hangfire
            builder.Services.AddHangfire(config =>
                config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"))); // Use your database connection string
            builder.Services.AddHangfireServer(); // Add Hangfire server here, as recommended
                                                  // Add BookingService as a dependency
            builder.Services.AddScoped<BookingLog>();

            // Configure controllers
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add(typeof(MyExceptionFilter));
                options.Filters.Add(typeof(ParseBadRequest));
            }).ConfigureApiBehaviorOptions(BadRequestBehavior.Parse);

            // Configure JWT authentication
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"])),
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Configure authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("IsAdmin", policy => policy.RequireClaim(ClaimTypes.Role, "admin"));
                options.AddPolicy("IsLaundryShopAccount", policy => policy.RequireClaim(ClaimTypes.Role, "laundryShopAccount"));
                options.AddPolicy("IsRiderAccount", policy => policy.RequireClaim(ClaimTypes.Role, "riderAccount"));
                options.AddPolicy("IsClientAccount", policy => policy.RequireClaim(ClaimTypes.Role, "clientAccount"));

                options.AddPolicy("IsAdminOrLaundryShopAccount", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c => c.Type == ClaimTypes.Role &&
                            (c.Value == "admin" || c.Value == "laundryShopAccount"))));

                options.AddPolicy("IsAdminOrLaundryShopAccountOrClientAccount", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c => c.Type == ClaimTypes.Role &&
                            (c.Value == "admin" || c.Value == "laundryShopAccount" || c.Value == "clientAccount"))));

                options.AddPolicy("IsAdminOrLaundryShopAccountOrRiderAccount", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c => c.Type == ClaimTypes.Role &&
                            (c.Value == "admin" || c.Value == "laundryShopAccount" || c.Value == "riderAccount"))));
            });

            // Configure Swagger for API documentation
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

                // Add security definition for JWT
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Please enter JWT token in the format **Bearer {token}**",
                });

                // Add security requirement
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Add other services
            builder.Services.AddAutoMapper(typeof(Program));
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Build the application
            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Configure Hangfire Dashboard (for monitoring jobs)
            app.UseHangfireDashboard(); // No longer using `app.UseHangfireServer()`

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors("AllowSpecificOrigin");

            app.MapControllers();

            app.Run();
        }
    }
}
