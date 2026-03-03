# Penzvalto (Currency Exchange) REST API - Implementation Plan

## Overview

A Currency Exchange REST API built with ASP.NET Core 10, following the reference architecture patterns from the `__full example__` folder. This API enables authenticated users to manage daily exchange rates and process currency exchange transactions.

**Supported Currencies:**

- USD (United States Dollar)
- GBP (British Pound Sterling)
- CHF (Swiss Franc)
- HUF (Hungarian Forint) - base currency for all exchanges

**Technology Stack:**

- ASP.NET Core 10 REST API
- Entity Framework Core 9.0.8
- ASP.NET Core Identity
- JWT Bearer + Basic Authentication
- FluentValidation
- ErrorOr pattern for functional error handling
- Scalar for OpenAPI documentation

---

## Project Structure

Following the reference architecture (Clean/Layered Architecture):

```
Solution.sln
├── Solution.Api/           # Presentation layer - Controllers, Auth, Configuration
├── Solution.Core/          # Domain layer - DTOs, Interfaces, Models
├── Solution.Services/      # Application layer - Service implementations
├── Solution.Database/      # Data layer - DbContext, Entities, Migrations
├── Solution.Validators/    # Validation layer - FluentValidation rules
├── Solution.Common/        # Shared layer - Constants, Errors, Extensions
├── Solution.Tests/         # Test layer - Unit and Integration tests
└── Solution.Maui/          # Desktop app - MAUI Windows desktop client
```

---

## Phase 1: Database Layer (Solution.Database)

### 1.1 Enums

**Create: `/Solution.Database/Enums/Currency.cs`**

```csharp
namespace Solution.Database.Enums;

public enum Currency : byte
{
    USD = 1,
    GBP = 2,
    CHF = 3,
    HUF = 4
}
```

**Create: `/Solution.Database/Enums/TransactionType.cs`**

```csharp
namespace Solution.Database.Enums;

public enum TransactionType : byte
{
    Buy = 1,   // Customer buys foreign currency (pays HUF, receives foreign)
    Sell = 2   // Customer sells foreign currency (pays foreign, receives HUF)
}
```

**Create: `/Solution.Database/Enums/CustomerIdType.cs`**

```csharp
namespace Solution.Database.Enums;

public enum CustomerIdType : byte
{
    PersonalIdCard = 1,   // Szemelyi igazolvany
    Passport = 2,         // Utlevel
    DrivingLicense = 3    // Jogositvany
}
```

### 1.2 Entities

**Create: `/Solution.Database/Entities/ExchangeRateEntity.cs`**

```csharp
namespace Solution.Database.Entities;

[Table("ExchangeRate")]
public class ExchangeRateEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Currency Currency { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    [Precision(18, 4)]
    public decimal BuyRate { get; set; }  // Rate when bank buys foreign currency

    [Required]
    [Precision(18, 4)]
    public decimal SellRate { get; set; } // Rate when bank sells foreign currency

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    [Required]
    public string CreatedByUserId { get; set; }

    public string? ModifiedByUserId { get; set; }

    // Navigation properties
    public virtual UserEntity CreatedByUser { get; set; }
    public virtual UserEntity? ModifiedByUser { get; set; }
    public virtual ICollection<TransactionEntity> Transactions { get; set; }
}
```

**Create: `/Solution.Database/Entities/TransactionEntity.cs`**

```csharp
namespace Solution.Database.Entities;

[Table("Transaction")]
public class TransactionEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    public Currency Currency { get; set; }

    [Required]
    [Precision(18, 2)]
    public decimal ForeignAmount { get; set; }  // Amount in foreign currency

    [Required]
    [Precision(18, 2)]
    public decimal HufAmount { get; set; }      // Amount in HUF

    [Required]
    [Precision(18, 4)]
    public decimal AppliedRate { get; set; }    // Snapshot of rate at transaction time

    // Customer information
    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; }

    [Required]
    public CustomerIdType CustomerIdType { get; set; }

    [Required]
    [MaxLength(50)]
    public string CustomerIdNumber { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; }

    [Required]
    public string ProcessedByUserId { get; set; }

    public int ExchangeRateId { get; set; }

    // Navigation properties
    public virtual UserEntity ProcessedByUser { get; set; }
    public virtual ExchangeRateEntity ExchangeRate { get; set; }
}
```

### 1.3 Update AppDbContext.cs

**Modify: `/Solution.Database/AppDbContext.cs`**

Add DbSets:

```csharp
public DbSet<ExchangeRateEntity> ExchangeRates { get; set; }
public DbSet<TransactionEntity> Transactions { get; set; }
```

Add to `OnModelCreating`:

```csharp
// ExchangeRate configuration
builder.Entity<ExchangeRateEntity>(b =>
{
    b.ToTable("ExchangeRate");
    b.HasIndex(e => new { e.Currency, e.Date }).IsUnique();
    b.Property(e => e.BuyRate).HasPrecision(18, 4);
    b.Property(e => e.SellRate).HasPrecision(18, 4);
    b.Property(e => e.Currency).HasConversion<string>();

    b.HasOne(e => e.CreatedByUser)
     .WithMany()
     .HasForeignKey(e => e.CreatedByUserId)
     .OnDelete(DeleteBehavior.Restrict);

    b.HasOne(e => e.ModifiedByUser)
     .WithMany()
     .HasForeignKey(e => e.ModifiedByUserId)
     .OnDelete(DeleteBehavior.Restrict);
});

// Transaction configuration
builder.Entity<TransactionEntity>(b =>
{
    b.ToTable("Transaction");
    b.Property(e => e.ForeignAmount).HasPrecision(18, 2);
    b.Property(e => e.HufAmount).HasPrecision(18, 2);
    b.Property(e => e.AppliedRate).HasPrecision(18, 4);
    b.Property(e => e.Type).HasConversion<string>();
    b.Property(e => e.Currency).HasConversion<string>();
    b.Property(e => e.CustomerIdType).HasConversion<string>();

    b.HasOne(e => e.ProcessedByUser)
     .WithMany()
     .HasForeignKey(e => e.ProcessedByUserId)
     .OnDelete(DeleteBehavior.Restrict);

    b.HasOne(e => e.ExchangeRate)
     .WithMany(e => e.Transactions)
     .HasForeignKey(e => e.ExchangeRateId)
     .OnDelete(DeleteBehavior.Restrict);
});
```

### 1.4 Update GlobalImports.cs

**Modify: `/Solution.Database/GlobalImports.cs`**

```csharp
global using Solution.Database.Enums;
```

---

## Phase 2: Core Layer (Solution.Core)

### 2.1 Request DTOs

**Create: `/Solution.Core/Models/Request/CreateExchangeRatesRequest.cs`**

```csharp
namespace Solution.Core.Models.Request;

public class CreateExchangeRatesRequest
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }

    [JsonPropertyName("usdBuyRate")]
    public decimal UsdBuyRate { get; set; }

    [JsonPropertyName("usdSellRate")]
    public decimal UsdSellRate { get; set; }

    [JsonPropertyName("gbpBuyRate")]
    public decimal GbpBuyRate { get; set; }

    [JsonPropertyName("gbpSellRate")]
    public decimal GbpSellRate { get; set; }

    [JsonPropertyName("chfBuyRate")]
    public decimal ChfBuyRate { get; set; }

    [JsonPropertyName("chfSellRate")]
    public decimal ChfSellRate { get; set; }
}
```

**Create: `/Solution.Core/Models/Request/UpdateExchangeRateRequest.cs`**

```csharp
namespace Solution.Core.Models.Request;

public class UpdateExchangeRateRequest
{
    [JsonPropertyName("currency")]
    public Currency Currency { get; set; }

    [JsonPropertyName("buyRate")]
    public decimal BuyRate { get; set; }

    [JsonPropertyName("sellRate")]
    public decimal SellRate { get; set; }
}
```

**Create: `/Solution.Core/Models/Request/CreateTransactionRequest.cs`**

```csharp
namespace Solution.Core.Models.Request;

public class CreateTransactionRequest
{
    [JsonPropertyName("currency")]
    public Currency Currency { get; set; }

    [JsonPropertyName("foreignAmount")]
    public decimal ForeignAmount { get; set; }

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; }

    [JsonPropertyName("customerIdType")]
    public CustomerIdType CustomerIdType { get; set; }

    [JsonPropertyName("customerIdNumber")]
    public string CustomerIdNumber { get; set; }
}
```

**Create: `/Solution.Core/Models/Request/CreateUserRequest.cs`**

```csharp
namespace Solution.Core.Models.Request;

public class CreateUserRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("password")]
    [JsonConverter(typeof(HidePasswordInJsonConverter))]
    public string Password { get; set; }

    [JsonPropertyName("role")]
    public UserRole Role { get; set; }
}
```

**Create: `/Solution.Core/Models/Request/UpdateUserRequest.cs`**

```csharp
namespace Solution.Core.Models.Request;

public class UpdateUserRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("role")]
    public UserRole Role { get; set; }
}
```

**Create: `/Solution.Core/Models/Request/ResetPasswordRequest.cs`**

```csharp
namespace Solution.Core.Models.Request;

public class ResetPasswordRequest
{
    [JsonPropertyName("newPassword")]
    [JsonConverter(typeof(HidePasswordInJsonConverter))]
    public string NewPassword { get; set; }
}
```

### 2.2 Response DTOs

**Create: `/Solution.Core/Models/Response/ExchangeRateResponse.cs`**

```csharp
namespace Solution.Core.Models.Response;

public class ExchangeRateResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }

    [JsonPropertyName("buyRate")]
    public decimal BuyRate { get; set; }

    [JsonPropertyName("sellRate")]
    public decimal SellRate { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("modifiedAt")]
    public DateTime? ModifiedAt { get; set; }
}
```

**Create: `/Solution.Core/Models/Response/ExchangeRatesResponse.cs`**

```csharp
namespace Solution.Core.Models.Response;

public class ExchangeRatesResponse
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }

    [JsonPropertyName("rates")]
    public List<ExchangeRateResponse> Rates { get; set; } = new();
}
```

**Create: `/Solution.Core/Models/Response/TransactionResponse.cs`**

```csharp
namespace Solution.Core.Models.Response;

public class TransactionResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("foreignAmount")]
    public decimal ForeignAmount { get; set; }

    [JsonPropertyName("hufAmount")]
    public decimal HufAmount { get; set; }

    [JsonPropertyName("appliedRate")]
    public decimal AppliedRate { get; set; }

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; }

    [JsonPropertyName("customerIdType")]
    public string CustomerIdType { get; set; }

    [JsonPropertyName("customerIdNumber")]
    public string CustomerIdNumber { get; set; }

    [JsonPropertyName("transactionDate")]
    public DateTime TransactionDate { get; set; }

    [JsonPropertyName("processedBy")]
    public string ProcessedBy { get; set; }
}
```

**Create: `/Solution.Core/Models/Response/TransactionListResponse.cs`**

```csharp
namespace Solution.Core.Models.Response;

public class TransactionListResponse
{
    [JsonPropertyName("transactions")]
    public List<TransactionResponse> Transactions { get; set; } = new();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}
```

**Create: `/Solution.Core/Models/Response/StatisticsResponses.cs`**

```csharp
namespace Solution.Core.Models.Response;

public class RateStatisticsResponse
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("dataPoints")]
    public List<RateDataPoint> DataPoints { get; set; } = new();
}

public class RateDataPoint
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }

    [JsonPropertyName("buyRate")]
    public decimal BuyRate { get; set; }

    [JsonPropertyName("sellRate")]
    public decimal SellRate { get; set; }
}

public class TransactionStatisticsResponse
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("totalBuyCount")]
    public int TotalBuyCount { get; set; }

    [JsonPropertyName("totalSellCount")]
    public int TotalSellCount { get; set; }

    [JsonPropertyName("totalBuyHufAmount")]
    public decimal TotalBuyHufAmount { get; set; }

    [JsonPropertyName("totalSellHufAmount")]
    public decimal TotalSellHufAmount { get; set; }

    [JsonPropertyName("dailyBreakdown")]
    public List<DailyTransactionData> DailyBreakdown { get; set; } = new();
}

public class DailyTransactionData
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }

    [JsonPropertyName("buyCount")]
    public int BuyCount { get; set; }

    [JsonPropertyName("sellCount")]
    public int SellCount { get; set; }

    [JsonPropertyName("buyHufAmount")]
    public decimal BuyHufAmount { get; set; }

    [JsonPropertyName("sellHufAmount")]
    public decimal SellHufAmount { get; set; }
}

public class SummaryStatisticsResponse
{
    [JsonPropertyName("todayRatesSet")]
    public bool TodayRatesSet { get; set; }

    [JsonPropertyName("totalTransactionsToday")]
    public int TotalTransactionsToday { get; set; }

    [JsonPropertyName("totalHufVolumeToday")]
    public decimal TotalHufVolumeToday { get; set; }

    [JsonPropertyName("transactionsByCurrency")]
    public List<CurrencySummary> TransactionsByCurrency { get; set; } = new();
}

public class CurrencySummary
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("buyCount")]
    public int BuyCount { get; set; }

    [JsonPropertyName("sellCount")]
    public int SellCount { get; set; }

    [JsonPropertyName("totalHufVolume")]
    public decimal TotalHufVolume { get; set; }
}
```

**Create: `/Solution.Core/Models/Response/UserListResponse.cs`**

