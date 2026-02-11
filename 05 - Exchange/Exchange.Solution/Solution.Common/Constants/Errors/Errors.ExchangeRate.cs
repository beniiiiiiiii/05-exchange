using ErrorOr;

namespace Solution.Common.Constants.Errors;

public static partial class Errors
{
    public static class ExchangeRate
    {
        public static Error NotFoundForDate => Error.NotFound();
    }
}
