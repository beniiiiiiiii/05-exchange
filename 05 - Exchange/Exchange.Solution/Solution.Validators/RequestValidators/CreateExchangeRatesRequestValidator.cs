namespace Solution.Validators.RequestValidators;

public class CreateExchangeRatesRequestValidator : AbstractValidator<CreateExchangeRatesRequest> 
{
    public CreateExchangeRatesRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date is required");

        RuleFor(x => x.UsdBuyRate)
            .GreaterThan(0)
            .WithMessage("USD Buy Rate must be greater than 0");

        RuleFor(x => x.UsdSellRate)
            .GreaterThan(0)
            .WithMessage("USD Sell Rate must be greater than 0");

        RuleFor(x => x.GbpBuyRate)
            .GreaterThan(0)
            .WithMessage("GBP Buy Rate must be greater than 0");

        RuleFor(x => x.GbpSellRate)
            .GreaterThan(0)
            .WithMessage("GBP Sell Rate must be greater than 0");

        RuleFor(x => x.ChfBuyRate)
            .GreaterThan(0)
            .WithMessage("CHF Buy Rate must be greater than 0");

        RuleFor(x => x.ChfSellRate)
            .GreaterThan(0)
            .WithMessage("CHF Sell Rate must be greater than 0");
    }
}