```csharp
namespace Solution.Core.Models.Response;

public class UserListResponse
{
    [JsonPropertyName("users")]
    public List<UserResponseModel> Users { get; set; } = new();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}
```

### 2.3 Service Interfaces

**Create: `/Solution.Core/Interfaces/Services/IExchangeRateService.cs`**

```csharp
namespace Solution.Core.Interfaces.Services;

public interface IExchangeRateService
{
    Task<ErrorOr<ExchangeRatesResponse>> GetRatesByDateAsync(DateOnly date);
    Task<ErrorOr<ExchangeRatesResponse>> GetTodayRatesAsync();
    Task<ErrorOr<List<ExchangeRatesResponse>>> GetRatesHistoryAsync(DateOnly? startDate, DateOnly? endDate);
    Task<ErrorOr<bool>> RatesExistForDateAsync(DateOnly date);
    Task<ErrorOr<ExchangeRatesResponse>> CreateDailyRatesAsync(CreateExchangeRatesRequest request);
    Task<ErrorOr<ExchangeRateResponse>> UpdateRateAsync(UpdateExchangeRateRequest request);
}
```

**Create: `/Solution.Core/Interfaces/Services/ITransactionService.cs`**

```csharp
namespace Solution.Core.Interfaces.Services;

public interface ITransactionService
{
    Task<ErrorOr<TransactionResponse>> CreateBuyTransactionAsync(CreateTransactionRequest request);
    Task<ErrorOr<TransactionResponse>> CreateSellTransactionAsync(CreateTransactionRequest request);
    Task<ErrorOr<TransactionListResponse>> GetTransactionsAsync(DateOnly? date, Currency? currency, TransactionType? type);
    Task<ErrorOr<TransactionResponse>> GetTransactionByIdAsync(int id);
}
```

**Create: `/Solution.Core/Interfaces/Services/IStatisticsService.cs`**

```csharp
namespace Solution.Core.Interfaces.Services;

public interface IStatisticsService
{
    Task<ErrorOr<List<RateStatisticsResponse>>> GetRateStatisticsAsync(DateOnly startDate, DateOnly endDate);
    Task<ErrorOr<List<TransactionStatisticsResponse>>> GetTransactionStatisticsAsync(DateOnly startDate, DateOnly endDate);
    Task<ErrorOr<SummaryStatisticsResponse>> GetSummaryAsync();
}
```

**Create: `/Solution.Core/Interfaces/Services/IUserManagementService.cs`**

```csharp
namespace Solution.Core.Interfaces.Services;

public interface IUserManagementService
{
    Task<ErrorOr<UserListResponse>> GetAllUsersAsync();
    Task<ErrorOr<UserResponseModel>> GetUserByIdAsync(string userId);
    Task<ErrorOr<UserResponseModel>> CreateUserAsync(CreateUserRequest request);
    Task<ErrorOr<UserResponseModel>> UpdateUserAsync(string userId, UpdateUserRequest request);
    Task<ErrorOr<Success>> DeleteUserAsync(string userId, string currentUserId);
    Task<ErrorOr<Success>> ResetPasswordAsync(string userId, ResetPasswordRequest request);
}
```

### 2.4 Update GlobalImports.cs

**Modify: `/Solution.Core/GlobalImports.cs`**

```csharp
global using Solution.Database.Enums;
```

---

## Phase 3: Validators (Solution.Validators)

### 3.1 Request Validators

**Create: `/Solution.Validators/RequestValidators/CreateExchangeRatesRequestValidator.cs`**

```csharp
namespace Solution.Validators.RequestValidators;

public class CreateExchangeRatesRequestValidator : AbstractValidator<CreateExchangeRatesRequest>
{
    public CreateExchangeRatesRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.")
            .Must(BeCurrentDate).WithMessage("Exchange rates can only be created for the current date.");

        RuleFor(x => x.UsdBuyRate)
            .GreaterThan(0).WithMessage("USD buy rate must be greater than 0.");

        RuleFor(x => x.UsdSellRate)
            .GreaterThan(0).WithMessage("USD sell rate must be greater than 0.")
            .GreaterThan(x => x.UsdBuyRate).WithMessage("USD sell rate must be greater than buy rate.");

        RuleFor(x => x.GbpBuyRate)
            .GreaterThan(0).WithMessage("GBP buy rate must be greater than 0.");

        RuleFor(x => x.GbpSellRate)
            .GreaterThan(0).WithMessage("GBP sell rate must be greater than 0.")
            .GreaterThan(x => x.GbpBuyRate).WithMessage("GBP sell rate must be greater than buy rate.");

        RuleFor(x => x.ChfBuyRate)
            .GreaterThan(0).WithMessage("CHF buy rate must be greater than 0.");

        RuleFor(x => x.ChfSellRate)
            .GreaterThan(0).WithMessage("CHF sell rate must be greater than 0.")
            .GreaterThan(x => x.ChfBuyRate).WithMessage("CHF sell rate must be greater than buy rate.");
    }

    private bool BeCurrentDate(DateOnly date)
    {
        return date == DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
```

**Create: `/Solution.Validators/RequestValidators/UpdateExchangeRateRequestValidator.cs`**

```csharp
namespace Solution.Validators.RequestValidators;

public class UpdateExchangeRateRequestValidator : AbstractValidator<UpdateExchangeRateRequest>
{
    public UpdateExchangeRateRequestValidator()
    {
        RuleFor(x => x.Currency)
            .IsInEnum().WithMessage("Invalid currency.")
            .Must(c => c != Currency.HUF).WithMessage("Cannot set exchange rate for HUF.");

        RuleFor(x => x.BuyRate)
            .GreaterThan(0).WithMessage("Buy rate must be greater than 0.");

        RuleFor(x => x.SellRate)
            .GreaterThan(0).WithMessage("Sell rate must be greater than 0.")
            .GreaterThan(x => x.BuyRate).WithMessage("Sell rate must be greater than buy rate.");
    }
}
```

**Create: `/Solution.Validators/RequestValidators/CreateTransactionRequestValidator.cs`**

```csharp
namespace Solution.Validators.RequestValidators;

public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.Currency)
            .IsInEnum().WithMessage("Invalid currency.")
            .Must(c => c != Currency.HUF).WithMessage("Cannot exchange HUF for HUF.");

        RuleFor(x => x.ForeignAmount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(100).WithMessage("Customer name cannot exceed 100 characters.");

        RuleFor(x => x.CustomerIdType)
            .IsInEnum().WithMessage("Invalid customer ID type.");

        RuleFor(x => x.CustomerIdNumber)
            .NotEmpty().WithMessage("Customer ID number is required.")
            .MaximumLength(50).WithMessage("Customer ID number cannot exceed 50 characters.");
    }
}
```

**Create: `/Solution.Validators/RequestValidators/CreateUserRequestValidator.cs`**

```csharp
namespace Solution.Validators.RequestValidators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role.");
    }
}
```

**Create: `/Solution.Validators/RequestValidators/UpdateUserRequestValidator.cs`**

```csharp
namespace Solution.Validators.RequestValidators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role.");
    }
}
```

**Create: `/Solution.Validators/RequestValidators/ResetPasswordRequestValidator.cs`**

```csharp
namespace Solution.Validators.RequestValidators;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");
    }
}
```

### 3.2 Update GlobalImports.cs

**Modify: `/Solution.Validators/GlobalImports.cs`**

```csharp
global using Solution.Database.Enums;
```

---

## Phase 4: Error Constants (Solution.Common)

**Create: `/Solution.Common/Constants/Errors/Errors.ExchangeRate.cs`**

```csharp
namespace Solution.Common.Constants;

public static partial class Errors
{
    public static class ExchangeRate
    {
        public static Error NotFoundForDate => Error.NotFound(
            code: "ExchangeRate.NotFoundForDate",
            description: "Exchange rates not found for the specified date."
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
```

**Create: `/Solution.Common/Constants/Errors/Errors.Transaction.cs`**

```csharp
namespace Solution.Common.Constants;

public static partial class Errors
{
    public static class Transaction
    {
        public static Error NotFound => Error.NotFound(
            code: "Transaction.NotFound",
            description: "Transaction not found."
        );

        public static Error NoRateForToday => Error.Validation(
            code: "Transaction.NoRateForToday",
            description: "Exchange rate not available for today. Please set daily rates first."
        );

        public static Error InvalidAmount => Error.Validation(
            code: "Transaction.InvalidAmount",
            description: "Transaction amount must be greater than zero."
        );
    }
}
```

**Update: `/Solution.Common/Constants/Errors/Errors.User.cs`**

```csharp
namespace Solution.Common.Constants;

public static partial class Errors
{
    public static class User
    {
        public static Error NotFound => Error.NotFound(
            code: "User.NotFound",
            description: "User not found."
        );

        public static Error EmailAlreadyExists => Error.Conflict(
            code: "User.EmailAlreadyExists",
            description: "A user with this email already exists."
        );

        public static Error CreationFailed => Error.Failure(
            code: "User.CreationFailed",
            description: "Failed to create user."
        );

        public static Error DeletionFailed => Error.Failure(
            code: "User.DeletionFailed",
            description: "Failed to delete user."
        );

        public static Error CannotDeleteSelf => Error.Validation(
            code: "User.CannotDeleteSelf",
            description: "Users cannot delete their own account."
        );

        public static Error PasswordResetFailed => Error.Failure(
            code: "User.PasswordResetFailed",
            description: "Failed to reset password."
        );

        public static Error Unauthorized => Error.Unauthorized(
            code: "User.Unauthorized",
            description: "You are not authorized to perform this action."
        );
    }
}
```

---

## Phase 5: Services Layer (Solution.Services)

### 5.1 ExchangeRateService

**Create: `/Solution.Services/Services/ExchangeRateService.cs`**

```csharp
namespace Solution.Services.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<ExchangeRateService> logger;

    public ExchangeRateService(
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ExchangeRateService> logger)
    {
        this.dbContext = dbContext;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    public async Task<ErrorOr<ExchangeRatesResponse>> GetRatesByDateAsync(DateOnly date)
    {
        var rates = await dbContext.ExchangeRates
            .Where(r => r.Date == date)
            .OrderBy(r => r.Currency)
            .ToListAsync();

        if (!rates.Any())
            return Errors.ExchangeRate.NotFoundForDate;

        return MapToResponse(date, rates);
    }

    public async Task<ErrorOr<ExchangeRatesResponse>> GetTodayRatesAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await GetRatesByDateAsync(today);
    }

    public async Task<ErrorOr<List<ExchangeRatesResponse>>> GetRatesHistoryAsync(
        DateOnly? startDate, DateOnly? endDate)
    {
        var query = dbContext.ExchangeRates.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(r => r.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.Date <= endDate.Value);

        var rates = await query
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.Currency)
            .ToListAsync();

        var grouped = rates.GroupBy(r => r.Date)
            .Select(g => MapToResponse(g.Key, g.ToList()))
            .ToList();

        return grouped;
    }

    public async Task<ErrorOr<bool>> RatesExistForDateAsync(DateOnly date)
    {
        var count = await dbContext.ExchangeRates
            .CountAsync(r => r.Date == date);

        return count == 3; // All three currencies must exist
    }

    public async Task<ErrorOr<ExchangeRatesResponse>> CreateDailyRatesAsync(
        CreateExchangeRatesRequest request)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (request.Date != today)
            return Errors.ExchangeRate.OnlyCurrentDateAllowed;

        var existingRates = await dbContext.ExchangeRates
            .AnyAsync(r => r.Date == request.Date);

        if (existingRates)
            return Errors.ExchangeRate.AlreadyExistsForDate;

        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        var rates = new List<ExchangeRateEntity>
        {
            new() { Currency = Currency.USD, Date = request.Date, BuyRate = request.UsdBuyRate, SellRate = request.UsdSellRate, CreatedAt = now, CreatedByUserId = userId },
            new() { Currency = Currency.GBP, Date = request.Date, BuyRate = request.GbpBuyRate, SellRate = request.GbpSellRate, CreatedAt = now, CreatedByUserId = userId },
            new() { Currency = Currency.CHF, Date = request.Date, BuyRate = request.ChfBuyRate, SellRate = request.ChfSellRate, CreatedAt = now, CreatedByUserId = userId }
        };

        await dbContext.ExchangeRates.AddRangeAsync(rates);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Exchange rates created for date {Date} by user {UserId}", request.Date, userId);

        return MapToResponse(request.Date, rates);
    }

    public async Task<ErrorOr<ExchangeRateResponse>> UpdateRateAsync(
        UpdateExchangeRateRequest request)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var rate = await dbContext.ExchangeRates
            .FirstOrDefaultAsync(r => r.Date == today && r.Currency == request.Currency);

        if (rate is null)
            return Errors.ExchangeRate.NotFoundForCurrency;

        var userId = GetCurrentUserId();

        rate.BuyRate = request.BuyRate;
        rate.SellRate = request.SellRate;
        rate.ModifiedAt = DateTime.UtcNow;
        rate.ModifiedByUserId = userId;

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Exchange rate updated for {Currency} by user {UserId}", request.Currency, userId);

        return MapToResponse(rate);
    }

    private string GetCurrentUserId()
    {
        return httpContextAccessor.HttpContext?.User.FindFirstValue("uid")
            ?? throw new InvalidOperationException("User not authenticated");
    }

    private static ExchangeRatesResponse MapToResponse(DateOnly date, List<ExchangeRateEntity> rates)
    {
        return new ExchangeRatesResponse
        {
            Date = date,
            Rates = rates.Select(MapToResponse).ToList()
        };
    }

    private static ExchangeRateResponse MapToResponse(ExchangeRateEntity entity)
    {
        return new ExchangeRateResponse
        {
            Id = entity.Id,
            Currency = entity.Currency.ToString(),
            Date = entity.Date,
            BuyRate = entity.BuyRate,
            SellRate = entity.SellRate,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt
        };
    }
}
```

