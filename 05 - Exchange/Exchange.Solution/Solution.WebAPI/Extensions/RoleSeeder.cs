using Microsoft.AspNetCore.Identity;
using Solution.Database.Enums;

namespace Solution.WebAPI.Extensions;

public static class RoleSeeder
{
    public static async Task SeedRolesAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        
        var roles = Enum.GetValues<UserRole>();
        
        foreach (var role in roles)
        {
            var roleName = role.ToString();
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }
    }
}