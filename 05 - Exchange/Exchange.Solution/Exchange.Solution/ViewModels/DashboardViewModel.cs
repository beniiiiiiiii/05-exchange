using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Solution.Common;

namespace Solution.DesktopApp.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IStatisticsService _statisticsService;

    [ObservableProperty] private int totalTransactionsToday;
    [ObservableProperty] private decimal totalHufVolumeToday;
    [ObservableProperty] private ObservableCollection<CurrencySummary> currencySummaries = new();

    public DashboardViewModel(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
        Title = "Dashboard";
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            var result = await _statisticsService.GetSummaryAsync();

            if (result.IsError)
            {
                SetError(result.FirstError.Description);
                return;
            }

            var summary = result.Value;

            TotalTransactionsToday = summary.TotalTransactionsToday;
            TotalHufVolumeToday = summary.TotalHufVolumeToday;

            CurrencySummaries.Clear();
            if (summary.TransactionsByCurrency != null)
            {
                foreach (var tx in summary.TransactionsByCurrency)
                {
                    var cs = new CurrencySummary
                    {
                        Currency = tx.Currency,
                        BuyCount = tx.BuyCount,
                        SellCount = tx.SellCount,
                        TotalHufVolume = tx.TotalHufVolume
                    };

                    CurrencySummaries.Add(cs);
                }
            }
        }
        catch (Exception ex)
        {
            SetError($"Error loading dashboard: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadAsync();
    }
}
