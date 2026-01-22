namespace Solution.WebAPI.Configurations;

public static class LoadEnvironmentVariablesConfiguration
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder LoadEnvironmentVariables()
        {
            builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                 .AddEnvironmentVariables();

            return builder;
        }
    }
}
