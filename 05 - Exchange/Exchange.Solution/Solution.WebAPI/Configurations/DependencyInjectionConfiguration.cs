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

            builder.Services.AddScoped<ISecurityService, SecurityService>();
            builder.Services.AddScoped<IUserManagementService, UserManagementService>();
            builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
            builder.Services.AddScoped<ICurrentUserProvider, WebCurrentUserProvider>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<IStatisticsService, StatisticsService>();

            return builder;
        }
    }
}
