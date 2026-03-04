using Microsoft.Extensions.Logging;
using Solution.Services.Services;

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

            // Logging
#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);

            // Database
            builder.Services.AddTransient<ApplicationDbContext>();

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
