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
└── Solution.Tests/         # Test layer - Unit and Integration tests
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

        // Check if email is taken by another user
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null && existingUser.Id != userId)
            return Errors.User.EmailAlreadyExists;

        user.Name = request.Name;
        user.Email = request.Email;
        user.UserName = request.Email;

        await dbContext.SaveChangesAsync();

        // Update role
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

## API Endpoint Summary

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | /api/account/login | User login | Public |
| GET | /api/exchangerate | List rates with optional date filter | Authenticated |
| GET | /api/exchangerate/today | Get today's rates | Authenticated |
| GET | /api/exchangerate/{date} | Get rates for specific date | Authenticated |
| GET | /api/exchangerate/exists/{date} | Check if rates exist | Authenticated |
| POST | /api/exchangerate | Create daily rates (all 3 currencies) | Authenticated |
| PUT | /api/exchangerate | Update single rate for today | Authenticated |
| POST | /api/transaction/buy | Create buy transaction | Authenticated |
| POST | /api/transaction/sell | Create sell transaction | Authenticated |
| GET | /api/transaction | List transactions with filters | Authenticated |
| GET | /api/transaction/{id} | Get transaction by ID | Authenticated |
| GET | /api/statistics/rates | Rate trends for charts | Authenticated |
| GET | /api/statistics/transactions | Transaction statistics | Authenticated |
| GET | /api/statistics/summary | Dashboard summary | Authenticated |
| GET | /api/user | List all users | Admin only |
| GET | /api/user/{id} | Get user by ID | Admin only |
| POST | /api/user | Create new user | Admin only |
| PUT | /api/user/{id} | Update user | Admin only |
| DELETE | /api/user/{id} | Delete user (not self) | Admin only |
| POST | /api/user/{id}/reset-password | Reset user password | Admin only |

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

### 3. Test with Scalar UI
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

---

## Files Summary

| Layer | New Files | Modified Files |
|-------|-----------|----------------|
| Database | 3 enums, 2 entities | AppDbContext.cs, GlobalImports.cs |
| Core | 6 request DTOs, 8 response DTOs, 4 interfaces | GlobalImports.cs |
| Validators | 6 validators | GlobalImports.cs |
| Common | 3 error files | - |
| Services | 4 services | GlobalImports.cs |
| Api | 4 controllers | DependencyInjectionConfiguration.cs, GlobalImports.cs |

**Total new files: ~30**
**Modified files: ~7**
