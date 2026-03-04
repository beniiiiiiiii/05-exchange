namespace Solution.DesktopApp.ViewModels;

public partial class ExchangeRateViewModel : BaseViewModel
{
    private readonly IExchangeRateService _exchangeRateService;

    [ObservableProperty] private ObservableCollection<ExchangeRateResponse> exchangeRates = new();
    [ObservableProperty] private decimal buyRate;
    [ObservableProperty] private decimal sellRate;
    [ObservableProperty] private Currency selectedCurrency = Currency.USD;
    [ObservableProperty] private DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Now);
    [ObservableProperty] private bool showCreateForm;

    public Array CurrencyOptions => Enum.GetValues(typeof(Currency));

    public ExchangeRateViewModel(IExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
        Title = "Exchange Rates";
    }

    [RelayCommand]
    public async Task LoadExchangeRatesAsync()
    {
        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            var result = await _exchangeRateService.GetExchangeRatesAsync(null);
            
            if (result.IsError)
            {
                SetError(result.FirstError.Description);
                return;
            }

            ExchangeRates.Clear();
            foreach (var rate in result.Value)
                ExchangeRates.Add(rate);
        }
        catch (Exception ex)
        {
            SetError($"Error loading rates: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CreateRateAsync()
    {
        if (!ValidateRate())
            return;

        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            var request = new CreateExchangeRatesRequest
            {
                Currency = SelectedCurrency,
                BuyRate = BuyRate,
                SellRate = SellRate,
                Date = SelectedDate
            };

            var result = await _exchangeRateService.CreateExchangeRateAsync(request);
            
            if (result.IsError)
            {
                SetError(result.FirstError.Description);
                return;
            }

            ExchangeRates.Insert(0, result.Value);
            ResetForm();
            ShowCreateForm = false;
        }
        catch (Exception ex)
        {
            SetError($"Error creating rate: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void ToggleCreateForm()
    {
        ShowCreateForm = !ShowCreateForm;
        if (!ShowCreateForm)
            ResetForm();
    }

    private bool ValidateRate()
    {
        if (BuyRate <= 0)
        {
            SetError("Buy rate must be greater than 0");
            return false;
        }

        if (SellRate <= 0)
        {
            SetError("Sell rate must be greater than 0");
            return false;
        }

        return true;
    }

    private void ResetForm()
    {
        BuyRate = 0;
        SellRate = 0;
        SelectedCurrency = Currency.USD;
        SelectedDate = DateOnly.FromDateTime(DateTime.Now);
    }
}
