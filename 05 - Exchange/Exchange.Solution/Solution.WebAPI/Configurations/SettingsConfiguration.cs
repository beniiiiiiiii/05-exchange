namespace Solution.WebAPI.Configurations;

public static class SettingsConfiguration
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder LoadSettings()
        {
            builder.Services.Configure<JWTSettingsModel>(builder.Configuration.GetSection("JWT"));

            return builder;
        }
    }
}
