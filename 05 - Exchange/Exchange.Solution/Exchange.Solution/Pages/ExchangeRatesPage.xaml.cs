namespace Solution.DesktopApp.Pages;

public partial class ExchangeRatesPage : ContentPage
{
    private readonly ExchangeRateViewModel _viewModel;

    public ExchangeRatesPage(ExchangeRateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadExchangeRatesAsync();
    }
}
