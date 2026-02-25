using Solution.Core.Interfaces.Services;
using Solution.Services;
using Solution.Services.Services;

namespace Solution.WebAPI.Configurations;

public static class DependencyInjectionConfiguration
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder ConfigureDI()
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();

            builder.Services.AddTransient<ISecurityService, SecurityService>();
            builder.Services.AddTransient<IUserManagementService, UserManagementService>();

            return builder;
        }
    }
}