### 5.2 TransactionService

**Create: `/Solution.Services/Services/TransactionService.cs`**

```csharp
namespace Solution.Services.Services;

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

        // Buy: Customer buys foreign currency = bank sells = use SellRate
        // Sell: Customer sells foreign currency = bank buys = use BuyRate
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
            ProcessedBy = entity.ProcessedByUser?.Name ?? "Unknown"
        };
    }
}
```

### 5.3 StatisticsService

**Create: `/Solution.Services/Services/StatisticsService.cs`**

```csharp
namespace Solution.Services.Services;

public class StatisticsService : IStatisticsService
{
    private readonly ApplicationDbContext dbContext;

    public StatisticsService(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ErrorOr<List<RateStatisticsResponse>>> GetRateStatisticsAsync(
        DateOnly startDate, DateOnly endDate)
    {
        var rates = await dbContext.ExchangeRates
            .Where(r => r.Date >= startDate && r.Date <= endDate)
            .OrderBy(r => r.Date)
            .ToListAsync();

        var result = rates
            .GroupBy(r => r.Currency)
            .Select(g => new RateStatisticsResponse
            {
                Currency = g.Key.ToString(),
                DataPoints = g.Select(r => new RateDataPoint
                {
                    Date = r.Date,
                    BuyRate = r.BuyRate,
                    SellRate = r.SellRate
                }).ToList()
            })
            .ToList();

        return result;
    }

    public async Task<ErrorOr<List<TransactionStatisticsResponse>>> GetTransactionStatisticsAsync(
        DateOnly startDate, DateOnly endDate)
    {
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue);

        var transactions = await dbContext.Transactions
            .Where(t => t.TransactionDate >= startDateTime && t.TransactionDate <= endDateTime)
            .ToListAsync();

        var result = transactions
            .GroupBy(t => t.Currency)
            .Select(g => new TransactionStatisticsResponse
            {
                Currency = g.Key.ToString(),
                TotalBuyCount = g.Count(t => t.Type == TransactionType.Buy),
                TotalSellCount = g.Count(t => t.Type == TransactionType.Sell),
                TotalBuyHufAmount = g.Where(t => t.Type == TransactionType.Buy).Sum(t => t.HufAmount),
                TotalSellHufAmount = g.Where(t => t.Type == TransactionType.Sell).Sum(t => t.HufAmount),
                DailyBreakdown = g.GroupBy(t => DateOnly.FromDateTime(t.TransactionDate))
                    .Select(d => new DailyTransactionData
                    {
                        Date = d.Key,
                        BuyCount = d.Count(t => t.Type == TransactionType.Buy),
                        SellCount = d.Count(t => t.Type == TransactionType.Sell),
                        BuyHufAmount = d.Where(t => t.Type == TransactionType.Buy).Sum(t => t.HufAmount),
                        SellHufAmount = d.Where(t => t.Type == TransactionType.Sell).Sum(t => t.HufAmount)
                    })
                    .OrderBy(d => d.Date)
                    .ToList()
            })
            .ToList();

        return result;
    }

    public async Task<ErrorOr<SummaryStatisticsResponse>> GetSummaryAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startOfDay = today.ToDateTime(TimeOnly.MinValue);
        var endOfDay = today.ToDateTime(TimeOnly.MaxValue);

        var todayRatesCount = await dbContext.ExchangeRates.CountAsync(r => r.Date == today);

        var todayTransactions = await dbContext.Transactions
            .Where(t => t.TransactionDate >= startOfDay && t.TransactionDate <= endOfDay)
            .ToListAsync();

        var summary = new SummaryStatisticsResponse
        {
            TodayRatesSet = todayRatesCount == 3,
            TotalTransactionsToday = todayTransactions.Count,
            TotalHufVolumeToday = todayTransactions.Sum(t => t.HufAmount),
            TransactionsByCurrency = todayTransactions
                .GroupBy(t => t.Currency)
                .Select(g => new CurrencySummary
                {
                    Currency = g.Key.ToString(),
                    BuyCount = g.Count(t => t.Type == TransactionType.Buy),
                    SellCount = g.Count(t => t.Type == TransactionType.Sell),
                    TotalHufVolume = g.Sum(t => t.HufAmount)
                })
                .ToList()
        };

        return summary;
    }
}
```

### 5.4 UserManagementService

**Create: `/Solution.Services/Services/UserManagementService.cs`**

```csharp
namespace Solution.Services.Services;

public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext dbContext;
    private readonly UserManager<UserEntity> userManager;
    private readonly ILogger<UserManagementService> logger;

    public UserManagementService(
        ApplicationDbContext dbContext,
        UserManager<UserEntity> userManager,
        ILogger<UserManagementService> logger)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.logger = logger;
    }

    public async Task<ErrorOr<UserListResponse>> GetAllUsersAsync()
    {
        var users = await dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync();

        return new UserListResponse
        {
            Users = users.Select(MapToResponse).ToList(),
            TotalCount = users.Count
        };
    }

    public async Task<ErrorOr<UserResponseModel>> GetUserByIdAsync(string userId)
    {
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return Errors.User.NotFound;

        return MapToResponse(user);
    }

    public async Task<ErrorOr<UserResponseModel>> CreateUserAsync(CreateUserRequest request)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return Errors.User.EmailAlreadyExists;

        var user = new UserEntity
        {
            Name = request.Name,
            Email = request.Email,
            UserName = request.Email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return Errors.User.CreationFailed;

        await userManager.AddToRoleAsync(user, request.Role.ToString());

        logger.LogInformation("User {Email} created with role {Role}", request.Email, request.Role);

        return await GetUserByIdAsync(user.Id);
    }

    public async Task<ErrorOr<UserResponseModel>> UpdateUserAsync(string userId, UpdateUserRequest request)
    {
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return Errors.User.NotFound;

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null && existingUser.Id != userId)
            return Errors.User.EmailAlreadyExists;

        user.Name = request.Name;
        user.Email = request.Email;
        user.UserName = request.Email;

        await dbContext.SaveChangesAsync();

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, request.Role.ToString());

        logger.LogInformation("User {UserId} updated", userId);

        return await GetUserByIdAsync(userId);
    }

    public async Task<ErrorOr<Success>> DeleteUserAsync(string userId, string currentUserId)
    {
        if (userId == currentUserId)
            return Errors.User.CannotDeleteSelf;

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Errors.User.NotFound;

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return Errors.User.DeletionFailed;

        logger.LogInformation("User {UserId} deleted", userId);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ResetPasswordAsync(string userId, ResetPasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Errors.User.NotFound;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
            return Errors.User.PasswordResetFailed;

        logger.LogInformation("Password reset for user {UserId}", userId);

        return Result.Success;
    }

    private static UserResponseModel MapToResponse(UserEntity entity)
    {
        return new UserResponseModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Email = entity.Email,
            Roles = entity.UserRoles?.Select(ur => ur.Role?.Name).Where(r => r != null).ToList()
                    ?? new List<string>()
        };
    }
}
```

### 5.5 Update GlobalImports.cs

**Modify: `/Solution.Services/GlobalImports.cs`**

```csharp
global using Microsoft.AspNetCore.Http;
global using Microsoft.EntityFrameworkCore;
global using System.Security.Claims;
```

---

## Phase 6: API Layer (Solution.Api)

### 6.1 Controllers

**Create: `/Solution.Api/Controllers/ExchangeRateController.cs`**

```csharp
namespace Solution.Api.Controllers;

[Authorize]
public class ExchangeRateController(IExchangeRateService exchangeRateService) : BaseController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ExchangeRatesResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetRatesAsync(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate)
    {
        var result = await exchangeRateService.GetRatesHistoryAsync(startDate, endDate);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("today")]
    [ProducesResponseType(typeof(ExchangeRatesResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTodayRatesAsync()
    {
        var result = await exchangeRateService.GetTodayRatesAsync();
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("{date}")]
    [ProducesResponseType(typeof(ExchangeRatesResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetRatesByDateAsync([FromRoute] DateOnly date)
    {
        var result = await exchangeRateService.GetRatesByDateAsync(date);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("exists/{date}")]
    [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> RatesExistAsync([FromRoute] DateOnly date)
    {
        var result = await exchangeRateService.RatesExistForDateAsync(date);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExchangeRatesResponse), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> CreateDailyRatesAsync(
        [FromBody][Required] CreateExchangeRatesRequest request)
    {
        var result = await exchangeRateService.CreateDailyRatesAsync(request);
        return result.Match(
            result => CreatedAtAction(nameof(GetRatesByDateAsync), new { date = result.Date }, result),
            errors => Problem(errors)
        );
    }

    [HttpPut]
    [ProducesResponseType(typeof(ExchangeRateResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateRateAsync(
        [FromBody][Required] UpdateExchangeRateRequest request)
    {
        var result = await exchangeRateService.UpdateRateAsync(request);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }
}
```

**Create: `/Solution.Api/Controllers/TransactionController.cs`**

```csharp
namespace Solution.Api.Controllers;

[Authorize]
public class TransactionController(ITransactionService transactionService) : BaseController
{
    [HttpPost("buy")]
    [ProducesResponseType(typeof(TransactionResponse), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> CreateBuyTransactionAsync(
        [FromBody][Required] CreateTransactionRequest request)
    {
        var result = await transactionService.CreateBuyTransactionAsync(request);
        return result.Match(
            result => CreatedAtAction(nameof(GetTransactionByIdAsync), new { id = result.Id }, result),
            errors => Problem(errors)
        );
    }

    [HttpPost("sell")]
    [ProducesResponseType(typeof(TransactionResponse), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> CreateSellTransactionAsync(
        [FromBody][Required] CreateTransactionRequest request)
    {
        var result = await transactionService.CreateSellTransactionAsync(request);
        return result.Match(
            result => CreatedAtAction(nameof(GetTransactionByIdAsync), new { id = result.Id }, result),
            errors => Problem(errors)
        );
    }

    [HttpGet]
    [ProducesResponseType(typeof(TransactionListResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTransactionsAsync(
        [FromQuery] DateOnly? date,
        [FromQuery] Currency? currency,
        [FromQuery] TransactionType? type)
    {
        var result = await transactionService.GetTransactionsAsync(date, currency, type);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TransactionResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTransactionByIdAsync([FromRoute] int id)
    {
        var result = await transactionService.GetTransactionByIdAsync(id);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }
}
```

**Create: `/Solution.Api/Controllers/StatisticsController.cs`**

```csharp
namespace Solution.Api.Controllers;

[Authorize]
public class StatisticsController(IStatisticsService statisticsService) : BaseController
{
    [HttpGet("rates")]
    [ProducesResponseType(typeof(List<RateStatisticsResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetRateStatisticsAsync(
        [FromQuery][Required] DateOnly startDate,
        [FromQuery][Required] DateOnly endDate)
    {
        var result = await statisticsService.GetRateStatisticsAsync(startDate, endDate);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("transactions")]
    [ProducesResponseType(typeof(List<TransactionStatisticsResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTransactionStatisticsAsync(
        [FromQuery][Required] DateOnly startDate,
        [FromQuery][Required] DateOnly endDate)
    {
        var result = await statisticsService.GetTransactionStatisticsAsync(startDate, endDate);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(SummaryStatisticsResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetSummaryAsync()
    {
        var result = await statisticsService.GetSummaryAsync();
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }
}
```

**Create: `/Solution.Api/Controllers/UserController.cs`**

```csharp
namespace Solution.Api.Controllers;

[Authorize(Roles = "Administrator")]
[Route("[controller]")]
public class UserController(IUserManagementService userManagementService) : BaseController
{
    [HttpGet]
    [ProducesResponseType(typeof(UserListResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetAllUsersAsync()
    {
        var result = await userManagementService.GetAllUsersAsync();
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponseModel), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetUserByIdAsync([FromRoute] string id)
    {
        var result = await userManagementService.GetUserByIdAsync(id);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponseModel), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> CreateUserAsync(
        [FromBody][Required] CreateUserRequest request)
    {
        var result = await userManagementService.CreateUserAsync(request);
        return result.Match(
            result => CreatedAtAction(nameof(GetUserByIdAsync), new { id = result.Id }, result),
            errors => Problem(errors)
        );
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserResponseModel), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateUserAsync(
        [FromRoute] string id,
        [FromBody][Required] UpdateUserRequest request)
    {
        var result = await userManagementService.UpdateUserAsync(id, request);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpDelete("{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<IActionResult> DeleteUserAsync([FromRoute] string id)
    {
        var currentUserId = User.FindFirstValue("uid");
        var result = await userManagementService.DeleteUserAsync(id, currentUserId!);
        return result.Match(
            _ => NoContent(),
            errors => Problem(errors)
        );
    }

    [HttpPost("{id}/reset-password")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<IActionResult> ResetPasswordAsync(
        [FromRoute] string id,
        [FromBody][Required] ResetPasswordRequest request)
    {
        var result = await userManagementService.ResetPasswordAsync(id, request);
        return result.Match(
            _ => NoContent(),
            errors => Problem(errors)
        );
    }
}
```

### 6.2 Update DependencyInjectionConfiguration.cs

