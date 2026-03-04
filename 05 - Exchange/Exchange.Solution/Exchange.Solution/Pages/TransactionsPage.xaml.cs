namespace Solution.DesktopApp.Pages;

public partial class TransactionsPage : ContentPage
{
    private readonly TransactionViewModel _viewModel;

    public TransactionsPage(TransactionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTransactionsAsync();
    }
}
