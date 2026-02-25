namespace Solution.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<TransactionService> logger;

        public TransactionService(
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<TransactionService> logger)
        {
            this.dbContext = dbContext;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
        }

        public async Task<ErrorOr<TransactionResponse>> CreateBuyTransactionAsync(
            CreateTransactionRequest request)
        {
            return await CreateTransactionAsync(request, TransactionType.Buy);
        }

        public async Task<ErrorOr<TransactionResponse>> CreateSellTransactionAsync(
            CreateTransactionRequest request)
        {
            return await CreateTransactionAsync(request, TransactionType.Sell);
        }

        private async Task<ErrorOr<TransactionResponse>> CreateTransactionAsync(
       CreateTransactionRequest request, TransactionType type)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var exchangeRate = await dbContext.ExchangeRates
                .FirstOrDefaultAsync(r => r.Date == today && r.Currency == request.Currency);

            if (exchangeRate is null)
                return Errors.Transaction.NoRateForToday;

            var appliedRate = type == TransactionType.Buy
                ? exchangeRate.SellRate
                : exchangeRate.BuyRate;

            var hufAmount = request.ForeignAmount * appliedRate;

            var userId = GetCurrentUserId();

            var transaction = new TransactionEntity
            {
                Type = type,
                Currency = request.Currency,
                ForeignAmount = request.ForeignAmount,
                HufAmount = hufAmount,
                AppliedRate = appliedRate,
                CustomerName = request.CustomerName,
                CustomerIdType = request.CustomerIdType,
                CustomerIdNumber = request.CustomerIdNumber,
                TransactionDate = DateTime.UtcNow,
                ProcessedByUserId = userId,
                ExchangeRateId = exchangeRate.Id
            };

            await dbContext.Transactions.AddAsync(transaction);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Transaction {Type} created for {Amount} {Currency} by user {UserId}",
                type, request.ForeignAmount, request.Currency, userId);

            return await GetTransactionByIdAsync(transaction.Id);
        }

        public async Task<ErrorOr<TransactionListResponse>> GetTransactionsAsync(
        DateOnly? date, Currency? currency, TransactionType? type)
        {
            var query = dbContext.Transactions
                .Include(t => t.ProcessedByUser)
                .AsQueryable();

            if (date.HasValue)
            {
                var startOfDay = date.Value.ToDateTime(TimeOnly.MinValue);
                var endOfDay = date.Value.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(t => t.TransactionDate >= startOfDay && t.TransactionDate <= endOfDay);
            }

            if (currency.HasValue)
                query = query.Where(t => t.Currency == currency.Value);

            if (type.HasValue)
                query = query.Where(t => t.Type == type.Value);

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            return new TransactionListResponse
            {
                Transactions = transactions.Select(MapToResponse).ToList(),
                TotalCount = transactions.Count
            };
        }

        public async Task<ErrorOr<TransactionResponse>> GetTransactionByIdAsync(int id)
        {
            var transaction = await dbContext.Transactions
                .Include(t => t.ProcessedByUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction is null)
                return Errors.Transaction.NotFound;

            return MapToResponse(transaction);
        }

        private string GetCurrentUserId()
        {
            return httpContextAccessor.HttpContext?.User.FindFirstValue("uid")
                ?? throw new InvalidOperationException("User not authenticated");
        }

        private static TransactionResponse MapToResponse(TransactionEntity entity)
        {
            return new TransactionResponse
            {
                Id = entity.Id,
                Type = entity.Type.ToString(),
                Currency = entity.Currency.ToString(),
                ForeignAmount = entity.ForeignAmount,
                HufAmount = entity.HufAmount,
                AppliedRate = entity.AppliedRate,
                CustomerName = entity.CustomerName,
                CustomerIdType = entity.CustomerIdType.ToString(),
                CustomerIdNumber = entity.CustomerIdNumber,
                TransactionDate = entity.TransactionDate,
                ProcessedBy = entity.ProcessedByUser?.FullName ?? "Unknown"
            };
        }
    }
}