**Modify: `/Solution.Api/ConfigurationExtensions/DependencyInjectionConfiguration.cs`**

Add to `ConfigureDI` method:

```csharp
builder.Services.AddTransient<IExchangeRateService, ExchangeRateService>();
builder.Services.AddTransient<ITransactionService, TransactionService>();
builder.Services.AddTransient<IStatisticsService, StatisticsService>();
builder.Services.AddTransient<IUserManagementService, UserManagementService>();
```

### 6.3 Update GlobalImports.cs

**Modify: `/Solution.Api/GlobalImports.cs`**

```csharp
global using Solution.Database.Enums;
```

---

## Phase 7: Migration

After implementing all the above, run the following commands:

```bash
# Create migration
dotnet ef migrations add AddCurrencyExchangeEntities --project Solution.Database --startup-project Solution.Api

# Apply migration
dotnet ef database update --project Solution.Database --startup-project Solution.Api
```

---

## Phase 8: MAUI Desktop App (Solution.Maui)

The desktop client provides a Windows GUI for cashiers to process transactions and administrators to manage rates and users. It communicates exclusively with the REST API over HTTP using JWT authentication.

**Technology Stack:**

- .NET MAUI 10 (Windows target)
- CommunityToolkit.Mvvm (source generators, `[ObservableProperty]`, `[RelayCommand]`)
- Microsoft.Extensions.Http
- Shell-based navigation with flyout menu

### 8.1 Project File

**Create: `/Solution.Maui/Solution.Maui.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net10.0-windows10.0.19041.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>Solution.Maui</RootNamespace>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ApplicationTitle>Pénzváltó</ApplicationTitle>
    <ApplicationId>com.solution.penzvalto</ApplicationId>
    <ApplicationVersion>1</ApplicationVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
    <PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="10.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Solution.Core\Solution.Core.csproj" />
  </ItemGroup>

</Project>
```

### 8.2 Global Imports

**Create: `/Solution.Maui/GlobalImports.cs`**

```csharp
global using System.Collections.ObjectModel;
global using System.Net.Http.Headers;
global using System.Net.Http.Json;
global using System.Text.Json;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using Solution.Core.Models.Request;
global using Solution.Core.Models.Response;
global using Solution.Maui.Services;
global using Solution.Maui.Settings;
global using Solution.Maui.ViewModels;
global using Solution.Maui.Pages;
```

### 8.3 API Constants

**Create: `/Solution.Maui/Constants/ApiConstants.cs`**

```csharp
namespace Solution.Maui.Constants;

public static class ApiConstants
{
    public static class Endpoints
    {
        public const string ExchangeRate          = "exchangerate";
        public const string ExchangeRateToday     = "exchangerate/today";
        public const string Transaction           = "transaction";
        public const string TransactionBuy        = "transaction/buy";
        public const string TransactionSell       = "transaction/sell";
        public const string StatisticsSummary     = "statistics/summary";
        public const string StatisticsRates       = "statistics/rates";
        public const string StatisticsTransactions= "statistics/transactions";
        public const string User                  = "user";
    }
}
```

### 8.4 App Settings

Credentials and the API base URL are stored in `appsettings.json` and read at startup. No login UI is needed — the desktop app always runs as a named operator account configured by an administrator.

**Create: `/Solution.Maui/appsettings.json`**

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001/api/",
    "Username": "operator@penzvalto.hu",
    "Password": "ChangeMe123!"
  }
}
```

**Create: `/Solution.Maui/Settings/ApiSettings.cs`**

```csharp
namespace Solution.Maui.Settings;

