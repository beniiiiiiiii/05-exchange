namespace Solution.DesktopApp.ViewModels;

public partial class TransactionViewModel : BaseViewModel
{
    private readonly ITransactionService _transactionService;

    [ObservableProperty] private ObservableCollection<TransactionResponse> transactions = new();
    [ObservableProperty] private decimal foreignAmount;
    [ObservableProperty] private Currency selectedCurrency = Currency.USD;
    [ObservableProperty] private string customerName = string.Empty;
    [ObservableProperty] private string customerIdNumber = string.Empty;
    [ObservableProperty] private CustomerIdType selectedIdType = CustomerIdType.Passport;
    [ObservableProperty] private bool showCreateForm;

    public Array CurrencyOptions => Enum.GetValues(typeof(Currency));
    public Array IdTypeOptions => Enum.GetValues(typeof(CustomerIdType));

    public TransactionViewModel(ITransactionService transactionService)
    {
        _transactionService = transactionService;
        Title = "Transactions";
    }

    [RelayCommand]
    public async Task LoadTransactionsAsync()
    {
        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            var result = await _transactionService.GetTransactionsAsync(null, null, null);
            
            if (result.IsError)
            {
                SetError(result.FirstError.Description);
                return;
            }

            Transactions.Clear();
            foreach (var item in result.Value.Transactions)
                Transactions.Add(item);
        }
        catch (Exception ex)
        {
            SetError($"Error loading transactions: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CreateBuyTransactionAsync()
    {
        if (!ValidateTransaction())
            return;

        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            var request = new CreateTransactionRequest
            {
                ForeignAmount = ForeignAmount,
                Currency = SelectedCurrency,
                CustomerName = CustomerName,
                CustomerIdType = SelectedIdType,
                CustomerIdNumber = CustomerIdNumber
            };

            var result = await _transactionService.CreateBuyTransactionAsync(request);
            
            if (result.IsError)
            {
                var errorMsg = result.FirstError.Description;
                if (result.FirstError.Code == "Transaction.NoRateForToday")
                {
                    errorMsg = "Please set today's exchange rates first before creating transactions. Go to Exchange Rates page to set rates.";
                }
                SetError(errorMsg);
                return;
            }

            Transactions.Insert(0, result.Value);
            ResetForm();
            ShowCreateForm = false;
        }
        catch (Exception ex)
        {
            SetError($"Error creating buy transaction: {ex.Message}\n\nInner: {ex.InnerException?.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CreateSellTransactionAsync()
    {
        if (!ValidateTransaction())
            return;

        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            var request = new CreateTransactionRequest
            {
                ForeignAmount = ForeignAmount,
                Currency = SelectedCurrency,
                CustomerName = CustomerName,
                CustomerIdType = SelectedIdType,
                CustomerIdNumber = CustomerIdNumber
            };

            var result = await _transactionService.CreateSellTransactionAsync(request);
            
            if (result.IsError)
            {
                var errorMsg = result.FirstError.Description;
                if (result.FirstError.Code == "Transaction.NoRateForToday")
                {
                    errorMsg = "Please set today's exchange rates first before creating transactions. Go to Exchange Rates page to set rates.";
                }
                SetError(errorMsg);
                return;
            }

            Transactions.Insert(0, result.Value);
            ResetForm();
            ShowCreateForm = false;
        }
        catch (Exception ex)
        {
            SetError($"Error creating sell transaction: {ex.Message}\n\nInner: {ex.InnerException?.Message}");
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

    private bool ValidateTransaction()
    {
        if (ForeignAmount <= 0)
        {
            SetError("Amount must be greater than 0");
            return false;
        }

        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            SetError("Customer name is required");
            return false;
        }

        if (string.IsNullOrWhiteSpace(CustomerIdNumber))
        {
            SetError("Customer ID number is required");
            return false;
        }

        return true;
    }

    private void ResetForm()
    {
        ForeignAmount = 0;
        CustomerName = string.Empty;
        CustomerIdNumber = string.Empty;
        SelectedCurrency = Currency.USD;
        SelectedIdType = CustomerIdType.Passport;
    }
}
