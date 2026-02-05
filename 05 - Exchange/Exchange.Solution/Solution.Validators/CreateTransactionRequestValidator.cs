namespace Solution.Validators;

public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.Currency)
            .IsInEnum()
            .WithMessage("Currency must be a valid enum value.")
            .Must(c => c != Currency.HUF)
            .WithMessage("Cannot exchange HUF to HUF.");

        RuleFor(x => x.ForeignAmount)
            .GreaterThan(0)
            .WithMessage("Foreign amount must be greater than zero.");

        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .WithMessage("Customer name is required.")
            .MaximumLength(100)
            .WithMessage("Customer name cannot exceed 100 characters.");

        RuleFor(x => x.CustomerIdType)
            .IsInEnum()
            .WithMessage("Invalid customer ID type.");
    }
}
