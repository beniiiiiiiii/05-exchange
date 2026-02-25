using Microsoft.AspNetCore.Identity.Data;

namespace Solution.Validators.RequestValidators;

public class ResetPasswordRequestValidator: AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .MinimumLength(8)
            .WithMessage("New password must be at least 8 characters long.");
    }
}
