namespace Solution.Validators;

public class UpdateExchangeRateRequestValidator : AbstractValidator<UpdateExchangeRateRequest>
{
    public UpdateExchangeRateRequestValidator()
    {
        RuleFor(x => x.Currency)
            .IsInEnum()
            .WithMessage("Invalid currency.")
            .Must(c => c != Currency.HUF)
            .WithMessage("Cannot set exchange rate for HUF");

        RuleFor(x => x.BuyRate)
            .GreaterThan(0)
            .WithMessage("Buy rate must be greater than zero.");

        RuleFor(x => x.SellRate)
            .GreaterThan(0)
            .WithMessage("Sell rate must be greater than zero.")
            .GreaterThan(x => x.BuyRate)
            .WithMessage("Sell rate must be greater than buy rate.");
    }
}
