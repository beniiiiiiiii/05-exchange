using FluentValidation;
using Solution.Core.Models.Requests;

namespace Solution.Validators;

public class CreateExchangeRatesRequestValidator : AbstractValidator<CreateExchangeRatesRequest> 
{
    public CreateExchangeRatesRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date is required")
            .Must(BeCurrentDate)
            .WithMessage("Exchange rates can only be created for the current day");

        RuleFor(x => x.UsdBuyRate)
            .GreaterThan(0)
            .WithMessage("USD Buy Rate must be greater than 0");

        RuleFor(x => x.UsdSellRate)
            .GreaterThan(0)
            .WithMessage("USD Sell Rate must be greater than 0");

        RuleFor(x => x.gbpBuyRate)
            .GreaterThan(0)
            .WithMessage("GBP Buy Rate must be greater than 0");

        RuleFor(x => x.gbpSellRate)
            .GreaterThan(0)
            .WithMessage("GBP Sell Rate must be greater than 0");

        RuleFor(x => x.chfBuyRate)
            .GreaterThan(0)
            .WithMessage("CHF Buy Rate must be greater than 0");

        RuleFor(x => x.chfSellRate)
            .GreaterThan(0)
            .WithMessage("CHF Sell Rate must be greater than 0");
    }

    private bool BeCurrentDate(DateOnly date)
    {
        return date == DateOnly.FromDateTime(DateTime.Today);
    }


}