public class ApiSettings
{
    public string BaseUrl  { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

### 8.5 API Client Service

The `ApiClient` wraps all HTTP communication. It reads credentials from `ApiSettings` and attaches a Basic Auth header to every request. The API's Basic Auth middleware exchanges the header for a JWT internally, so the MAUI client never needs to manage tokens.

**Create: `/Solution.Maui/Services/IApiClient.cs`**

```csharp
namespace Solution.Maui.Services;

public interface IApiClient
{
    bool IsAdmin { get; }

    // Exchange Rates
    Task<ExchangeRatesResponse?> GetTodayRatesAsync();
    Task<List<ExchangeRatesResponse>?> GetRatesHistoryAsync(DateOnly? start, DateOnly? end);
    Task<ExchangeRatesResponse?> CreateDailyRatesAsync(CreateExchangeRatesRequest request);
    Task<ExchangeRateResponse?> UpdateRateAsync(UpdateExchangeRateRequest request);

    // Transactions
    Task<TransactionResponse?> CreateBuyTransactionAsync(CreateTransactionRequest request);
    Task<TransactionResponse?> CreateSellTransactionAsync(CreateTransactionRequest request);
    Task<TransactionListResponse?> GetTransactionsAsync(DateOnly? date, string? currency, string? type);

    // Statistics
    Task<SummaryStatisticsResponse?> GetSummaryAsync();
    Task<List<RateStatisticsResponse>?> GetRateStatisticsAsync(DateOnly start, DateOnly end);
    Task<List<TransactionStatisticsResponse>?> GetTransactionStatisticsAsync(DateOnly start, DateOnly end);

    // Users (admin only)
    Task<UserListResponse?> GetAllUsersAsync();
    Task<UserResponseModel?> CreateUserAsync(CreateUserRequest request);
    Task<UserResponseModel?> UpdateUserAsync(string id, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(string id);
    Task<bool> ResetPasswordAsync(string id, ResetPasswordRequest request);
}
```

**Create: `/Solution.Maui/Services/ApiClient.cs`**

```csharp
namespace Solution.Maui.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient httpClient;
    private readonly ApiSettings settings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient httpClient, ApiSettings settings)
    {
        this.httpClient = httpClient;
        this.settings   = settings;

        // Attach Basic Auth header once for the lifetime of this client
        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{settings.Username}:{settings.Password}"));
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
    }

    // IsAdmin is derived from the configured username's role.
    // In practice, read from the role claim returned by the API on first use.
    // For simplicity we expose it as a settable property populated by DashboardViewModel.
    public bool IsAdmin { get; set; }

    // -------------------------------------------------------------------------
    // Exchange Rates
    // -------------------------------------------------------------------------

    public async Task<ExchangeRatesResponse?> GetTodayRatesAsync()
    {
        var response = await httpClient.GetAsync(ApiConstants.Endpoints.ExchangeRateToday);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ExchangeRatesResponse>(JsonOptions)
            : null;
    }

    public async Task<List<ExchangeRatesResponse>?> GetRatesHistoryAsync(DateOnly? start, DateOnly? end)
    {
        var query = BuildQuery(
            ("startDate", start?.ToString("yyyy-MM-dd")),
            ("endDate",   end?.ToString("yyyy-MM-dd"))
        );

        var response = await httpClient.GetAsync($"{ApiConstants.Endpoints.ExchangeRate}{query}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<ExchangeRatesResponse>>(JsonOptions)
            : null;
    }

    public async Task<ExchangeRatesResponse?> CreateDailyRatesAsync(CreateExchangeRatesRequest request)
    {
        var response = await httpClient.PostAsJsonAsync(ApiConstants.Endpoints.ExchangeRate, request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ExchangeRatesResponse>(JsonOptions)
            : null;
    }

    public async Task<ExchangeRateResponse?> UpdateRateAsync(UpdateExchangeRateRequest request)
    {
        var response = await httpClient.PutAsJsonAsync(ApiConstants.Endpoints.ExchangeRate, request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ExchangeRateResponse>(JsonOptions)
            : null;
    }

    // -------------------------------------------------------------------------
    // Transactions
    // -------------------------------------------------------------------------

    public async Task<TransactionResponse?> CreateBuyTransactionAsync(CreateTransactionRequest request)
    {
        var response = await httpClient.PostAsJsonAsync(ApiConstants.Endpoints.TransactionBuy, request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions)
            : null;
    }

    public async Task<TransactionResponse?> CreateSellTransactionAsync(CreateTransactionRequest request)
    {
        var response = await httpClient.PostAsJsonAsync(ApiConstants.Endpoints.TransactionSell, request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions)
            : null;
    }

    public async Task<TransactionListResponse?> GetTransactionsAsync(
        DateOnly? date, string? currency, string? type)
    {
        var query = BuildQuery(
            ("date",     date?.ToString("yyyy-MM-dd")),
            ("currency", currency),
            ("type",     type)
        );

        var response = await httpClient.GetAsync($"{ApiConstants.Endpoints.Transaction}{query}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<TransactionListResponse>(JsonOptions)
            : null;
    }

    // -------------------------------------------------------------------------
    // Statistics
    // -------------------------------------------------------------------------

    public async Task<SummaryStatisticsResponse?> GetSummaryAsync()
    {
        var response = await httpClient.GetAsync(ApiConstants.Endpoints.StatisticsSummary);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<SummaryStatisticsResponse>(JsonOptions)
            : null;
    }

    public async Task<List<RateStatisticsResponse>?> GetRateStatisticsAsync(DateOnly start, DateOnly end)
    {
        var query = BuildQuery(
            ("startDate", start.ToString("yyyy-MM-dd")),
            ("endDate",   end.ToString("yyyy-MM-dd"))
        );

        var response = await httpClient.GetAsync($"{ApiConstants.Endpoints.StatisticsRates}{query}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<RateStatisticsResponse>>(JsonOptions)
            : null;
    }

    public async Task<List<TransactionStatisticsResponse>?> GetTransactionStatisticsAsync(
        DateOnly start, DateOnly end)
    {
        var query = BuildQuery(
            ("startDate", start.ToString("yyyy-MM-dd")),
            ("endDate",   end.ToString("yyyy-MM-dd"))
        );

        var response = await httpClient.GetAsync($"{ApiConstants.Endpoints.StatisticsTransactions}{query}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<TransactionStatisticsResponse>>(JsonOptions)
            : null;
    }

    // -------------------------------------------------------------------------
    // Users
    // -------------------------------------------------------------------------

    public async Task<UserListResponse?> GetAllUsersAsync()
    {
        var response = await httpClient.GetAsync(ApiConstants.Endpoints.User);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<UserListResponse>(JsonOptions)
            : null;
    }

    public async Task<UserResponseModel?> CreateUserAsync(CreateUserRequest request)
    {
        var response = await httpClient.PostAsJsonAsync(ApiConstants.Endpoints.User, request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<UserResponseModel>(JsonOptions)
            : null;
    }

    public async Task<UserResponseModel?> UpdateUserAsync(string id, UpdateUserRequest request)
    {
        var response = await httpClient.PutAsJsonAsync($"{ApiConstants.Endpoints.User}/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<UserResponseModel>(JsonOptions)
            : null;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var response = await httpClient.DeleteAsync($"{ApiConstants.Endpoints.User}/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ResetPasswordAsync(string id, ResetPasswordRequest request)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"{ApiConstants.Endpoints.User}/{id}/reset-password", request);
        return response.IsSuccessStatusCode;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string BuildQuery(params (string Key, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => p.Value is not null)
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}");

        var joined = string.Join("&", parts);
        return joined.Length > 0 ? $"?{joined}" : string.Empty;
    }
}
```

### 8.7 Base ViewModel

**Create: `/Solution.Maui/ViewModels/BaseViewModel.cs`**

```csharp
namespace Solution.Maui.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    public bool IsNotBusy => !IsBusy;

    protected void SetError(string message)
    {
        ErrorMessage = message;
        HasError     = true;
    }

    protected void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError     = false;
    }
}
```

### 8.8 Dashboard ViewModel

**Create: `/Solution.Maui/ViewModels/DashboardViewModel.cs`**

```csharp
namespace Solution.Maui.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IApiClient apiClient;
    private readonly ApiSettings settings;

    [ObservableProperty] private bool todayRatesSet;
    [ObservableProperty] private int totalTransactionsToday;
    [ObservableProperty] private decimal totalHufVolumeToday;
    [ObservableProperty] private bool isAdmin;
    [ObservableProperty] private ObservableCollection<CurrencySummary> currencySummaries = new();

    public DashboardViewModel(IApiClient apiClient, ApiSettings settings)
    {
        this.apiClient = apiClient;
        this.settings  = settings;
        Title = "Irányítópult";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            var summary = await apiClient.GetSummaryAsync();
            if (summary is null)
            {
                SetError("Nem sikerült betölteni az összesítőt.");
                return;
            }

            // Reflect admin state back onto the shared ApiClient so Shell can use it
            IsAdmin = apiClient.IsAdmin;

            TodayRatesSet          = summary.TodayRatesSet;
            TotalTransactionsToday = summary.TotalTransactionsToday;
            TotalHufVolumeToday    = summary.TotalHufVolumeToday;

            CurrencySummaries.Clear();
            foreach (var item in summary.TransactionsByCurrency)
                CurrencySummaries.Add(item);
        }
        catch (Exception ex)
        {
            SetError($"Hiba: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 8.9 Exchange Rates ViewModel

**Create: `/Solution.Maui/ViewModels/ExchangeRatesViewModel.cs`**

```csharp
namespace Solution.Maui.ViewModels;

public partial class ExchangeRatesViewModel : BaseViewModel
{
    private readonly IApiClient apiClient;

    // Today's rate display
    [ObservableProperty] private ObservableCollection<ExchangeRateResponse> todayRates = new();
    [ObservableProperty] private bool ratesExistToday;

    // Create daily rates form
    [ObservableProperty] private decimal usdBuyRate;
    [ObservableProperty] private decimal usdSellRate;
    [ObservableProperty] private decimal gbpBuyRate;
    [ObservableProperty] private decimal gbpSellRate;
    [ObservableProperty] private decimal chfBuyRate;
    [ObservableProperty] private decimal chfSellRate;

    // Update single rate form
    [ObservableProperty] private ExchangeRateResponse? selectedRate;
    [ObservableProperty] private decimal updateBuyRate;
    [ObservableProperty] private decimal updateSellRate;

    // History
    [ObservableProperty] private ObservableCollection<ExchangeRatesResponse> ratesHistory = new();
    [ObservableProperty] private DateTime historyStartDate = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime historyEndDate   = DateTime.Today;

    public ExchangeRatesViewModel(IApiClient apiClient)
    {
        this.apiClient = apiClient;
        Title = "Árfolyamok";
    }

    [RelayCommand]
    private async Task LoadTodayAsync()
    {
        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            var result = await apiClient.GetTodayRatesAsync();
            TodayRates.Clear();

            if (result is not null)
            {
                foreach (var rate in result.Rates)
                    TodayRates.Add(rate);
                RatesExistToday = true;
            }
            else
            {
                RatesExistToday = false;
            }
        }
        catch (Exception ex)
        {
            SetError($"Hiba: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateDailyRatesAsync()
    {
        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            var request = new CreateExchangeRatesRequest
            {
                Date       = DateOnly.FromDateTime(DateTime.Today),
                UsdBuyRate = UsdBuyRate, UsdSellRate = UsdSellRate,
                GbpBuyRate = GbpBuyRate, GbpSellRate = GbpSellRate,
                ChfBuyRate = ChfBuyRate, ChfSellRate = ChfSellRate
            };

            var result = await apiClient.CreateDailyRatesAsync(request);
            if (result is not null)
            {
                await LoadTodayAsync();
                await Shell.Current.DisplayAlert("Siker", "Mai árfolyamok mentve.", "OK");
            }
            else
            {
                SetError("Nem sikerült az árfolyamokat létrehozni. Ellenőrizze az értékeket.");
            }
        }
        catch (Exception ex)
        {
            SetError($"Hiba: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UpdateRateAsync()
    {
        if (SelectedRate is null || IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            if (!Enum.TryParse<Solution.Database.Enums.Currency>(SelectedRate.Currency, out var currency))
            {
                SetError("Érvénytelen deviza.");
                return;
            }

            var request = new UpdateExchangeRateRequest
            {
                Currency = currency,
                BuyRate  = UpdateBuyRate,
                SellRate = UpdateSellRate
            };

            var result = await apiClient.UpdateRateAsync(request);
            if (result is not null)
            {
                await LoadTodayAsync();
                await Shell.Current.DisplayAlert("Siker", $"{SelectedRate.Currency} árfolyam frissítve.", "OK");
            }
            else
            {
                SetError("Nem sikerült az árfolyamot frissíteni.");
            }
        }
        catch (Exception ex)
        {
            SetError($"Hiba: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedRateChanged(ExchangeRateResponse? value)
    {
        if (value is null) return;
        UpdateBuyRate  = value.BuyRate;
        UpdateSellRate = value.SellRate;
    }

    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            var start  = DateOnly.FromDateTime(HistoryStartDate);
            var end    = DateOnly.FromDateTime(HistoryEndDate);
            var result = await apiClient.GetRatesHistoryAsync(start, end);

            RatesHistory.Clear();
            if (result is not null)
                foreach (var item in result)
                    RatesHistory.Add(item);
        }
        catch (Exception ex)
        {
            SetError($"Hiba: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 8.10 Transactions ViewModel

**Create: `/Solution.Maui/ViewModels/TransactionsViewModel.cs`**

```csharp
namespace Solution.Maui.ViewModels;

public partial class TransactionsViewModel : BaseViewModel
{
    private readonly IApiClient apiClient;

    // Transaction list
    [ObservableProperty] private ObservableCollection<TransactionResponse> transactions = new();
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private DateTime filterDate = DateTime.Today;
    [ObservableProperty] private string selectedCurrencyFilter = string.Empty;
    [ObservableProperty] private string selectedTypeFilter = string.Empty;

    // New transaction form
    [ObservableProperty] private string selectedCurrency = "USD";
    [ObservableProperty] private decimal foreignAmount;
    [ObservableProperty] private string customerName = string.Empty;
    [ObservableProperty] private string selectedIdType = "PersonalIdCard";
    [ObservableProperty] private string customerIdNumber = string.Empty;

    // Last created transaction receipt
    [ObservableProperty] private TransactionResponse? lastTransaction;
    [ObservableProperty] private bool showReceipt;

    public List<string> Currencies    { get; } = new() { "USD", "GBP", "CHF" };
    public List<string> IdTypes       { get; } = new() { "PersonalIdCard", "Passport", "DrivingLicense" };
    public List<string> CurrencyFilters { get; } = new() { "", "USD", "GBP", "CHF" };
    public List<string> TypeFilters     { get; } = new() { "", "Buy", "Sell" };

    public TransactionsViewModel(IApiClient apiClient)
    {
        this.apiClient = apiClient;
        Title = "Tranzakciók";
    }

    [RelayCommand]
    private async Task LoadTransactionsAsync()
    {
        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            var date     = DateOnly.FromDateTime(FilterDate);
            var currency = string.IsNullOrEmpty(SelectedCurrencyFilter) ? null : SelectedCurrencyFilter;
            var type     = string.IsNullOrEmpty(SelectedTypeFilter)     ? null : SelectedTypeFilter;

            var result = await apiClient.GetTransactionsAsync(date, currency, type);

            Transactions.Clear();
            if (result is not null)
            {
                foreach (var t in result.Transactions)
                    Transactions.Add(t);
                TotalCount = result.TotalCount;
            }
        }
        catch (Exception ex)
        {
            SetError($"Hiba: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateBuyAsync()
        => await ProcessTransactionAsync(isBuy: true);

    [RelayCommand]
    private async Task CreateSellAsync()
        => await ProcessTransactionAsync(isBuy: false);

    private async Task ProcessTransactionAsync(bool isBuy)
    {
        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            if (!Enum.TryParse<Solution.Database.Enums.Currency>(SelectedCurrency, out var currency))
            {
                SetError("Érvénytelen deviza."); return;
            }
            if (!Enum.TryParse<Solution.Database.Enums.CustomerIdType>(SelectedIdType, out var idType))
            {
                SetError("Érvénytelen igazolványtípus."); return;
            }

            var request = new CreateTransactionRequest
            {
                Currency       = currency,
                ForeignAmount  = ForeignAmount,
                CustomerName   = CustomerName,
                CustomerIdType = idType,
                CustomerIdNumber = CustomerIdNumber
            };

            var result = isBuy
                ? await apiClient.CreateBuyTransactionAsync(request)
                : await apiClient.CreateSellTransactionAsync(request);

            if (result is not null)
            {
                LastTransaction = result;
                ShowReceipt     = true;
                ResetForm();
                await LoadTransactionsAsync();
            }
            else
            {
                SetError("Nem sikerült a tranzakciót létrehozni. Ellenőrizze, hogy a mai árfolyamok be vannak-e állítva.");
            }
        }
        catch (Exception ex)
        {
            SetError($"Hiba: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void DismissReceipt() => ShowReceipt = false;

    private void ResetForm()
    {
        ForeignAmount    = 0;
        CustomerName     = string.Empty;
        CustomerIdNumber = string.Empty;
    }
}
```

### 8.11 Users ViewModel

**Create: `/Solution.Maui/ViewModels/UsersViewModel.cs`**

```csharp
namespace Solution.Maui.ViewModels;

public partial class UsersViewModel : BaseViewModel
{
    private readonly IApiClient apiClient;

    [ObservableProperty] private ObservableCollection<UserResponseModel> users = new();
    [ObservableProperty] private UserResponseModel? selectedUser;

    // Create user form
    [ObservableProperty] private string newName     = string.Empty;
    [ObservableProperty] private string newEmail    = string.Empty;
    [ObservableProperty] private string newPassword = string.Empty;
    [ObservableProperty] private string newRole     = "Cashier";

    // Reset password form
    [ObservableProperty] private string resetPasswordValue = string.Empty;

    public List<string> Roles { get; } = new() { "Cashier", "Administrator" };

    public UsersViewModel(IApiClient apiClient)
    {
        this.apiClient = apiClient;
        Title = "Felhasználók";
    }

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            var result = await apiClient.GetAllUsersAsync();
            Users.Clear();
            if (result is not null)
                foreach (var u in result.Users)
                    Users.Add(u);
        }
        catch (Exception ex)
        {
            SetError($"Hiba: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateUserAsync()
    {
        if (IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            if (!Enum.TryParse<Solution.Database.Enums.UserRole>(NewRole, out var role))
            {
                SetError("Érvénytelen szerepkör."); return;
            }

            var request = new CreateUserRequest
            {
                Name     = NewName,
                Email    = NewEmail,
                Password = NewPassword,
                Role     = role
            };

            var result = await apiClient.CreateUserAsync(request);
            if (result is not null)
            {
                await LoadUsersAsync();
                ClearNewUserForm();
                await Shell.Current.DisplayAlert("Siker", $"Felhasználó {NewEmail} létrehozva.", "OK");
            }
            else
            {
                SetError("Nem sikerült a felhasználót létrehozni. Lehetséges, hogy az e-mail már foglalt.");
            }
        }
        catch (Exception ex)
        {
            SetError($"Hiba: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteUserAsync(UserResponseModel user)
    {
        if (IsBusy) return;

        var confirmed = await Shell.Current.DisplayAlert(
            "Megerősítés",
            $"Biztosan törölni szeretné: {user.Name}?",
            "Törlés", "Mégse");

        if (!confirmed) return;

        ClearError();
        IsBusy = true;

        try
        {
            var success = await apiClient.DeleteUserAsync(user.Id);
            if (success)
                await LoadUsersAsync();
            else
                SetError("Nem sikerült törölni. Nem törölheti saját fiókját.");
        }
        catch (Exception ex)
        {
            SetError($"Hiba: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        if (SelectedUser is null || IsBusy) return;
        ClearError();
        IsBusy = true;

        try
        {
            var request = new ResetPasswordRequest { NewPassword = ResetPasswordValue };
            var success = await apiClient.ResetPasswordAsync(SelectedUser.Id, request);

            if (success)
            {
                ResetPasswordValue = string.Empty;
                await Shell.Current.DisplayAlert("Siker", "Jelszó sikeresen visszaállítva.", "OK");
            }
            else
            {
                SetError("Nem sikerült a jelszót visszaállítani.");
            }
        }
        catch (Exception ex)
        {
            SetError($"Hiba: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearNewUserForm()
    {
        NewName     = string.Empty;
        NewEmail    = string.Empty;
        NewPassword = string.Empty;
    }
}
```

### 8.12 Pages

**Create: `/Solution.Maui/Pages/DashboardPage.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:Solution.Maui.ViewModels"
             xmlns:model="clr-namespace:Solution.Core.Models.Response;assembly=Solution.Core"
             x:Class="Solution.Maui.Pages.DashboardPage"
             x:DataType="vm:DashboardViewModel"
             Title="{Binding Title}">

    <ScrollView Padding="24">
        <VerticalStackLayout Spacing="20">

            <!-- Header -->
            <Label Text="Irányítópult" FontSize="24" FontAttributes="Bold" TextColor="#1A237E" />

            <!-- Error Banner -->
            <Frame BackgroundColor="#FFEBEE" CornerRadius="6" Padding="12,8"
                   IsVisible="{Binding HasError}">
                <Label Text="{Binding ErrorMessage}" TextColor="#C62828" FontSize="13" />
            </Frame>

            <!-- Summary Cards -->
            <Grid ColumnDefinitions="*,*,*" ColumnSpacing="16">

                <!-- Rates Status -->
                <Frame Grid.Column="0" CornerRadius="10" Padding="16" HasShadow="True"
                       BackgroundColor="{Binding TodayRatesSet, Converter={StaticResource BoolToColorConverter}}">
                    <VerticalStackLayout Spacing="6">
                        <Label Text="Mai árfolyamok" FontSize="13" TextColor="#424242" />
                        <Label Text="{Binding TodayRatesSet, Converter={StaticResource BoolToStatusConverter}}"
                               FontSize="20" FontAttributes="Bold" />
                    </VerticalStackLayout>
                </Frame>

                <!-- Transactions Today -->
                <Frame Grid.Column="1" CornerRadius="10" Padding="16" HasShadow="True" BackgroundColor="White">
                    <VerticalStackLayout Spacing="6">
                        <Label Text="Mai tranzakciók" FontSize="13" TextColor="#424242" />
                        <Label Text="{Binding TotalTransactionsToday}" FontSize="28" FontAttributes="Bold" TextColor="#1A237E" />
                    </VerticalStackLayout>
                </Frame>

                <!-- HUF Volume -->
                <Frame Grid.Column="2" CornerRadius="10" Padding="16" HasShadow="True" BackgroundColor="White">
                    <VerticalStackLayout Spacing="6">
                        <Label Text="Mai forgalom (HUF)" FontSize="13" TextColor="#424242" />
                        <Label Text="{Binding TotalHufVolumeToday, StringFormat='{0:N0} Ft'}"
                               FontSize="20" FontAttributes="Bold" TextColor="#2E7D32" />
                    </VerticalStackLayout>
                </Frame>

            </Grid>

            <!-- Currency Breakdown Table -->
            <Frame CornerRadius="10" Padding="16" HasShadow="True" BackgroundColor="White">
                <VerticalStackLayout Spacing="12">
                    <Label Text="Devizánkénti bontás" FontSize="16" FontAttributes="Bold" TextColor="#1A237E" />

                    <!-- Table Header -->
                    <Grid ColumnDefinitions="*,*,*,*" Padding="0,0,0,8">
                        <Label Grid.Column="0" Text="Deviza"   FontAttributes="Bold" TextColor="#757575" FontSize="12" />
                        <Label Grid.Column="1" Text="Vétel db" FontAttributes="Bold" TextColor="#757575" FontSize="12" />
                        <Label Grid.Column="2" Text="Eladás db"FontAttributes="Bold" TextColor="#757575" FontSize="12" />
                        <Label Grid.Column="3" Text="Forgalom" FontAttributes="Bold" TextColor="#757575" FontSize="12" />
                    </Grid>

                    <BoxView HeightRequest="1" BackgroundColor="#E0E0E0" />

                    <CollectionView ItemsSource="{Binding CurrencySummaries}">
                        <CollectionView.ItemTemplate>
                            <DataTemplate x:DataType="model:CurrencySummary">
                                <Grid ColumnDefinitions="*,*,*,*" Padding="0,6">
                                    <Label Grid.Column="0" Text="{Binding Currency}" FontAttributes="Bold" />
                                    <Label Grid.Column="1" Text="{Binding BuyCount}"  TextColor="#1565C0" />
                                    <Label Grid.Column="2" Text="{Binding SellCount}" TextColor="#6A1B9A" />
                                    <Label Grid.Column="3" Text="{Binding TotalHufVolume, StringFormat='{0:N0} Ft'}" />
                                </Grid>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>
                </VerticalStackLayout>
            </Frame>

            <ActivityIndicator IsRunning="{Binding IsBusy}" IsVisible="{Binding IsBusy}"
                               Color="#1A237E" HorizontalOptions="Center" />

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

**Create: `/Solution.Maui/Pages/DashboardPage.xaml.cs`**

```csharp
namespace Solution.Maui.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadCommand.Execute(null);
    }
}
```

**Create: `/Solution.Maui/Pages/ExchangeRatesPage.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:Solution.Maui.ViewModels"
             xmlns:model="clr-namespace:Solution.Core.Models.Response;assembly=Solution.Core"
             x:Class="Solution.Maui.Pages.ExchangeRatesPage"
             x:DataType="vm:ExchangeRatesViewModel"
             Title="{Binding Title}">

    <ScrollView Padding="24">
        <VerticalStackLayout Spacing="20">

            <Label Text="Árfolyamok" FontSize="24" FontAttributes="Bold" TextColor="#1A237E" />

            <!-- Error Banner -->
            <Frame BackgroundColor="#FFEBEE" CornerRadius="6" Padding="12,8"
                   IsVisible="{Binding HasError}">
                <Label Text="{Binding ErrorMessage}" TextColor="#C62828" FontSize="13" />
            </Frame>

            <!-- Today's Rates Panel -->
            <Frame CornerRadius="10" Padding="20" HasShadow="True" BackgroundColor="White">
                <VerticalStackLayout Spacing="14">
                    <Grid ColumnDefinitions="*, Auto">
                        <Label Grid.Column="0" Text="Mai árfolyamok"
                               FontSize="16" FontAttributes="Bold" TextColor="#1A237E" />
                        <Button Grid.Column="1" Text="Frissítés"
                                Command="{Binding LoadTodayCommand}"
                                BackgroundColor="#E3F2FD" TextColor="#1565C0"
                                CornerRadius="6" HeightRequest="32" FontSize="12" />
                    </Grid>

                    <!-- Rates Table -->
                    <Grid ColumnDefinitions="*,*,*,*" Padding="0,0,0,6">
                        <Label Grid.Column="0" Text="Deviza"  FontAttributes="Bold" TextColor="#757575" FontSize="12" />
                        <Label Grid.Column="1" Text="Vétel"   FontAttributes="Bold" TextColor="#757575" FontSize="12" />
                        <Label Grid.Column="2" Text="Eladás"  FontAttributes="Bold" TextColor="#757575" FontSize="12" />
                        <Label Grid.Column="3" Text="Módosítva" FontAttributes="Bold" TextColor="#757575" FontSize="12" />
                    </Grid>
                    <BoxView HeightRequest="1" BackgroundColor="#E0E0E0" />

                    <CollectionView ItemsSource="{Binding TodayRates}"
                                    SelectionMode="Single"
                                    SelectedItem="{Binding SelectedRate}">
                        <CollectionView.ItemTemplate>
                            <DataTemplate x:DataType="model:ExchangeRateResponse">
                                <Grid ColumnDefinitions="*,*,*,*" Padding="0,8">
                                    <Label Grid.Column="0" Text="{Binding Currency}" FontAttributes="Bold" />
                                    <Label Grid.Column="1" Text="{Binding BuyRate,  StringFormat='{0:N4}'}" />
                                    <Label Grid.Column="2" Text="{Binding SellRate, StringFormat='{0:N4}'}" />
                                    <Label Grid.Column="3"
                                           Text="{Binding ModifiedAt, StringFormat='{0:HH:mm}'}"
                                           TextColor="#9E9E9E" FontSize="12" />
                                </Grid>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>

                    <!-- No rates message -->
                    <Label Text="Még nincsenek mai árfolyamok beállítva."
                           IsVisible="{Binding RatesExistToday, Converter={StaticResource InverseBoolConverter}}"
                           TextColor="#FF6F00" FontSize="13" HorizontalTextAlignment="Center" />
                </VerticalStackLayout>
            </Frame>

            <!-- Create Daily Rates Panel (shown when no rates exist today) -->
            <Frame CornerRadius="10" Padding="20" HasShadow="True" BackgroundColor="White"
                   IsVisible="{Binding RatesExistToday, Converter={StaticResource InverseBoolConverter}}">
                <VerticalStackLayout Spacing="14">
                    <Label Text="Mai árfolyamok beállítása" FontSize="16" FontAttributes="Bold" TextColor="#1A237E" />

                    <!-- USD Row -->
                    <Grid ColumnDefinitions="80,*,*" ColumnSpacing="12">
                        <Label Grid.Column="0" Text="USD" FontAttributes="Bold" VerticalOptions="Center" />
                        <VerticalStackLayout Grid.Column="1" Spacing="4">
                            <Label Text="Vétel" FontSize="12" TextColor="#757575" />
                            <Entry Text="{Binding UsdBuyRate}" Keyboard="Numeric" Placeholder="0.0000" />
                        </VerticalStackLayout>
                        <VerticalStackLayout Grid.Column="2" Spacing="4">
                            <Label Text="Eladás" FontSize="12" TextColor="#757575" />
                            <Entry Text="{Binding UsdSellRate}" Keyboard="Numeric" Placeholder="0.0000" />
                        </VerticalStackLayout>
                    </Grid>

                    <!-- GBP Row -->
                    <Grid ColumnDefinitions="80,*,*" ColumnSpacing="12">
                        <Label Grid.Column="0" Text="GBP" FontAttributes="Bold" VerticalOptions="Center" />
                        <VerticalStackLayout Grid.Column="1" Spacing="4">
                            <Label Text="Vétel" FontSize="12" TextColor="#757575" />
                            <Entry Text="{Binding GbpBuyRate}" Keyboard="Numeric" Placeholder="0.0000" />
                        </VerticalStackLayout>
                        <VerticalStackLayout Grid.Column="2" Spacing="4">
                            <Label Text="Eladás" FontSize="12" TextColor="#757575" />
                            <Entry Text="{Binding GbpSellRate}" Keyboard="Numeric" Placeholder="0.0000" />
                        </VerticalStackLayout>
                    </Grid>

                    <!-- CHF Row -->
                    <Grid ColumnDefinitions="80,*,*" ColumnSpacing="12">
                        <Label Grid.Column="0" Text="CHF" FontAttributes="Bold" VerticalOptions="Center" />
                        <VerticalStackLayout Grid.Column="1" Spacing="4">
                            <Label Text="Vétel" FontSize="12" TextColor="#757575" />
                            <Entry Text="{Binding ChfBuyRate}" Keyboard="Numeric" Placeholder="0.0000" />
                        </VerticalStackLayout>
                        <VerticalStackLayout Grid.Column="2" Spacing="4">
                            <Label Text="Eladás" FontSize="12" TextColor="#757575" />
                            <Entry Text="{Binding ChfSellRate}" Keyboard="Numeric" Placeholder="0.0000" />
                        </VerticalStackLayout>
                    </Grid>

                    <Button Text="Árfolyamok mentése"
                            Command="{Binding CreateDailyRatesCommand}"
                            IsEnabled="{Binding IsNotBusy}"
                            BackgroundColor="#1A237E" TextColor="White"
                            CornerRadius="8" HeightRequest="48" />
                </VerticalStackLayout>
            </Frame>

            <!-- Update Single Rate Panel (shown when rates exist) -->
            <Frame CornerRadius="10" Padding="20" HasShadow="True" BackgroundColor="White"
                   IsVisible="{Binding RatesExistToday}">
                <VerticalStackLayout Spacing="14">
                    <Label Text="Árfolyam módosítása" FontSize="16" FontAttributes="Bold" TextColor="#1A237E" />
                    <Label Text="Válasszon devizát a fenti táblázatból a módosításhoz."
                           FontSize="12" TextColor="#757575" />

                    <Grid ColumnDefinitions="*,*" ColumnSpacing="12"
                          IsVisible="{Binding SelectedRate, Converter={StaticResource NullToBoolConverter}}">
                        <VerticalStackLayout Grid.Column="0" Spacing="4">
                            <Label Text="Új vétel" FontSize="12" TextColor="#757575" />
                            <Entry Text="{Binding UpdateBuyRate}" Keyboard="Numeric" />
                        </VerticalStackLayout>
                        <VerticalStackLayout Grid.Column="1" Spacing="4">
                            <Label Text="Új eladás" FontSize="12" TextColor="#757575" />
                            <Entry Text="{Binding UpdateSellRate}" Keyboard="Numeric" />
                        </VerticalStackLayout>
                    </Grid>

                    <Button Text="Módosítás mentése"
                            Command="{Binding UpdateRateCommand}"
                            IsEnabled="{Binding IsNotBusy}"
                            IsVisible="{Binding SelectedRate, Converter={StaticResource NullToBoolConverter}}"
                            BackgroundColor="#E65100" TextColor="White"
                            CornerRadius="8" HeightRequest="44" />
                </VerticalStackLayout>
            </Frame>

            <ActivityIndicator IsRunning="{Binding IsBusy}" IsVisible="{Binding IsBusy}"
                               Color="#1A237E" HorizontalOptions="Center" />

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

**Create: `/Solution.Maui/Pages/ExchangeRatesPage.xaml.cs`**

```csharp
namespace Solution.Maui.Pages;

public partial class ExchangeRatesPage : ContentPage
{
    private readonly ExchangeRatesViewModel viewModel;

    public ExchangeRatesPage(ExchangeRatesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadTodayCommand.Execute(null);
    }
}
```

**Create: `/Solution.Maui/Pages/TransactionsPage.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:Solution.Maui.ViewModels"
             xmlns:model="clr-namespace:Solution.Core.Models.Response;assembly=Solution.Core"
             x:Class="Solution.Maui.Pages.TransactionsPage"
             x:DataType="vm:TransactionsViewModel"
             Title="{Binding Title}">

    <Grid RowDefinitions="Auto,*" ColumnDefinitions="420,*" ColumnSpacing="20" Padding="24">

        <!-- Left Panel: New Transaction Form -->
        <ScrollView Grid.Row="0" Grid.RowSpan="2" Grid.Column="0">
            <VerticalStackLayout Spacing="16">

                <Label Text="Új tranzakció" FontSize="20" FontAttributes="Bold" TextColor="#1A237E" />

                <!-- Error Banner -->
                <Frame BackgroundColor="#FFEBEE" CornerRadius="6" Padding="12,8"
                       IsVisible="{Binding HasError}">
                    <Label Text="{Binding ErrorMessage}" TextColor="#C62828" FontSize="13" />
                </Frame>

                <Frame CornerRadius="10" Padding="20" HasShadow="True" BackgroundColor="White">
                    <VerticalStackLayout Spacing="14">

                        <!-- Currency Picker -->
                        <VerticalStackLayout Spacing="4">
                            <Label Text="Deviza" FontSize="13" TextColor="#424242" />
                            <Picker ItemsSource="{Binding Currencies}"
                                    SelectedItem="{Binding SelectedCurrency}" />
                        </VerticalStackLayout>

                        <!-- Amount -->
                        <VerticalStackLayout Spacing="4">
                            <Label Text="Összeg (devizában)" FontSize="13" TextColor="#424242" />
                            <Entry Text="{Binding ForeignAmount}" Keyboard="Numeric" Placeholder="0.00" />
                        </VerticalStackLayout>

                        <BoxView HeightRequest="1" BackgroundColor="#E0E0E0" />

                        <!-- Customer Name -->
                        <VerticalStackLayout Spacing="4">
                            <Label Text="Ügyfél neve" FontSize="13" TextColor="#424242" />
                            <Entry Text="{Binding CustomerName}" Placeholder="Teljes név" />
                        </VerticalStackLayout>

                        <!-- ID Type Picker -->
                        <VerticalStackLayout Spacing="4">
                            <Label Text="Igazolványtípus" FontSize="13" TextColor="#424242" />
                            <Picker ItemsSource="{Binding IdTypes}"
                                    SelectedItem="{Binding SelectedIdType}" />
                        </VerticalStackLayout>

                        <!-- ID Number -->
                        <VerticalStackLayout Spacing="4">
                            <Label Text="Igazolvány száma" FontSize="13" TextColor="#424242" />
                            <Entry Text="{Binding CustomerIdNumber}" Placeholder="Pl. 123456AB" />
                        </VerticalStackLayout>

                        <!-- Action Buttons -->
                        <Grid ColumnDefinitions="*,*" ColumnSpacing="12" Margin="0,8,0,0">
                            <Button Grid.Column="0"
                                    Text="VÉTEL (ügyfél vesz)"
                                    Command="{Binding CreateBuyCommand}"
                                    IsEnabled="{Binding IsNotBusy}"
                                    BackgroundColor="#1565C0" TextColor="White"
                                    CornerRadius="8" HeightRequest="50"
                                    FontSize="12" />
                            <Button Grid.Column="1"
                                    Text="ELADÁS (ügyfél ad)"
                                    Command="{Binding CreateSellCommand}"
                                    IsEnabled="{Binding IsNotBusy}"
                                    BackgroundColor="#6A1B9A" TextColor="White"
                                    CornerRadius="8" HeightRequest="50"
                                    FontSize="12" />
                        </Grid>

                    </VerticalStackLayout>
                </Frame>

                <!-- Receipt Panel -->
                <Frame CornerRadius="10" Padding="20" HasShadow="True"
                       BackgroundColor="#E8F5E9"
                       IsVisible="{Binding ShowReceipt}">
                    <VerticalStackLayout Spacing="10">
                        <Label Text="✓ Tranzakció sikeres" FontSize="16" FontAttributes="Bold"
                               TextColor="#2E7D32" />
                        <BoxView HeightRequest="1" BackgroundColor="#A5D6A7" />
                        <Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto,Auto,Auto,Auto" ColumnSpacing="12" RowSpacing="6">
                            <Label Grid.Row="0" Grid.Column="0" Text="Típus:"   TextColor="#424242" FontSize="13" />
                            <Label Grid.Row="0" Grid.Column="1" Text="{Binding LastTransaction.Type}" FontAttributes="Bold" FontSize="13" />
                            <Label Grid.Row="1" Grid.Column="0" Text="Deviza:"  TextColor="#424242" FontSize="13" />
                            <Label Grid.Row="1" Grid.Column="1" Text="{Binding LastTransaction.Currency}" FontAttributes="Bold" FontSize="13" />
                            <Label Grid.Row="2" Grid.Column="0" Text="Deviza összeg:" TextColor="#424242" FontSize="13" />
                            <Label Grid.Row="2" Grid.Column="1" Text="{Binding LastTransaction.ForeignAmount, StringFormat='{0:N2}'}" FontAttributes="Bold" FontSize="13" />
                            <Label Grid.Row="3" Grid.Column="0" Text="HUF összeg:" TextColor="#424242" FontSize="13" />
                            <Label Grid.Row="3" Grid.Column="1" Text="{Binding LastTransaction.HufAmount, StringFormat='{0:N0} Ft'}" FontAttributes="Bold" TextColor="#2E7D32" FontSize="13" />
                            <Label Grid.Row="4" Grid.Column="0" Text="Árfolyam:" TextColor="#424242" FontSize="13" />
                            <Label Grid.Row="4" Grid.Column="1" Text="{Binding LastTransaction.AppliedRate, StringFormat='{0:N4}'}" FontSize="13" />
                        </Grid>
                        <Button Text="Bezárás" Command="{Binding DismissReceiptCommand}"
                                BackgroundColor="#A5D6A7" TextColor="#1B5E20"
                                CornerRadius="6" HeightRequest="36" FontSize="12" />
                    </VerticalStackLayout>
                </Frame>

                <ActivityIndicator IsRunning="{Binding IsBusy}" IsVisible="{Binding IsBusy}"
                                   Color="#1A237E" HorizontalOptions="Center" />

            </VerticalStackLayout>
        </ScrollView>

        <!-- Right Panel: Transaction List -->
        <VerticalStackLayout Grid.Row="0" Grid.RowSpan="2" Grid.Column="1" Spacing="12">

            <Label Text="Tranzakciólista" FontSize="20" FontAttributes="Bold" TextColor="#1A237E" />

            <!-- Filters -->
            <Frame CornerRadius="8" Padding="14" HasShadow="True" BackgroundColor="White">
                <Grid ColumnDefinitions="*,*,*,Auto" ColumnSpacing="12">
                    <VerticalStackLayout Grid.Column="0" Spacing="4">
                        <Label Text="Dátum" FontSize="12" TextColor="#757575" />
                        <DatePicker Date="{Binding FilterDate}" Format="yyyy-MM-dd" />
                    </VerticalStackLayout>
                    <VerticalStackLayout Grid.Column="1" Spacing="4">
                        <Label Text="Deviza" FontSize="12" TextColor="#757575" />
                        <Picker ItemsSource="{Binding CurrencyFilters}"
                                SelectedItem="{Binding SelectedCurrencyFilter}" />
                    </VerticalStackLayout>
                    <VerticalStackLayout Grid.Column="2" Spacing="4">
                        <Label Text="Típus" FontSize="12" TextColor="#757575" />
                        <Picker ItemsSource="{Binding TypeFilters}"
                                SelectedItem="{Binding SelectedTypeFilter}" />
                    </VerticalStackLayout>
                    <Button Grid.Column="3"
                            Text="Szűrés"
                            Command="{Binding LoadTransactionsCommand}"
                            BackgroundColor="#1A237E" TextColor="White"
                            CornerRadius="6" HeightRequest="40"
                            VerticalOptions="End" />
                </Grid>
            </Frame>

            <Label Text="{Binding TotalCount, StringFormat='Összesen: {0} tranzakció'}"
                   FontSize="12" TextColor="#757575" />

            <!-- Transactions CollectionView -->
            <CollectionView ItemsSource="{Binding Transactions}" VerticalScrollBarVisibility="Always">
                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="model:TransactionResponse">
                        <Frame Margin="0,4" Padding="14" CornerRadius="8" HasShadow="False"
                               BackgroundColor="White">
                            <Grid ColumnDefinitions="60,*,*,*,*" ColumnSpacing="8">
                                <!-- Type Badge -->
                                <Frame Grid.Column="0" Padding="6,2" CornerRadius="4"
                                       BackgroundColor="{Binding Type, Converter={StaticResource TypeToColorConverter}}">
                                    <Label Text="{Binding Type}" FontSize="11" FontAttributes="Bold"
                                           TextColor="White" HorizontalTextAlignment="Center" />
                                </Frame>
                                <VerticalStackLayout Grid.Column="1" Spacing="2">
                                    <Label Text="{Binding Currency}" FontAttributes="Bold" FontSize="14" />
                                    <Label Text="{Binding TransactionDate, StringFormat='{0:HH:mm}'}"
                                           FontSize="11" TextColor="#9E9E9E" />
                                </VerticalStackLayout>
                                <VerticalStackLayout Grid.Column="2" Spacing="2">
                                    <Label Text="{Binding ForeignAmount, StringFormat='{0:N2}'}" FontSize="13" />
                                    <Label Text="deviza" FontSize="11" TextColor="#9E9E9E" />
                                </VerticalStackLayout>
                                <VerticalStackLayout Grid.Column="3" Spacing="2">
                                    <Label Text="{Binding HufAmount, StringFormat='{0:N0} Ft'}"
                                           FontAttributes="Bold" FontSize="13" TextColor="#2E7D32" />
                                    <Label Text="{Binding AppliedRate, StringFormat='@ {0:N4}'}"
                                           FontSize="11" TextColor="#9E9E9E" />
                                </VerticalStackLayout>
                                <VerticalStackLayout Grid.Column="4" Spacing="2">
                                    <Label Text="{Binding CustomerName}" FontSize="12" />
                                    <Label Text="{Binding CustomerIdNumber}" FontSize="11" TextColor="#9E9E9E" />
                                </VerticalStackLayout>
                            </Grid>
                        </Frame>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>

        </VerticalStackLayout>

    </Grid>

</ContentPage>
```

**Create: `/Solution.Maui/Pages/TransactionsPage.xaml.cs`**

```csharp
namespace Solution.Maui.Pages;

public partial class TransactionsPage : ContentPage
{
    private readonly TransactionsViewModel viewModel;

    public TransactionsPage(TransactionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadTransactionsCommand.Execute(null);
    }
}
```

**Create: `/Solution.Maui/Pages/UsersPage.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:Solution.Maui.ViewModels"
             xmlns:model="clr-namespace:Solution.Core.Models.Response;assembly=Solution.Core"
             x:Class="Solution.Maui.Pages.UsersPage"
             x:DataType="vm:UsersViewModel"
             Title="{Binding Title}">

    <Grid ColumnDefinitions="360,*" ColumnSpacing="20" Padding="24">

        <!-- Left: Create User Form -->
        <ScrollView Grid.Column="0">
            <VerticalStackLayout Spacing="16">
                <Label Text="Felhasználók" FontSize="20" FontAttributes="Bold" TextColor="#1A237E" />

                <!-- Error Banner -->
                <Frame BackgroundColor="#FFEBEE" CornerRadius="6" Padding="12,8"
                       IsVisible="{Binding HasError}">
                    <Label Text="{Binding ErrorMessage}" TextColor="#C62828" FontSize="13" />
                </Frame>

                <!-- Create User -->
                <Frame CornerRadius="10" Padding="20" HasShadow="True" BackgroundColor="White">
                    <VerticalStackLayout Spacing="14">
                        <Label Text="Új felhasználó" FontSize="16" FontAttributes="Bold" TextColor="#1A237E" />

                        <VerticalStackLayout Spacing="4">
                            <Label Text="Teljes név" FontSize="13" TextColor="#424242" />
                            <Entry Text="{Binding NewName}" Placeholder="Teljes név" />
                        </VerticalStackLayout>
                        <VerticalStackLayout Spacing="4">
                            <Label Text="E-mail cím" FontSize="13" TextColor="#424242" />
                            <Entry Text="{Binding NewEmail}" Keyboard="Email" Placeholder="email@pelda.hu" />
                        </VerticalStackLayout>
                        <VerticalStackLayout Spacing="4">
                            <Label Text="Jelszó" FontSize="13" TextColor="#424242" />
                            <Entry Text="{Binding NewPassword}" IsPassword="True" Placeholder="Legalább 8 karakter" />
                        </VerticalStackLayout>
                        <VerticalStackLayout Spacing="4">
                            <Label Text="Szerepkör" FontSize="13" TextColor="#424242" />
                            <Picker ItemsSource="{Binding Roles}" SelectedItem="{Binding NewRole}" />
                        </VerticalStackLayout>

                        <Button Text="Felhasználó létrehozása"
                                Command="{Binding CreateUserCommand}"
                                IsEnabled="{Binding IsNotBusy}"
                                BackgroundColor="#1A237E" TextColor="White"
                                CornerRadius="8" HeightRequest="48" />
                    </VerticalStackLayout>
                </Frame>

                <!-- Reset Password -->
                <Frame CornerRadius="10" Padding="20" HasShadow="True" BackgroundColor="White"
                       IsVisible="{Binding SelectedUser, Converter={StaticResource NullToBoolConverter}}">
                    <VerticalStackLayout Spacing="14">
                        <Label Text="Jelszó visszaállítása" FontSize="16" FontAttributes="Bold" TextColor="#E65100" />
                        <Label Text="{Binding SelectedUser.Name, StringFormat='Felhasználó: {0}'}"
                               FontSize="13" TextColor="#757575" />
                        <VerticalStackLayout Spacing="4">
                            <Label Text="Új jelszó" FontSize="13" TextColor="#424242" />
                            <Entry Text="{Binding ResetPasswordValue}" IsPassword="True" Placeholder="Új jelszó" />
                        </VerticalStackLayout>
                        <Button Text="Jelszó visszaállítása"
                                Command="{Binding ResetPasswordCommand}"
                                IsEnabled="{Binding IsNotBusy}"
                                BackgroundColor="#E65100" TextColor="White"
                                CornerRadius="8" HeightRequest="44" />
                    </VerticalStackLayout>
                </Frame>

                <ActivityIndicator IsRunning="{Binding IsBusy}" IsVisible="{Binding IsBusy}"
                                   Color="#1A237E" HorizontalOptions="Center" />
            </VerticalStackLayout>
        </ScrollView>

        <!-- Right: User List -->
        <VerticalStackLayout Grid.Column="1" Spacing="12">

            <Grid ColumnDefinitions="*, Auto">
                <Label Grid.Column="0" Text="Felhasználólista"
                       FontSize="20" FontAttributes="Bold" TextColor="#1A237E" />
                <Button Grid.Column="1" Text="Frissítés"
                        Command="{Binding LoadUsersCommand}"
                        BackgroundColor="#E3F2FD" TextColor="#1565C0"
                        CornerRadius="6" HeightRequest="36" FontSize="12" />
            </Grid>

            <!-- Header -->
            <Frame Padding="14,8" CornerRadius="6" BackgroundColor="#E8EAF6">
                <Grid ColumnDefinitions="*,*,100,80">
                    <Label Grid.Column="0" Text="Név"       FontAttributes="Bold" FontSize="12" TextColor="#424242" />
                    <Label Grid.Column="1" Text="E-mail"    FontAttributes="Bold" FontSize="12" TextColor="#424242" />
                    <Label Grid.Column="2" Text="Szerepkör" FontAttributes="Bold" FontSize="12" TextColor="#424242" />
                    <Label Grid.Column="3" Text="Műveletek" FontAttributes="Bold" FontSize="12" TextColor="#424242" />
                </Grid>
            </Frame>

            <CollectionView ItemsSource="{Binding Users}"
                            SelectionMode="Single"
                            SelectedItem="{Binding SelectedUser}"
                            VerticalScrollBarVisibility="Always">
                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="model:UserResponseModel">
                        <Frame Margin="0,2" Padding="14,10" CornerRadius="6"
                               HasShadow="False" BackgroundColor="White">
                            <Grid ColumnDefinitions="*,*,100,80" ColumnSpacing="8">
                                <Label Grid.Column="0" Text="{Binding Name}" FontAttributes="Bold" FontSize="13" />
                                <Label Grid.Column="1" Text="{Binding Email}" FontSize="13" TextColor="#424242" />
                                <Label Grid.Column="2" FontSize="12"
                                       Text="{Binding Roles[0]}"
                                       TextColor="#1565C0" />
                                <Button Grid.Column="3"
                                        Text="Törlés"
                                        FontSize="11"
                                        BackgroundColor="#FFEBEE"
                                        TextColor="#C62828"
                                        CornerRadius="4"
                                        HeightRequest="30"
                                        CommandParameter="{Binding .}"
                                        Command="{Binding Source={RelativeSource AncestorType={x:Type vm:UsersViewModel}}, Path=DeleteUserCommand}" />
                            </Grid>
                        </Frame>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>

        </VerticalStackLayout>

    </Grid>

</ContentPage>
```

**Create: `/Solution.Maui/Pages/UsersPage.xaml.cs`**

```csharp
namespace Solution.Maui.Pages;

public partial class UsersPage : ContentPage
{
    private readonly UsersViewModel viewModel;

    public UsersPage(UsersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadUsersCommand.Execute(null);
    }
}
```

### 8.13 Value Converters

**Create: `/Solution.Maui/Converters/Converters.cs`**

```csharp
namespace Solution.Maui.Converters;

/// <summary>Maps true/false to green/red background colors for rate status cards.</summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Color.FromArgb("#E8F5E9") : Color.FromArgb("#FFF3E0");
        return Colors.White;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Maps true/false to "Beállítva"/"Nincs beállítva" status text.</summary>
public class BoolToStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? "✓ Beállítva" : "✗ Nincs beállítva";
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Inverts a boolean value for visibility bindings.</summary>
public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

/// <summary>Returns true when the bound value is not null.</summary>
public class NullToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Maps Buy/Sell transaction type to blue/purple badge color.</summary>
public class TypeToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
            return s == "Buy" ? Color.FromArgb("#1565C0") : Color.FromArgb("#6A1B9A");
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

### 8.14 App Shell

**Create: `/Solution.Maui/AppShell.xaml`**

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Shell xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
       xmlns:pages="clr-namespace:Solution.Maui.Pages"
       x:Class="Solution.Maui.AppShell"
       FlyoutBehavior="Flyout"
       FlyoutWidth="220"
       Shell.BackgroundColor="#1A237E"
       Shell.ForegroundColor="White"
       Shell.TitleColor="White">

    <!-- Flyout Header -->
    <Shell.FlyoutHeader>
        <VerticalStackLayout Padding="20,30,20,20" BackgroundColor="#1A237E">
            <Label Text="Pénzváltó" FontSize="20" FontAttributes="Bold" TextColor="White" />
            <Label Text="Kezelői felület" FontSize="12" TextColor="#90CAF9" />
        </VerticalStackLayout>
    </Shell.FlyoutHeader>

    <ShellContent Title="Irányítópult"
                  Icon="dashboard.png"
                  ContentTemplate="{DataTemplate pages:DashboardPage}"
                  Route="dashboard" />

    <ShellContent Title="Árfolyamok"
                  Icon="rates.png"
                  ContentTemplate="{DataTemplate pages:ExchangeRatesPage}"
                  Route="exchangerates" />

    <ShellContent Title="Tranzakciók"
                  Icon="transactions.png"
                  ContentTemplate="{DataTemplate pages:TransactionsPage}"
                  Route="transactions" />

    <ShellContent Title="Felhasználók"
                  Icon="users.png"
                  ContentTemplate="{DataTemplate pages:UsersPage}"
                  Route="users" />

</Shell>
```

**Create: `/Solution.Maui/AppShell.xaml.cs`**

```csharp
namespace Solution.Maui;

public partial class AppShell : Shell
{
    private readonly IApiClient apiClient;

    public AppShell()
    {
        InitializeComponent();
        apiClient = IPlatformApplication.Current!.Services.GetRequiredService<IApiClient>();
        HideUsersIfNotAdmin();
    }

    private void HideUsersIfNotAdmin()
    {
        foreach (var item in Items)
        {
            if (item is ShellContent content && content.Route == "users")
            {
                content.IsVisible = apiClient.IsAdmin;
                break;
            }
        }
    }
}
```

### 8.15 App Entry

**Create: `/Solution.Maui/App.xaml`**

```xml
<?xml version = "1.0" encoding = "UTF-8" ?>
<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:conv="clr-namespace:Solution.Maui.Converters"
             x:Class="Solution.Maui.App">
    <Application.Resources>
        <ResourceDictionary>
            <conv:BoolToColorConverter   x:Key="BoolToColorConverter" />
            <conv:BoolToStatusConverter  x:Key="BoolToStatusConverter" />
            <conv:InverseBoolConverter   x:Key="InverseBoolConverter" />
            <conv:NullToBoolConverter    x:Key="NullToBoolConverter" />
            <conv:TypeToColorConverter   x:Key="TypeToColorConverter" />
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

**Create: `/Solution.Maui/App.xaml.cs`**

```csharp
namespace Solution.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        // No login screen — go straight to the shell on every launch
        MainPage = new AppShell();
    }
}
```

### 8.16 Dependency Injection Entry Point

**Create: `/Solution.Maui/MauiProgram.cs`**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Solution.Maui.Constants;
using Solution.Maui.Converters;
using Solution.Maui.Pages;
using Solution.Maui.Services;
using Solution.Maui.Settings;
using Solution.Maui.ViewModels;

namespace Solution.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf",  "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // --- Configuration ---
        var assembly = typeof(MauiProgram).Assembly;
        using var stream = assembly.GetManifestResourceStream("Solution.Maui.appsettings.json");
        if (stream is not null)
        {
            var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
            builder.Configuration.AddConfiguration(config);
        }

        var apiSettings = builder.Configuration
            .GetSection("ApiSettings")
            .Get<ApiSettings>() ?? new ApiSettings();

        // Register as singleton so ApiClient and ViewModels share the same instance
        builder.Services.AddSingleton(apiSettings);

        // --- HTTP Client ---
        builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiSettings.BaseUrl);
            client.Timeout     = TimeSpan.FromSeconds(30);
        });

        // --- ViewModels ---
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ExchangeRatesViewModel>();
        builder.Services.AddTransient<TransactionsViewModel>();
        builder.Services.AddTransient<UsersViewModel>();

        // --- Pages ---
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ExchangeRatesPage>();
        builder.Services.AddTransient<TransactionsPage>();
        builder.Services.AddTransient<UsersPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

---

## Phase 9: MAUI — Run & Verify

```bash
# Build the MAUI project (Windows target)
dotnet build Solution.Maui/Solution.Maui.csproj -f net10.0-windows10.0.19041.0

# Run on Windows
dotnet run --project Solution.Maui/Solution.Maui.csproj -f net10.0-windows10.0.19041.0
```

**MAUI Test Sequence:**

1. Launch app → AppShell opens immediately (no login screen)
2. Dashboard → summary card shows today's rate status and transaction count
3. Árfolyamok → if no rates set today, the "Mai árfolyamok beállítása" form appears; fill in all 6 fields → Save
4. Tranzakciók → create a Buy transaction for USD; receipt panel slides into view
5. Tranzakciók → create a Sell transaction for GBP; filter by currency and verify list
6. Felhasználók (Admin only) → create a new Cashier user; verify in list
7. Update `appsettings.json` with the new Cashier credentials → relaunch → verify Users tab is hidden in flyout
8. Cashier creates a transaction → confirm it appears in today's list

---

## API Endpoint Summary

| Method | Endpoint                        | Description                           | Auth          |
| ------ | ------------------------------- | ------------------------------------- | ------------- |
| POST   | /api/account/login              | User login                            | Public        |
| GET    | /api/exchangerate               | List rates with optional date filter  | Authenticated |
| GET    | /api/exchangerate/today         | Get today's rates                     | Authenticated |
| GET    | /api/exchangerate/{date}        | Get rates for specific date           | Authenticated |
| GET    | /api/exchangerate/exists/{date} | Check if rates exist                  | Authenticated |
| POST   | /api/exchangerate               | Create daily rates (all 3 currencies) | Authenticated |
| PUT    | /api/exchangerate               | Update single rate for today          | Authenticated |
| POST   | /api/transaction/buy            | Create buy transaction                | Authenticated |
| POST   | /api/transaction/sell           | Create sell transaction               | Authenticated |
| GET    | /api/transaction                | List transactions with filters        | Authenticated |
| GET    | /api/transaction/{id}           | Get transaction by ID                 | Authenticated |
| GET    | /api/statistics/rates           | Rate trends for charts                | Authenticated |
| GET    | /api/statistics/transactions    | Transaction statistics                | Authenticated |
| GET    | /api/statistics/summary         | Dashboard summary                     | Authenticated |
| GET    | /api/user                       | List all users                        | Admin only    |
| GET    | /api/user/{id}                  | Get user by ID                        | Admin only    |
| POST   | /api/user                       | Create new user                       | Admin only    |
| PUT    | /api/user/{id}                  | Update user                           | Admin only    |
| DELETE | /api/user/{id}                  | Delete user (not self)                | Admin only    |
| POST   | /api/user/{id}/reset-password   | Reset user password                   | Admin only    |

---

## Business Rules Summary

1. **Exchange Rates**
   - One rate entry per currency per day
   - Rates can only be created/modified for the current date
   - All three currencies (USD, GBP, CHF) required on initial creation
   - Individual rate updates allowed after creation
   - BuyRate < SellRate (bank's perspective)

2. **Transactions**
   - Requires valid exchange rates for current date
   - Buy: Customer pays HUF, receives foreign currency (uses SellRate)
   - Sell: Customer pays foreign currency, receives HUF (uses BuyRate)
   - Rate snapshot stored with transaction
   - Customer ID required for all transactions

3. **User Management**
   - Admin-only operations
   - Users cannot delete their own account
   - Email must be unique

4. **MAUI Desktop App**
   - Credentials configured in `appsettings.json`; Basic Auth header attached to every request
   - App launches directly into the shell — no login screen
   - Users tab hidden from flyout for non-administrator accounts
   - Receipt panel displayed inline after each successful transaction
   - All API errors surface as banner messages within the page

---

## Verification Plan

### 1. Build & Migration

```bash
dotnet build
dotnet ef database update --project Solution.Database --startup-project Solution.Api
```

### 2. Run API

```bash
dotnet run --project Solution.Api
```

### 3. Test API with Scalar UI

Navigate to `https://localhost:{port}/scalar/v1`

**Test Sequence:**

1. Login with admin credentials (from appsettings.json)
2. POST /api/exchangerate - Create today's rates
3. GET /api/exchangerate/today - Verify rates were created
4. POST /api/transaction/buy - Create buy transaction
5. POST /api/transaction/sell - Create sell transaction
6. GET /api/transaction - List all transactions
7. GET /api/statistics/summary - Check dashboard data
8. GET /api/user (Admin) - List users
9. POST /api/user (Admin) - Create new user
10. DELETE /api/user/{id} (Admin) - Test self-deletion prevention

### 4. Business Rules Verification Checklist

- [ ] Exchange rates only for current date
- [ ] All 3 currencies required on initial creation
- [ ] Rate update only for today's rates
- [ ] Transaction fails if no rates for today
- [ ] Rate snapshot stored with transaction
- [ ] Correct rate applied (Buy vs Sell)
- [ ] Admin cannot delete self
- [ ] Non-admin cannot access /api/user endpoints
- [ ] MAUI: App starts directly at the shell with no login prompt
- [ ] MAUI: Users tab hidden for non-admin credentials in appsettings.json
- [ ] MAUI: Receipt displayed after transaction
- [ ] MAUI: Create rates form hidden when rates already exist today

---

## Files Summary

| Layer      | New Files                                     | Modified Files                                        |
| ---------- | --------------------------------------------- | ----------------------------------------------------- |
| Database   | 3 enums, 2 entities                           | AppDbContext.cs, GlobalImports.cs                     |
| Core       | 6 request DTOs, 8 response DTOs, 4 interfaces | GlobalImports.cs                                      |
| Validators | 6 validators                                  | GlobalImports.cs                                      |
| Common     | 3 error files                                 | -                                                     |
| Services   | 4 services                                    | GlobalImports.cs                                      |
| Api        | 4 controllers                                 | DependencyInjectionConfiguration.cs, GlobalImports.cs |
| Maui       | 22 files (settings, services, VMs, pages, converters)   | -                                                     |

**Total new files: ~52**
**Modified files: ~7**
