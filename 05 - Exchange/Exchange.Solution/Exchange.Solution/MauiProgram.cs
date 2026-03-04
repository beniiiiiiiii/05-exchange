using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Solution.Services.Services;
using Solution.Database;
using Microsoft.AspNetCore.Http;

namespace Solution.DesktopApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);

            builder.Services.AddSingleton<ICurrentUserProvider, DesktopCurrentUserProvider>();

            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            var connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;Database=AuthDB;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseLazyLoadingProxies()
                .UseSqlServer(connectionString, opt =>
                {
                    opt.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    opt.EnableRetryOnFailure();
                    opt.CommandTimeout(300);
                }));

            // Services
            builder.Services.AddTransient<IStatisticsService, StatisticsService>();
            builder.Services.AddTransient<ITransactionService, TransactionService>();
            builder.Services.AddTransient<IExchangeRateService, ExchangeRateService>();

            // ViewModels
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<TransactionViewModel>();
            builder.Services.AddTransient<ExchangeRateViewModel>();

            // Pages
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<TransactionsPage>();
            builder.Services.AddTransient<ExchangeRatesPage>();

            return builder.Build();
        }
    }
}
