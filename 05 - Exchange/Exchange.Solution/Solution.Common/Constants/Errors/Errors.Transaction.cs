using ErrorOr;

namespace Solution.Common.Constants.Errors;

public static partial class Errors
{
    public static class Transaction
    {
        public static Error NotFound => Error.NotFound(
            code: "Transaction.NotFound",
            description: "The transaction was not found."
        );

        public static Error NoRateForToday => Error.Validation(
            code: "Transaction.NoRateForToday",
            description: "Exchange rate not available for today. Please set daily rates first."
        );

        public static Error InvalidAmounts =>Error.Validation(
            code: "Transaction.InvalidAmounts",
            description: "Transaction amount must be greater than zero."
        );
    }
}
