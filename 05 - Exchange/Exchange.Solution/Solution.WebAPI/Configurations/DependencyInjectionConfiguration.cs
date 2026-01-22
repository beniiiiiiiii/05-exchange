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
            builder.Services.AddTransient<IUserService, UserService>();

            return builder;
        }
    }
}
