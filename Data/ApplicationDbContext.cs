
using LaundryDashAPI_2.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace LaundryDashAPI_2
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<LaundryShop> LaundryShops { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<LaundryServiceLog> LaundryServiceLogs { get; set; }


        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{

        //    // Configure the one-to-many relationship
        //    modelBuilder.Entity<DeviceModel>()
        //        .HasOne(dm => dm.Category)
        //        .WithMany(c => c.DeviceModels)
        //        .HasForeignKey(dm => dm.CategoryId);

        //    base.OnModelCreating(modelBuilder);


        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Call the base method to include the default Identity configurations
            base.OnModelCreating(modelBuilder);

            

            // Configure primary keys for Identity entities if necessary
            modelBuilder.Entity<IdentityUserLogin<string>>()
                .HasKey(login => new { login.LoginProvider, login.ProviderKey });

            modelBuilder.Entity<IdentityUserRole<string>>()
                .HasKey(role => new { role.UserId, role.RoleId });

            modelBuilder.Entity<IdentityUserToken<string>>()
                .HasKey(token => new { token.UserId, token.LoginProvider, token.Name });

            // Add additional configurations for other Identity entities as necessary
        }
    }

}
