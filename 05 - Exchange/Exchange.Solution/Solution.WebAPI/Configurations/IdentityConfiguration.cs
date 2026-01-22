using Microsoft.AspNetCore.Identity;
using Solution.Domain.Database.Entities;

namespace Solution.WebAPI.Configurations;

public static class IdentityConfiguration
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder UseIdentity()
        {
            builder.Services.AddDefaultIdentity<UserEntity>(options =>
            {
                //Password
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;

                // Lockout

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User
                options.User.RequireUniqueEmail = true;

                // Sign-in
                options.SignIn.RequireConfirmedAccount = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
             .AddRoles<IdentityRole<Guid>>()
             .AddEntityFrameworkStores<ApplicationDbContext>();

            return builder;
        }
    }
}
