using Solution.Core.Models.Response;

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
            var result = await _exchangeRateService.GetRatesHistoryAsync(null, null);

            if (result.IsError)
            {
                SetError(result.FirstError.Description);
                return;
            }

            ExchangeRates.Clear();

            var ratesResponse = result.Value; // ExchangeRatesResponse
            if (ratesResponse != null && ratesResponse.Rates != null)
            {
                foreach (var rate in ratesResponse.Rates)
                    ExchangeRates.Add(rate);
            }
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
            var request = BuildExchangeRateRequest();

            var result = await _exchangeRateService.CreateDailyRatesAsync(request);

            if (result.IsError)
            {
                SetError(result.FirstError.Description);
                return;
            }

            var multi = result.Value;
            if (multi != null && multi.Rates != null && multi.Rates.Count > 0)
            {
                foreach (var rate in multi.Rates)
                    ExchangeRates.Add(rate);
            }
            else
            {
                SetError("Unexpected response from service when creating rates.");
                return;
            }

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

    private CreateExchangeRatesRequest BuildExchangeRateRequest()
    {
        var request = new CreateExchangeRatesRequest { Date = SelectedDate };

        switch (SelectedCurrency)
        {
            case Currency.USD:
                request.UsdBuyRate = BuyRate;
                request.UsdSellRate = SellRate;
                break;
            case Currency.GBP:
                request.GbpBuyRate = BuyRate;
                request.GbpSellRate = SellRate;
                break;
            case Currency.CHF:
                request.ChfBuyRate = BuyRate;
                request.ChfSellRate = SellRate;
                break;
        }

        return request;
    }

    private void ResetForm()
    {
        BuyRate = 0;
        SellRate = 0;
        SelectedCurrency = Currency.USD;
        SelectedDate = DateOnly.FromDateTime(DateTime.Now);
    }
}
