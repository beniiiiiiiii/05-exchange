namespace Solution.Common.Constants.Errors;

public static partial class Errors
{
	public static class ExchangeRate
	{
		public static Error NotFoundForDate => Error.NotFound(
			code: "ExchangeRate.NotFoundForDate",
			description: "No exchange rate found for the specified date."
		);

		public static Error NotFoundForCurrency => Error.NotFound(
		   code: "ExchangeRate.NotFoundForCurrency",
		   description: "Exchange rate not found for the specified currency."
	   );

		public static Error AlreadyExistsForDate => Error.Conflict(
			code: "ExchangeRate.AlreadyExistsForDate",
			description: "Exchange rates already exist for this date."
		);

		public static Error OnlyCurrentDateAllowed => Error.Validation(
			code: "ExchangeRate.OnlyCurrentDateAllowed",
			description: "Exchange rates can only be created or modified for the current date."
		);

		public static Error IncompleteRates => Error.Validation(
			code: "ExchangeRate.IncompleteRates",
			description: "All three currency rates (USD, GBP, CHF) must be provided."
		);
	}
}
