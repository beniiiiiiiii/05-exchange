namespace Solution.DesktopApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute("dashboard", typeof(DashboardPage));
        Routing.RegisterRoute("transactions", typeof(TransactionsPage));
        Routing.RegisterRoute("exchangerates", typeof(ExchangeRatesPage));
    }
}
