namespace Solution.DesktopApp
{
    public partial class App : Application
    {
        private const string DefaultApiUrl = "https://localhost:7001";

        public App()
        {
            InitializeComponent();
        }

        protected override async void OnStart()
        {
            base.OnStart();

            // Initialize API Client
            var apiClient = IPlatformApplication.Current?.Services.GetService<IApiClient>();
            if (apiClient != null)
            {
                apiClient.SetBaseAddress(DefaultApiUrl);
                await apiClient.LoadStoredTokenAsync();
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
