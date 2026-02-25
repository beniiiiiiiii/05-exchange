# Pénzváltó (Currency Exchange) .NET MAUI Desktop App — Complete Implementation Plan

## Context

Build a .NET MAUI Desktop app for the Pénzváltó system. REST API exists at `/Users/marcellgrund/05-exchange/05 - Exchange/Exchange.Solution/Solution.WebAPI/`. The MAUI project (`Solution.DesktopApp.csproj`) is a default template. Must follow Motorcycles reference architecture (`/Users/marcellgrund/Maui13/Motorcycles/Solution.DesktopApp/`) but use HttpClient instead of direct DB access.

**Project root**: `/Users/marcellgrund/05-exchange/05 - Exchange/Exchange.Solution/Exchange.Solution/`
**RootNamespace**: `Exchange.Solution`

---

## File Tree (all relative to project root)

```
├── MauiProgram.cs ← MODIFY
├── App.xaml ← MODIFY
├── App.xaml.cs ← MODIFY
├── AppShell.xaml ← MODIFY
├── AppShell.xaml.cs ← MODIFY
├── GlobalUsings.cs ← CREATE
├── Solution.DesktopApp.csproj ← MODIFY
├── appsettings.Development.json ← CREATE
├── appsettings.Production.json ← CREATE
├── MainPage.xaml ← DELETE
├── MainPage.xaml.cs ← DELETE
├── Configurations/
│   ├── ConfigureAppVariables.cs
│   ├── ConfigureAppSettingsMapping.cs
│   ├── ConfigureDI.cs
│   ├── ConfigureFonts.cs
│   └── ConfigureHttpClient.cs
├── Services/
│   ├── ITokenStorageService.cs + TokenStorageService.cs
│   ├── AuthenticatedHttpClientHandler.cs
│   ├── IAuthService.cs + AuthService.cs
│   ├── IExchangeRateApiService.cs + ExchangeRateApiService.cs
│   ├── ITransactionApiService.cs + TransactionApiService.cs
│   ├── IStatisticsApiService.cs + StatisticsApiService.cs
│   └── IUserApiService.cs + UserApiService.cs
├── Models/
│   ├── ApiSettings.cs
│   ├── LoginModel.cs
│   ├── ExchangeRateFormModel.cs
│   ├── TransactionFormModel.cs
│   ├── UserFormModel.cs
│   └── RateChartDataPoint.cs
├── Validators/
│   ├── LoginModelValidator.cs
│   ├── ExchangeRateFormModelValidator.cs
│   ├── TransactionFormModelValidator.cs
│   └── UserFormModelValidator.cs
├── Converters/
│   ├── ValidationResultToErrorMessageConverter.cs
│   ├── ValidationResultToHasErrorConverter.cs
│   └── EnumToDisplayNameConverter.cs
├── Extensions/
│   └── NavigationStackExtensions.cs
├── ViewModels/
│   ├── AppShellViewModel.cs
│   ├── LoginViewModel.cs
│   ├── MainViewModel.cs
│   ├── ExchangeRateSetViewModel.cs
│   ├── ExchangeRateListViewModel.cs
│   ├── TransactionViewModel.cs
│   ├── StatisticsViewModel.cs
│   ├── UserListViewModel.cs
│   └── CreateOrEditUserViewModel.cs
├── Views/
│   ├── LoginView.xaml + .xaml.cs
│   ├── MainView.xaml + .xaml.cs
│   ├── ExchangeRateSetView.xaml + .xaml.cs
│   ├── ExchangeRateListView.xaml + .xaml.cs
│   ├── TransactionView.xaml + .xaml.cs
│   ├── StatisticsView.xaml + .xaml.cs
│   ├── UserListView.xaml + .xaml.cs
│   └── CreateOrEditUserView.xaml + .xaml.cs
├── Components/
│   ├── UserListComponent.xaml + .xaml.cs
│   └── ExchangeRateListComponent.xaml + .xaml.cs
└── Resources/Styles/AppStyles.xaml ← CREATE
```

---

## PHASE 1 — Foundation

### 1.1 Solution.DesktopApp.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
    <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>Exchange.Solution</RootNamespace>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ApplicationTitle>Pénzváltó</ApplicationTitle>
    <ApplicationId>com.companyname.exchange.solution</ApplicationId>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion>
    <WindowsPackageType>None</WindowsPackageType>
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</SupportedOSPlatformVersion>
    <TargetPlatformMinVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</TargetPlatformMinVersion>
  </PropertyGroup>
  <ItemGroup>
    <MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#512BD4" />
    <MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#512BD4" BaseSize="128,128" />
    <MauiImage Include="Resources\Images\*" />
    <MauiFont Include="Resources\Fonts\*" />
    <MauiAsset Include="Resources\Raw\**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="CommunityToolkit.Maui" Version="11.1.0" />
    <PackageReference Include="CommunityToolkit.Maui.Markup" Version="4.1.0" />
    <PackageReference Include="ErrorOr" Version="2.0.1" />
    <PackageReference Include="FluentValidation" Version="12.1.1" />
    <PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />
    <PackageReference Include="Microsoft.Maui.Essentials" Version="$(MauiVersion)" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.0" />
    <PackageReference Include="Syncfusion.Maui.Core" Version="*" />
    <PackageReference Include="Syncfusion.Maui.Toolkit" Version="*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Solution.Core\Solution.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <None Remove="appsettings.Development.json" />
    <EmbeddedResource Include="appsettings.Development.json" />
    <None Remove="appsettings.Production.json" />
    <EmbeddedResource Include="appsettings.Production.json" />
  </ItemGroup>
</Project>
```

### 1.2 GlobalUsings.cs

```csharp
global using CommunityToolkit.Maui;
global using CommunityToolkit.Maui.Alerts;
global using CommunityToolkit.Maui.Core;
global using CommunityToolkit.Maui.Markup;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Logging;
global using Exchange.Solution.Configurations;
global using Exchange.Solution.ViewModels;
global using Exchange.Solution.Views;
global using Exchange.Solution.Services;
global using Exchange.Solution.Models;
global using System.Runtime.InteropServices;
global using System.Collections.ObjectModel;
global using System.Windows.Input;
global using ErrorOr;
global using FluentValidation.Results;
global using System.Globalization;
global using Exchange.Solution.Validators;
global using FluentValidation;
global using Solution.Core.Models.Requests;
global using Solution.Core.Models.Requests.Security;
global using Solution.Core.Models.Responses;
global using Solution.Core.Models.Response;
global using Solution.Database.Enums;
global using System.Net.Http.Json;
global using System.Text.Json;
```

### 1.3 appsettings.Development.json

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001"
  }
}
```

### 1.4 appsettings.Production.json

```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.exchange.production.com"
  }
}
```

### 1.5 Models/ApiSettings.cs

```csharp
namespace Exchange.Solution.Models;

public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
}
```

### 1.6 Configurations/ConfigureAppVariables.cs

```csharp
namespace Exchange.Solution.Configurations;

public static class ConfigureAppVariables
{
    public static MauiAppBuilder UseAppConfigurations(this MauiAppBuilder builder)
    {
#if DEBUG
        var file = "appsettings.Development.json";
#else
        var file = "appsettings.Production.json";
#endif
        var assembly = typeof(ConfigureAppVariables).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith(file));
        using var stream = assembly.GetManifestResourceStream(resourceName!);
        var config = new ConfigurationBuilder().AddJsonStream(stream!).Build();
        builder.Configuration.AddConfiguration(config);
        return builder;
    }
}
```

### 1.7 Configurations/ConfigureAppSettingsMapping.cs

```csharp
namespace Exchange.Solution.Configurations;

public static class ConfigureAppSettingsMapping
{
    public static MauiAppBuilder UseAppSettingsMapping(this MauiAppBuilder builder)
    {
        var apiSettings = builder.Configuration.GetRequiredSection("ApiSettings").Get<ApiSettings>();
        builder.Services.AddSingleton(apiSettings!);
        return builder;
    }
}
```

### 1.8 Configurations/ConfigureFonts.cs

```csharp
namespace Exchange.Solution.Configurations;

public static class ConfigureFonts
{
    public static MauiAppBuilder UseFontConfiguration(this MauiAppBuilder builder)
    {
        builder.ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });
        return builder;
    }
}
```

### 1.9 Services/ITokenStorageService.cs

```csharp
namespace Exchange.Solution.Services;

public interface ITokenStorageService
{
    string? Token { get; }
    IList<string> Roles { get; }
    DateTime? Expiration { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    void Store(TokenResponseModel tokenResponse);
    void Clear();
}
```

### 1.10 Services/TokenStorageService.cs

```csharp
namespace Exchange.Solution.Services;

public class TokenStorageService : ITokenStorageService
{
    public string? Token { get; private set; }
    public IList<string> Roles { get; private set; } = new List<string>();
    public DateTime? Expiration { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token) && Expiration.HasValue && Expiration.Value > DateTime.UtcNow;
    public bool IsAdmin => Roles.Contains("Administrator") || Roles.Contains("Admin");

    public void Store(TokenResponseModel tokenResponse)
    {
        Token = tokenResponse.Token;
        Roles = tokenResponse.Roles;
        Expiration = tokenResponse.Expiration;
    }

    public void Clear()
    {
        Token = null;
        Roles = new List<string>();
        Expiration = null;
    }
}
```

### 1.11 Services/AuthenticatedHttpClientHandler.cs

```csharp
namespace Exchange.Solution.Services;

public class AuthenticatedHttpClientHandler(ITokenStorageService tokenStorage) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(tokenStorage.Token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenStorage.Token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            tokenStorage.Clear();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                Shell.Current.ClearNavigationStack();
                await Shell.Current.GoToAsync($"//{LoginView.Name}");
            });
        }
        return response;
    }
}
```

### 1.12 Configurations/ConfigureHttpClient.cs

```csharp
namespace Exchange.Solution.Configurations;

public static class ConfigureHttpClient
{
    public static MauiAppBuilder UseHttpClientConfiguration(this MauiAppBuilder builder)
    {
        builder.Services.AddTransient<AuthenticatedHttpClientHandler>();
        builder.Services.AddHttpClient("ExchangeApi", (sp, client) =>
        {
            var settings = sp.GetRequiredService<ApiSettings>();
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }).AddHttpMessageHandler<AuthenticatedHttpClientHandler>();
        return builder;
    }
}
```

### 1.13 Converters/ValidationResultToErrorMessageConverter.cs

```csharp
namespace Exchange.Solution.Converters;

public class ValidationResultToErrorMessageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        if (value is not ValidationResult validationResult || validationResult.IsValid) return null;
        if (parameter == null) return null;
        var property = parameter as string;
        var errorMessage = validationResult.Errors.Where(x => x.PropertyName == property).Select(x => x.ErrorMessage);
        return string.Join(Environment.NewLine, errorMessage);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
        => throw new NotImplementedException();
}
```

### 1.14 Converters/ValidationResultToHasErrorConverter.cs

```csharp
namespace Exchange.Solution.Converters;

public class ValidationResultToHasErrorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ValidationResult validationResult || parameter == null) return null;
        if (validationResult.IsValid) return false;
        var property = parameter as string;
        return validationResult.Errors.Any(x => x.PropertyName == property);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

### 1.15 Converters/EnumToDisplayNameConverter.cs

```csharp
namespace Exchange.Solution.Converters;

public class EnumToDisplayNameConverter : IValueConverter
{
    private static readonly Dictionary<string, string> DisplayNames = new()
    {
        { "PersonalIdCard", "Személyi igazolvány" }, { "Passport", "Útlevél" },
        { "DrivingLicense", "Jogosítvány" }, { "Buy", "Vétel" }, { "Sell", "Eladás" },
        { "USD", "USD" }, { "GBP", "GBP" }, { "CHF", "CHF" },
        { "User", "User" }, { "Admin", "Admin" },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return null;
        var key = value.ToString()!;
        return DisplayNames.TryGetValue(key, out var display) ? display : key;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

### 1.16 Extensions/NavigationStackExtensions.cs

```csharp
namespace Microsoft.Maui.Controls;

public static class NavigationStackExtensions
{
    public static void ClearNavigationStack(this Shell currentShell)
    {
        var stack = currentShell.Navigation.NavigationStack.ToArray();
        for (int i = stack.Length - 1; i > 0; i--)
            currentShell.Navigation.RemovePage(stack[i]);
    }
}
```

### 1.17 Resources/Styles/AppStyles.xaml

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<?xaml-comp compile="true" ?>
<ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">

    <Style x:Key="ValidationErrorLabelStyle" TargetType="Label">
        <Setter Property="TextColor" Value="Red" />
        <Setter Property="FontSize" Value="10" />
    </Style>
    <Style x:Key="FormLineContainer" TargetType="VerticalStackLayout">
        <Setter Property="Margin" Value="50,0,50,20" />
    </Style>
    <Style x:Key="PageTitle" TargetType="Label">
        <Setter Property="HorizontalOptions" Value="Start" />
        <Setter Property="TextColor" Value="#1B3A5C" />
        <Setter Property="Margin" Value="0,10,0,20" />
        <Setter Property="FontSize" Value="Title" />
        <Setter Property="FontAttributes" Value="Bold" />
    </Style>
    <Style x:Key="TableHeader" TargetType="Label">
        <Setter Property="FontSize" Value="16" />
        <Setter Property="TextColor" Value="White" />
        <Setter Property="FontAttributes" Value="Bold" />
    </Style>
    <Style x:Key="PrimaryButton" TargetType="Button">
        <Setter Property="BackgroundColor" Value="#2E5C8A" />
        <Setter Property="TextColor" Value="White" />
        <Setter Property="FontAttributes" Value="Bold" />
        <Setter Property="CornerRadius" Value="5" />
        <Setter Property="Padding" Value="20,10" />
    </Style>
    <Style x:Key="SecondaryButton" TargetType="Button">
        <Setter Property="BackgroundColor" Value="#6C757D" />
        <Setter Property="TextColor" Value="White" />
        <Setter Property="CornerRadius" Value="5" />
    </Style>
</ResourceDictionary>
```

### 1.18 Configurations/ConfigureDI.cs

```csharp
namespace Exchange.Solution.Configurations;

public static class ConfigureDI
{
    public static MauiAppBuilder UseDIConfiguration(this MauiAppBuilder builder)
    {
        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<ExchangeRateSetViewModel>();
        builder.Services.AddTransient<ExchangeRateListViewModel>();
        builder.Services.AddTransient<TransactionViewModel>();
        builder.Services.AddTransient<StatisticsViewModel>();
        builder.Services.AddTransient<UserListViewModel>();
        builder.Services.AddTransient<CreateOrEditUserViewModel>();
        // Views
        builder.Services.AddTransient<LoginView>();
        builder.Services.AddTransient<MainView>();
        builder.Services.AddTransient<ExchangeRateSetView>();
        builder.Services.AddTransient<ExchangeRateListView>();
        builder.Services.AddTransient<TransactionView>();
        builder.Services.AddTransient<StatisticsView>();
        builder.Services.AddTransient<UserListView>();
        builder.Services.AddTransient<CreateOrEditUserView>();
        // Services
        builder.Services.AddSingleton<ITokenStorageService, TokenStorageService>();
        builder.Services.AddTransient<IAuthService, AuthService>();
        builder.Services.AddTransient<IExchangeRateApiService, ExchangeRateApiService>();
        builder.Services.AddTransient<ITransactionApiService, TransactionApiService>();
        builder.Services.AddTransient<IStatisticsApiService, StatisticsApiService>();
        builder.Services.AddTransient<IUserApiService, UserApiService>();
        // Validators
        builder.Services.AddTransient<IValidator<LoginModel>, LoginModelValidator>();
        builder.Services.AddTransient<IValidator<ExchangeRateFormModel>, ExchangeRateFormModelValidator>();
        builder.Services.AddTransient<IValidator<TransactionFormModel>, TransactionFormModelValidator>();
        builder.Services.AddTransient<IValidator<UserFormModel>, UserFormModelValidator>();
        return builder;
    }
}
```

### 1.19 MauiProgram.cs

```csharp
using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;

namespace Exchange.Solution;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
               .UseMauiCommunityToolkit(options => options.SetShouldEnableSnackbarOnWindows(true))
               .ConfigureSyncfusionCore()
               .ConfigureSyncfusionToolkit()
               .UseMauiCommunityToolkitMarkup()
               .UseFontConfiguration()
               .UseAppConfigurations()
               .UseAppSettingsMapping()
               .UseHttpClientConfiguration()
               .UseDIConfiguration();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
```

### 1.20 App.xaml

```xml
<?xml version = "1.0" encoding = "UTF-8" ?>
<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:converters="clr-namespace:Exchange.Solution.Converters"
             x:Class="Exchange.Solution.App">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
                <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
                <ResourceDictionary Source="Resources/Styles/AppStyles.xaml" />
            </ResourceDictionary.MergedDictionaries>
            <converters:ValidationResultToErrorMessageConverter x:Key="ValidationResultToErrorMessageConverter"/>
            <converters:ValidationResultToHasErrorConverter x:Key="ValidationResultToHasErrorConverter"/>
            <converters:EnumToDisplayNameConverter x:Key="EnumToDisplayNameConverter"/>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### 1.21 App.xaml.cs

```csharp
namespace Exchange.Solution;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ExceptionHandler.UnhandledException += OnException;
        InitializeComponent();
        MaximizeWindow();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var tokenStorage = _serviceProvider.GetRequiredService<ITokenStorageService>();
        return new Window(new AppShell(new AppShellViewModel(tokenStorage)));
    }

    private async void OnException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        var message = exception?.Message ?? "Unexpected error!";
        var toast = Toast.Make(message, ToastDuration.Long, 16);
        await toast.Show(new CancellationTokenSource().Token);
    }

    private void MaximizeWindow()
    {
        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
#if WINDOWS
            var nativeWindow = handler.PlatformView;
            nativeWindow.Activate();
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            ShowWindow(windowHandle, 3);
#endif
        });
    }

#if WINDOWS
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int cmdShow);
#endif
}
```

---

## PHASE 2 — Authentication

### 2.1 Services/IAuthService.cs

```csharp
namespace Exchange.Solution.Services;

public interface IAuthService
{
    Task<ErrorOr<TokenResponseModel>> LoginAsync(string email, string password);
    void Logout();
}
```

### 2.2 Services/AuthService.cs

```csharp
namespace Exchange.Solution.Services;

public class AuthService(IHttpClientFactory httpClientFactory, ITokenStorageService tokenStorage) : IAuthService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ExchangeApi");

    public async Task<ErrorOr<TokenResponseModel>> LoginAsync(string email, string password)
    {
        try
        {
            var client = CreateClient();
            var request = new LoginRequestModel { Email = email, Password = password };
            var response = await client.PostAsJsonAsync("api/security/login", request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return Error.Failure(description: string.IsNullOrWhiteSpace(err) ? "Login failed." : err);
            }
            var token = await response.Content.ReadFromJsonAsync<TokenResponseModel>();
            if (token is null) return Error.Failure(description: "Invalid server response.");
            tokenStorage.Store(token);
            return token;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public void Logout() => tokenStorage.Clear();
}
```

### 2.3 Models/LoginModel.cs

```csharp
namespace Exchange.Solution.Models;

public partial class LoginModel : ObservableObject
{
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;
}
```

### 2.4 Validators/LoginModelValidator.cs

```csharp
namespace Exchange.Solution.Validators;

public class LoginModelValidator : AbstractValidator<LoginModel>
{
    public static string EmailProperty => nameof(LoginModel.Email);
    public static string PasswordProperty => nameof(LoginModel.Password);

    public LoginModelValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required!").EmailAddress().WithMessage("Invalid email!");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required!");
    }
}
```

### 2.5 ViewModels/LoginViewModel.cs

```csharp
namespace Exchange.Solution.ViewModels;

[ObservableObject]
public partial class LoginViewModel(IAuthService authService)
{
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private ValidationResult validationResult = new();
    [ObservableProperty] private bool isLoading;

    private readonly LoginModelValidator validator = new();

    public IRelayCommand ValidateCommand => new AsyncRelayCommand<string>(OnValidateAsync);
    public IAsyncRelayCommand LoginCommand => new AsyncRelayCommand(OnLoginAsync);

    private async Task OnLoginAsync()
    {
        var model = new LoginModel { Email = Email, Password = Password };
        ValidationResult = await validator.ValidateAsync(model);
        if (!ValidationResult.IsValid) return;

        IsLoading = true;
        var result = await authService.LoginAsync(Email, Password);
        IsLoading = false;

        if (result.IsError)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", result.FirstError.Description, "OK");
            return;
        }
        Shell.Current.ClearNavigationStack();
        await Shell.Current.GoToAsync($"//{MainView.Name}");
    }

    private async Task OnValidateAsync(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return;
        var model = new LoginModel { Email = Email, Password = Password };
        var result = await validator.ValidateAsync(model, o => o.IncludeProperties(propertyName));
        ValidationResult.Errors.RemoveAll(x => x.PropertyName == propertyName);
        ValidationResult.Errors.AddRange(result.Errors);
        OnPropertyChanged(nameof(ValidationResult));
    }
}
```

### 2.6 Views/LoginView.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
             xmlns:sf="clr-namespace:Syncfusion.Maui.Toolkit.TextInputLayout;assembly=Syncfusion.Maui.Toolkit"
             xmlns:viewModels="clr-namespace:Exchange.Solution.ViewModels"
             xmlns:validators="clr-namespace:Exchange.Solution.Validators"
             x:Class="Exchange.Solution.Views.LoginView"
             x:DataType="viewModels:LoginViewModel"
             x:Name="this"
             Shell.NavBarIsVisible="False">
    <Grid>
        <Image Source="background.jpg" Aspect="AspectFill" />
        <BoxView Color="#80000000" />

        <Border StrokeShape="RoundRectangle 12" BackgroundColor="#E0FFFFFF"
                WidthRequest="400" HorizontalOptions="Center" VerticalOptions="Center">
            <VerticalStackLayout Padding="30" Spacing="15">
                <Label Text="Login" FontSize="28" FontAttributes="Bold"
                       TextColor="#1B3A5C" HorizontalOptions="Center" Margin="0,0,0,10" />

                <sf:SfTextInputLayout Hint="Username" ContainerType="Outlined" OutlineCornerRadius="8"
                    ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                        ConverterParameter={x:Static validators:LoginModelValidator.EmailProperty}}"
                    HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                        ConverterParameter={x:Static validators:LoginModelValidator.EmailProperty}}">
                    <Entry Text="{Binding Email}" Keyboard="Email">
                        <Entry.Behaviors>
                            <toolkit:EventToCommandBehavior EventName="TextChanged"
                                BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                                Command="{Binding ValidateCommand}"
                                CommandParameter="{x:Static validators:LoginModelValidator.EmailProperty}" />
                        </Entry.Behaviors>
                    </Entry>
                </sf:SfTextInputLayout>

                <sf:SfTextInputLayout Hint="Password" ContainerType="Outlined" OutlineCornerRadius="8"
                    EnablePasswordVisibilityToggle="True"
                    ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                        ConverterParameter={x:Static validators:LoginModelValidator.PasswordProperty}}"
                    HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                        ConverterParameter={x:Static validators:LoginModelValidator.PasswordProperty}}">
                    <Entry Text="{Binding Password}" IsPassword="True">
                        <Entry.Behaviors>
                            <toolkit:EventToCommandBehavior EventName="TextChanged"
                                BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                                Command="{Binding ValidateCommand}"
                                CommandParameter="{x:Static validators:LoginModelValidator.PasswordProperty}" />
                        </Entry.Behaviors>
                    </Entry>
                </sf:SfTextInputLayout>

                <Button Text="Login" Command="{Binding LoginCommand}"
                        Style="{StaticResource PrimaryButton}" HorizontalOptions="FillAndExpand" />
                <ActivityIndicator IsRunning="{Binding IsLoading}" IsVisible="{Binding IsLoading}"
                                   Color="#2E5C8A" HorizontalOptions="Center" />
            </VerticalStackLayout>
        </Border>
    </Grid>
</ContentPage>
```

### 2.7 Views/LoginView.xaml.cs

```csharp
namespace Exchange.Solution.Views;

public partial class LoginView : ContentPage
{
    public static string Name => nameof(LoginView);
    public LoginView(LoginViewModel viewModel)
    {
        this.BindingContext = viewModel;
        InitializeComponent();
    }
}
```

### 2.8 ViewModels/AppShellViewModel.cs

```csharp
namespace Exchange.Solution.ViewModels;

[ObservableObject]
public partial class AppShellViewModel(ITokenStorageService tokenStorage)
{
    public IAsyncRelayCommand HomeCommand => new AsyncRelayCommand(async () =>
    { Shell.Current.ClearNavigationStack(); await Shell.Current.GoToAsync(MainView.Name); });

    public IAsyncRelayCommand RatesListCommand => new AsyncRelayCommand(async () =>
    { Shell.Current.ClearNavigationStack(); await Shell.Current.GoToAsync(ExchangeRateListView.Name); });

    public IAsyncRelayCommand SetRatesCommand => new AsyncRelayCommand(async () =>
    { Shell.Current.ClearNavigationStack(); await Shell.Current.GoToAsync(ExchangeRateSetView.Name); });

    public IAsyncRelayCommand ExchangeCommand => new AsyncRelayCommand(async () =>
    { Shell.Current.ClearNavigationStack(); await Shell.Current.GoToAsync(TransactionView.Name); });

    public IAsyncRelayCommand StatisticsCommand => new AsyncRelayCommand(async () =>
    { Shell.Current.ClearNavigationStack(); await Shell.Current.GoToAsync(StatisticsView.Name); });

    public IAsyncRelayCommand AdminUsersCommand => new AsyncRelayCommand(async () =>
    { Shell.Current.ClearNavigationStack(); await Shell.Current.GoToAsync(UserListView.Name); });

    public IAsyncRelayCommand LogoutCommand => new AsyncRelayCommand(async () =>
    { tokenStorage.Clear(); Shell.Current.ClearNavigationStack(); await Shell.Current.GoToAsync($"//{LoginView.Name}"); });

    public IAsyncRelayCommand ExitCommand => new AsyncRelayCommand(async () => Application.Current!.Quit());
}
```

### 2.9 AppShell.xaml

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Shell x:Class="Exchange.Solution.AppShell"
       xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
       xmlns:viewModels="clr-namespace:Exchange.Solution.ViewModels"
       xmlns:views="clr-namespace:Exchange.Solution.Views"
       x:DataType="viewModels:AppShellViewModel"
       Shell.FlyoutBehavior="Disabled"
       Title="Pénzváltó">

    <Shell.Resources>
        <Style x:Key="ShellContentStyle" TargetType="Element">
            <Setter Property="Shell.BackgroundColor" Value="#1B3A5C" />
            <Setter Property="Shell.TitleColor" Value="White" />
        </Style>
    </Shell.Resources>

    <Shell.MenuBarItems>
        <MenuBarItem Text="Home">
            <MenuFlyoutItem Text="Dashboard" Command="{Binding HomeCommand}" />
            <MenuFlyoutItem Text="Exit" Command="{Binding ExitCommand}" />
        </MenuBarItem>
        <MenuBarItem Text="Rates">
            <MenuFlyoutItem Text="View Rates" Command="{Binding RatesListCommand}" />
            <MenuFlyoutItem Text="Set Rates" Command="{Binding SetRatesCommand}" />
        </MenuBarItem>
        <MenuBarItem Text="Exchange">
            <MenuFlyoutItem Text="Currency Exchange" Command="{Binding ExchangeCommand}" />
            <MenuFlyoutItem Text="Statistics" Command="{Binding StatisticsCommand}" />
        </MenuBarItem>
        <MenuBarItem Text="Admin">
            <MenuFlyoutItem Text="User Management" Command="{Binding AdminUsersCommand}" />
        </MenuBarItem>
        <MenuBarItem Text="Logout">
            <MenuFlyoutItem Text="Sign Out" Command="{Binding LogoutCommand}" />
        </MenuBarItem>
    </Shell.MenuBarItems>

    <ShellContent ContentTemplate="{DataTemplate views:LoginView}" Route="LoginView" />
</Shell>
```

### 2.10 AppShell.xaml.cs

```csharp
namespace Exchange.Solution;

public partial class AppShell : Shell
{
    public AppShell(AppShellViewModel viewModel)
    {
        this.BindingContext = viewModel;
        InitializeComponent();
        Routing.RegisterRoute(LoginView.Name, typeof(LoginView));
        Routing.RegisterRoute(MainView.Name, typeof(MainView));
        Routing.RegisterRoute(ExchangeRateSetView.Name, typeof(ExchangeRateSetView));
        Routing.RegisterRoute(ExchangeRateListView.Name, typeof(ExchangeRateListView));
        Routing.RegisterRoute(TransactionView.Name, typeof(TransactionView));
        Routing.RegisterRoute(StatisticsView.Name, typeof(StatisticsView));
        Routing.RegisterRoute(UserListView.Name, typeof(UserListView));
        Routing.RegisterRoute(CreateOrEditUserView.Name, typeof(CreateOrEditUserView));
    }
}
```

## PHASE 3 — Exchange Rates

### 3.1 Services/IExchangeRateApiService.cs

```csharp
namespace Exchange.Solution.Services;

public interface IExchangeRateApiService
{
    Task<ErrorOr<bool>> RatesExistForDateAsync(DateOnly date);
    Task<ErrorOr<ExchangeRatesResponse>> GetTodayRatesAsync();
    Task<ErrorOr<ExchangeRatesResponse>> GetRatesByDateAsync(DateOnly date);
    Task<ErrorOr<List<ExchangeRatesResponse>>> GetRatesHistoryAsync(DateOnly? startDate, DateOnly? endDate);
    Task<ErrorOr<ExchangeRatesResponse>> CreateDailyRatesAsync(CreateExchangeRatesRequest request);
    Task<ErrorOr<ExchangeRateResponse>> UpdateRateAsync(UpdateExchangeRateRequest request);
}
```

### 3.2 Services/ExchangeRateApiService.cs

```csharp
namespace Exchange.Solution.Services;

public class ExchangeRateApiService(IHttpClientFactory httpClientFactory) : IExchangeRateApiService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ExchangeApi");

    public async Task<ErrorOr<bool>> RatesExistForDateAsync(DateOnly date)
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync($"api/exchangerate/exists/{date:yyyy-MM-dd}");
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return await response.Content.ReadFromJsonAsync<bool>();
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<ExchangeRatesResponse>> GetTodayRatesAsync()
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync("api/exchangerate/today");
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<ExchangeRatesResponse>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<ExchangeRatesResponse>> GetRatesByDateAsync(DateOnly date)
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync($"api/exchangerate/{date:yyyy-MM-dd}");
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<ExchangeRatesResponse>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<List<ExchangeRatesResponse>>> GetRatesHistoryAsync(DateOnly? startDate, DateOnly? endDate)
    {
        try
        {
            var client = CreateClient();
            var q = new List<string>();
            if (startDate.HasValue) q.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            if (endDate.HasValue) q.Add($"endDate={endDate.Value:yyyy-MM-dd}");
            var qs = q.Count > 0 ? "?" + string.Join("&", q) : "";
            var response = await client.GetAsync($"api/exchangerate{qs}");
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<List<ExchangeRatesResponse>>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<ExchangeRatesResponse>> CreateDailyRatesAsync(CreateExchangeRatesRequest request)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync("api/exchangerate", request);
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<ExchangeRatesResponse>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<ExchangeRateResponse>> UpdateRateAsync(UpdateExchangeRateRequest request)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PutAsJsonAsync("api/exchangerate", request);
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<ExchangeRateResponse>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }
}
```

### 3.3 Models/ExchangeRateFormModel.cs

```csharp
namespace Exchange.Solution.Models;

public partial class ExchangeRateFormModel : ObservableObject
{
    [ObservableProperty] private decimal? usdBuyRate;
    [ObservableProperty] private decimal? usdSellRate;
    [ObservableProperty] private decimal? gbpBuyRate;
    [ObservableProperty] private decimal? gbpSellRate;
    [ObservableProperty] private decimal? chfBuyRate;
    [ObservableProperty] private decimal? chfSellRate;
}
```

### 3.4 Validators/ExchangeRateFormModelValidator.cs

```csharp
namespace Exchange.Solution.Validators;

public class ExchangeRateFormModelValidator : AbstractValidator<ExchangeRateFormModel>
{
    public static string UsdBuyRateProperty => nameof(ExchangeRateFormModel.UsdBuyRate);
    public static string UsdSellRateProperty => nameof(ExchangeRateFormModel.UsdSellRate);
    public static string GbpBuyRateProperty => nameof(ExchangeRateFormModel.GbpBuyRate);
    public static string GbpSellRateProperty => nameof(ExchangeRateFormModel.GbpSellRate);
    public static string ChfBuyRateProperty => nameof(ExchangeRateFormModel.ChfBuyRate);
    public static string ChfSellRateProperty => nameof(ExchangeRateFormModel.ChfSellRate);

    public ExchangeRateFormModelValidator()
    {
        RuleFor(x => x.UsdBuyRate).NotNull().GreaterThan(0).WithMessage("USD buy rate must be > 0!");
        RuleFor(x => x.UsdSellRate).NotNull().GreaterThan(0).GreaterThan(x => x.UsdBuyRate).WithMessage("USD sell > buy!");
        RuleFor(x => x.GbpBuyRate).NotNull().GreaterThan(0).WithMessage("GBP buy rate must be > 0!");
        RuleFor(x => x.GbpSellRate).NotNull().GreaterThan(0).GreaterThan(x => x.GbpBuyRate).WithMessage("GBP sell > buy!");
        RuleFor(x => x.ChfBuyRate).NotNull().GreaterThan(0).WithMessage("CHF buy rate must be > 0!");
        RuleFor(x => x.ChfSellRate).NotNull().GreaterThan(0).GreaterThan(x => x.ChfBuyRate).WithMessage("CHF sell > buy!");
    }
}
```

### 3.5 ViewModels/ExchangeRateSetViewModel.cs

```csharp
namespace Exchange.Solution.ViewModels;

public partial class ExchangeRateSetViewModel(
    IExchangeRateApiService exchangeRateService) : ExchangeRateFormModel, IQueryAttributable
{
    [ObservableProperty] private ValidationResult validationResult = new();
    [ObservableProperty] private string title = "Set Exchange Rates";
    [ObservableProperty] private string dateDisplay = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");

    public IAsyncRelayCommand AppearingCommand => new AsyncRelayCommand(OnAppearingAsync);
    public IAsyncRelayCommand DisappearingCommand => new AsyncRelayCommand(OnDisappearingAsync);
    public IAsyncRelayCommand SubmitCommand => new AsyncRelayCommand(OnSubmitAsync);
    public IRelayCommand ValidateCommand => new AsyncRelayCommand<string>(OnValidateAsync);

    private readonly ExchangeRateFormModelValidator validator = new();
    private delegate Task ButtonActionDelegate();
    private ButtonActionDelegate asyncButtonAction;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Rates", out object? result) && result is ExchangeRatesResponse rates)
        {
            foreach (var r in rates.Rates)
            {
                switch (r.Currency)
                {
                    case "USD": UsdBuyRate = r.BuyRate; UsdSellRate = r.SellRate; break;
                    case "GBP": GbpBuyRate = r.BuyRate; GbpSellRate = r.SellRate; break;
                    case "CHF": ChfBuyRate = r.BuyRate; ChfSellRate = r.SellRate; break;
                }
            }
            DateDisplay = rates.Date.ToString("yyyy-MM-dd");
            asyncButtonAction = OnUpdateAsync;
            Title = "Update Exchange Rates";
        }
        else
        {
            asyncButtonAction = OnSaveAsync;
            Title = "Set Exchange Rates";
        }
    }

    private async Task OnAppearingAsync() { }
    private async Task OnDisappearingAsync() { }
    private async Task OnSubmitAsync() => await asyncButtonAction();

    private async Task OnSaveAsync()
    {
        ValidationResult = await validator.ValidateAsync(this);
        if (!ValidationResult.IsValid) return;
        var req = new CreateExchangeRatesRequest
        {
            Date = DateOnly.FromDateTime(DateTime.Now),
            UsdBuyRate = UsdBuyRate!.Value, UsdSellRate = UsdSellRate!.Value,
            GbpBuyRate = GbpBuyRate!.Value, GbpSellRate = GbpSellRate!.Value,
            ChfBuyRate = ChfBuyRate!.Value, ChfSellRate = ChfSellRate!.Value
        };
        var result = await exchangeRateService.CreateDailyRatesAsync(req);
        var msg = result.IsError ? result.FirstError.Description : "Rates saved.";
        await Application.Current!.MainPage!.DisplayAlert(result.IsError ? "Error" : "Info", msg, "OK");
        if (!result.IsError) { Shell.Current.ClearNavigationStack(); await Shell.Current.GoToAsync(MainView.Name); }
    }

    private async Task OnUpdateAsync()
    {
        ValidationResult = await validator.ValidateAsync(this);
        if (!ValidationResult.IsValid) return;
        var currencies = new[] {
            (Currency.USD, UsdBuyRate!.Value, UsdSellRate!.Value),
            (Currency.GBP, GbpBuyRate!.Value, GbpSellRate!.Value),
            (Currency.CHF, ChfBuyRate!.Value, ChfSellRate!.Value) };
        foreach (var (c, b, s) in currencies)
        {
            var r = await exchangeRateService.UpdateRateAsync(new UpdateExchangeRateRequest { Currency = c, BuyRate = b, SellRate = s });
            if (r.IsError) { await Application.Current!.MainPage!.DisplayAlert("Error", r.FirstError.Description, "OK"); return; }
        }
        await Application.Current!.MainPage!.DisplayAlert("Info", "Rates updated.", "OK");
    }

    private async Task OnValidateAsync(string? prop)
    {
        if (string.IsNullOrEmpty(prop)) return;
        var r = await validator.ValidateAsync(this, o => o.IncludeProperties(prop));
        ValidationResult.Errors.RemoveAll(x => x.PropertyName == prop);
        ValidationResult.Errors.AddRange(r.Errors);
        OnPropertyChanged(nameof(ValidationResult));
    }
}
```

### 3.6 Views/ExchangeRateSetView.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
             xmlns:sf="clr-namespace:Syncfusion.Maui.Toolkit.TextInputLayout;assembly=Syncfusion.Maui.Toolkit"
             xmlns:editors="clr-namespace:Syncfusion.Maui.Toolkit.NumericEntry;assembly=Syncfusion.Maui.Toolkit"
             xmlns:viewModels="clr-namespace:Exchange.Solution.ViewModels"
             xmlns:validators="clr-namespace:Exchange.Solution.Validators"
             x:Class="Exchange.Solution.Views.ExchangeRateSetView"
             x:DataType="viewModels:ExchangeRateSetViewModel"
             x:Name="this">
    <ContentPage.Behaviors>
        <toolkit:EventToCommandBehavior Command="{Binding AppearingCommand}" EventName="Appearing" />
        <toolkit:EventToCommandBehavior Command="{Binding DisappearingCommand}" EventName="Disappearing" />
    </ContentPage.Behaviors>

    <ScrollView>
        <VerticalStackLayout Padding="40" Spacing="10">
            <Label Text="{Binding Title}" Style="{StaticResource PageTitle}" />

            <HorizontalStackLayout Spacing="10">
                <Label Text="Date:" FontSize="16" VerticalOptions="Center" />
                <Label Text="{Binding DateDisplay}" FontSize="16" FontAttributes="Bold" VerticalOptions="Center" />
            </HorizontalStackLayout>

            <!-- USD -->
            <Label Text="USD to HUF:" FontSize="16" FontAttributes="Bold" Margin="0,15,0,0" />
            <Grid ColumnDefinitions="*,*" ColumnSpacing="15">
                <sf:SfTextInputLayout Grid.Column="0" Hint="Buy Rate" ContainerType="Outlined"
                    ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                        ConverterParameter={x:Static validators:ExchangeRateFormModelValidator.UsdBuyRateProperty}}"
                    HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                        ConverterParameter={x:Static validators:ExchangeRateFormModelValidator.UsdBuyRateProperty}}">
                    <editors:SfNumericEntry Value="{Binding UsdBuyRate}" CustomFormat="N4" ShowClearButton="True">
                        <editors:SfNumericEntry.Behaviors>
                            <toolkit:EventToCommandBehavior EventName="ValueChanged"
                                BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                                Command="{Binding ValidateCommand}"
                                CommandParameter="{x:Static validators:ExchangeRateFormModelValidator.UsdBuyRateProperty}" />
                        </editors:SfNumericEntry.Behaviors>
                    </editors:SfNumericEntry>
                </sf:SfTextInputLayout>
                <sf:SfTextInputLayout Grid.Column="1" Hint="Sell Rate" ContainerType="Outlined"
                    ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                        ConverterParameter={x:Static validators:ExchangeRateFormModelValidator.UsdSellRateProperty}}"
                    HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                        ConverterParameter={x:Static validators:ExchangeRateFormModelValidator.UsdSellRateProperty}}">
                    <editors:SfNumericEntry Value="{Binding UsdSellRate}" CustomFormat="N4" ShowClearButton="True">
                        <editors:SfNumericEntry.Behaviors>
                            <toolkit:EventToCommandBehavior EventName="ValueChanged"
                                BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                                Command="{Binding ValidateCommand}"
                                CommandParameter="{x:Static validators:ExchangeRateFormModelValidator.UsdSellRateProperty}" />
                        </editors:SfNumericEntry.Behaviors>
                    </editors:SfNumericEntry>
                </sf:SfTextInputLayout>
            </Grid>

            <!-- GBP -->
            <Label Text="GBP to HUF:" FontSize="16" FontAttributes="Bold" Margin="0,15,0,0" />
            <Grid ColumnDefinitions="*,*" ColumnSpacing="15">
                <sf:SfTextInputLayout Grid.Column="0" Hint="Buy Rate" ContainerType="Outlined"
                    ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                        ConverterParameter={x:Static validators:ExchangeRateFormModelValidator.GbpBuyRateProperty}}"
                    HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                        ConverterParameter={x:Static validators:ExchangeRateFormModelValidator.GbpBuyRateProperty}}">
                    <editors:SfNumericEntry Value="{Binding GbpBuyRate}" CustomFormat="N4" ShowClearButton="True">
                        <editors:SfNumericEntry.Behaviors>
                            <toolkit:EventToCommandBehavior EventName="ValueChanged"
                                BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                                Command="{Binding ValidateCommand}"
                                CommandParameter="{x:Static validators:ExchangeRateFormModelValidator.GbpBuyRateProperty}" />
                        </editors:SfNumericEntry.Behaviors>
                    </editors:SfNumericEntry>
                </sf:SfTextInputLayout>
                <sf:SfTextInputLayout Grid.Column="1" Hint="Sell Rate" ContainerType="Outlined"
                    ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                        ConverterParameter={x:Static validators:ExchangeRateFormModelValidator.GbpSellRateProperty}}"
                    HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                        ConverterParameter={x:Static validators:ExchangeRateFormModelValidator.GbpSellRateProperty}}">
                    <editors:SfNumericEntry Value="{Binding GbpSellRate}" CustomFormat="N4" ShowClearButton="True">
                        <editors:SfNumericEntry.Behaviors>
                            <toolkit:EventToCommandBehavior EventName="ValueChanged"
                                BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                                Command="{Binding ValidateCommand}"
                                CommandParameter="{x:Static validators:ExchangeRateFormModelValidator.GbpSellRateProperty}" />
                        </editors:SfNumericEntry.Behaviors>
                    </editors:SfNumericEntry>
                </sf:SfTextInputLayout>
            </Grid>

            <!-- CHF -->
            <Label Text="CHF to HUF:" FontSize="16" FontAttributes="Bold" Margin="0,15,0,0" />
            <Grid ColumnDefinitions="*,*" ColumnSpacing="15">
                <sf:SfTextInputLayout Grid.Column="0" Hint="Buy Rate" ContainerType="Outlined"
                    ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                        ConverterParameter={x:Static validators:ExchangeRateFormModelValidator.ChfBuyRateProperty}}"
                    HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                        ConverterParameter={x:Static validators:ExchangeRateFormModelValidator.ChfBuyRateProperty}}">
                    <editors:SfNumericEntry Value="{Binding ChfBuyRate}" CustomFormat="N4" ShowClearButton="True">
                        <editors:SfNumericEntry.Behaviors>
                            <toolkit:EventToCommandBehavior EventName="ValueChanged"
                                BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                                Command="{Binding ValidateCommand}"
                                CommandParameter="{x:Static validators:ExchangeRateFormModelValidator.ChfBuyRateProperty}" />
                        </editors:SfNumericEntry.Behaviors>
                    </editors:SfNumericEntry>
                </sf:SfTextInputLayout>
                <sf:SfTextInputLayout Grid.Column="1" Hint="Sell Rate" ContainerType="Outlined"
                    ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                        ConverterParameter={x:Static validators:ExchangeRateFormModelValidator.ChfSellRateProperty}}"
                    HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                        ConverterParameter={x:Static validators:ExchangeRateFormModelValidator.ChfSellRateProperty}}">
                    <editors:SfNumericEntry Value="{Binding ChfSellRate}" CustomFormat="N4" ShowClearButton="True">
                        <editors:SfNumericEntry.Behaviors>
                            <toolkit:EventToCommandBehavior EventName="ValueChanged"
                                BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                                Command="{Binding ValidateCommand}"
                                CommandParameter="{x:Static validators:ExchangeRateFormModelValidator.ChfSellRateProperty}" />
                        </editors:SfNumericEntry.Behaviors>
                    </editors:SfNumericEntry>
                </sf:SfTextInputLayout>
            </Grid>

            <Button Text="Save Rates" Command="{Binding SubmitCommand}"
                    Style="{StaticResource PrimaryButton}" HorizontalOptions="Center"
                    WidthRequest="250" Margin="0,20,0,0" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

### 3.7 Views/ExchangeRateSetView.xaml.cs

```csharp
namespace Exchange.Solution.Views;

public partial class ExchangeRateSetView : ContentPage
{
    public static string Name => nameof(ExchangeRateSetView);
    public ExchangeRateSetView(ExchangeRateSetViewModel viewModel)
    {
        this.BindingContext = viewModel;
        InitializeComponent();
    }
}
```

### 3.8 ViewModels/ExchangeRateListViewModel.cs

```csharp
namespace Exchange.Solution.ViewModels;

[ObservableObject]
public partial class ExchangeRateListViewModel(IExchangeRateApiService exchangeRateService)
{
    public IAsyncRelayCommand AppearingCommand => new AsyncRelayCommand(OnAppearingAsync);
    public IAsyncRelayCommand DisappearingCommand => new AsyncRelayCommand(OnDisappearingAsync);
    public ICommand PreviousDateCommand { get; private set; }
    public ICommand NextDateCommand { get; private set; }
    public IAsyncRelayCommand EditCommand => new AsyncRelayCommand(OnEditAsync);

    [ObservableProperty] private ObservableCollection<ExchangeRateResponse> rates = new();
    [ObservableProperty] private string dateDisplay = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");

    private DateOnly currentDate = DateOnly.FromDateTime(DateTime.Now);
    private ExchangeRatesResponse? currentRatesResponse;

    private async Task OnAppearingAsync()
    {
        PreviousDateCommand = new Command(async () => { currentDate = currentDate.AddDays(-1); await LoadAsync(); });
        NextDateCommand = new Command(async () => { currentDate = currentDate.AddDays(1); await LoadAsync(); });
        await LoadAsync();
    }
    private async Task OnDisappearingAsync() { }

    private async Task LoadAsync()
    {
        var result = await exchangeRateService.GetRatesByDateAsync(currentDate);
        if (result.IsError) { Rates = new(); return; }
        currentRatesResponse = result.Value;
        Rates = new ObservableCollection<ExchangeRateResponse>(result.Value.Rates);
        DateDisplay = currentDate.ToString("yyyy-MM-dd");
    }

    private async Task OnEditAsync()
    {
        if (currentRatesResponse is null) return;
        var nav = new ShellNavigationQueryParameters { { "Rates", currentRatesResponse } };
        Shell.Current.ClearNavigationStack();
        await Shell.Current.GoToAsync(ExchangeRateSetView.Name, nav);
    }
}
```

### 3.9 Views/ExchangeRateListView.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
             xmlns:viewModels="clr-namespace:Exchange.Solution.ViewModels"
             xmlns:models="clr-namespace:Solution.Core.Models.Responses;assembly=Solution.Core"
             x:Class="Exchange.Solution.Views.ExchangeRateListView"
             x:DataType="viewModels:ExchangeRateListViewModel"
             x:Name="this">
    <ContentPage.Behaviors>
        <toolkit:EventToCommandBehavior Command="{Binding AppearingCommand}" EventName="Appearing"
            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}" />
        <toolkit:EventToCommandBehavior Command="{Binding DisappearingCommand}" EventName="Disappearing"
            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}" />
    </ContentPage.Behaviors>

    <Grid RowDefinitions="Auto,Auto,*" Padding="40">
        <Label Grid.Row="0" Text="Exchange Rates" Style="{StaticResource PageTitle}" />

        <HorizontalStackLayout Grid.Row="1" HorizontalOptions="Center" Spacing="15" Margin="0,0,0,20">
            <Button Text="&lt; Previous" Command="{Binding PreviousDateCommand}" Style="{StaticResource SecondaryButton}" />
            <Label Text="{Binding DateDisplay}" FontSize="18" FontAttributes="Bold" VerticalOptions="Center" />
            <Button Text="Next &gt;" Command="{Binding NextDateCommand}" Style="{StaticResource SecondaryButton}" />
        </HorizontalStackLayout>

        <VerticalStackLayout Grid.Row="2">
            <!-- Table Header -->
            <Grid BackgroundColor="#1B3A5C" Padding="10,8" ColumnDefinitions="2*,2*,*">
                <Label Grid.Column="0" Text="Currency" Style="{StaticResource TableHeader}" />
                <Label Grid.Column="1" Text="Rate" Style="{StaticResource TableHeader}" HorizontalTextAlignment="End" />
                <Label Grid.Column="2" Text="Action" Style="{StaticResource TableHeader}" HorizontalTextAlignment="Center" />
            </Grid>

            <CollectionView ItemsSource="{Binding Rates}">
                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="models:ExchangeRateResponse">
                        <Border Padding="10,8" StrokeThickness="0.5" Stroke="LightGray">
                            <Grid ColumnDefinitions="2*,2*,*">
                                <Label Grid.Column="0" VerticalTextAlignment="Center">
                                    <Label.Text>
                                        <MultiBinding StringFormat="{}{0} to HUF">
                                            <Binding Path="Currency" />
                                        </MultiBinding>
                                    </Label.Text>
                                </Label>
                                <Label Grid.Column="1" Text="{Binding BuyRate, StringFormat='{0:N2}'}"
                                       HorizontalTextAlignment="End" VerticalTextAlignment="Center" />
                                <Button Grid.Column="2" Text="Edit"
                                        Command="{Binding Source={RelativeSource AncestorType={x:Type viewModels:ExchangeRateListViewModel}}, Path=EditCommand}"
                                        Style="{StaticResource SecondaryButton}" HorizontalOptions="Center" />
                            </Grid>
                        </Border>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </VerticalStackLayout>
    </Grid>
</ContentPage>
```

### 3.10 Views/ExchangeRateListView.xaml.cs

```csharp
namespace Exchange.Solution.Views;

public partial class ExchangeRateListView : ContentPage
{
    public static string Name => nameof(ExchangeRateListView);
    public ExchangeRateListView(ExchangeRateListViewModel viewModel)
    {
        this.BindingContext = viewModel;
        InitializeComponent();
    }
}
```

### 3.11 ViewModels/MainViewModel.cs

```csharp
namespace Exchange.Solution.ViewModels;

[ObservableObject]
public partial class MainViewModel(IExchangeRateApiService exchangeRateService, IStatisticsApiService statisticsService)
{
    public IAsyncRelayCommand AppearingCommand => new AsyncRelayCommand(OnAppearingAsync);
    public IAsyncRelayCommand DisappearingCommand => new AsyncRelayCommand(OnDisappearingAsync);

    [ObservableProperty] private SummaryStatisticsResponse? summary;
    [ObservableProperty] private bool todayRatesExist;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private ExchangeRatesResponse? todayRates;

    private async Task OnAppearingAsync()
    {
        IsLoading = true;
        var exists = await exchangeRateService.RatesExistForDateAsync(DateOnly.FromDateTime(DateTime.Now));
        if (exists.IsError || !exists.Value)
        {
            IsLoading = false;
            Shell.Current.ClearNavigationStack();
            await Shell.Current.GoToAsync(ExchangeRateSetView.Name);
            return;
        }
        TodayRatesExist = true;
        var rates = await exchangeRateService.GetTodayRatesAsync();
        if (!rates.IsError) TodayRates = rates.Value;
        var sum = await statisticsService.GetSummaryAsync();
        if (!sum.IsError) Summary = sum.Value;
        IsLoading = false;
    }
    private async Task OnDisappearingAsync() { }
}
```

### 3.12 Views/MainView.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
             xmlns:viewModels="clr-namespace:Exchange.Solution.ViewModels"
             xmlns:models="clr-namespace:Solution.Core.Models.Responses;assembly=Solution.Core"
             x:Class="Exchange.Solution.Views.MainView"
             x:DataType="viewModels:MainViewModel"
             x:Name="this">
    <ContentPage.Behaviors>
        <toolkit:EventToCommandBehavior Command="{Binding AppearingCommand}" EventName="Appearing"
            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}" />
    </ContentPage.Behaviors>

    <ScrollView>
        <VerticalStackLayout Padding="40" Spacing="15">
            <Label Text="Dashboard" Style="{StaticResource PageTitle}" />
            <ActivityIndicator IsRunning="{Binding IsLoading}" IsVisible="{Binding IsLoading}" Color="#2E5C8A" />

            <Label Text="Today's Exchange Rates" FontSize="20" FontAttributes="Bold"
                   IsVisible="{Binding TodayRatesExist}" />

            <CollectionView ItemsSource="{Binding TodayRates.Rates}" IsVisible="{Binding TodayRatesExist}">
                <CollectionView.Header>
                    <Grid BackgroundColor="#1B3A5C" Padding="10,8" ColumnDefinitions="*,*,*">
                        <Label Grid.Column="0" Text="Currency" Style="{StaticResource TableHeader}" />
                        <Label Grid.Column="1" Text="Buy Rate" Style="{StaticResource TableHeader}" HorizontalTextAlignment="End" />
                        <Label Grid.Column="2" Text="Sell Rate" Style="{StaticResource TableHeader}" HorizontalTextAlignment="End" />
                    </Grid>
                </CollectionView.Header>
                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="models:ExchangeRateResponse">
                        <Border Padding="10,8" StrokeThickness="0.5" Stroke="LightGray">
                            <Grid ColumnDefinitions="*,*,*">
                                <Label Grid.Column="0" Text="{Binding Currency}" VerticalTextAlignment="Center" />
                                <Label Grid.Column="1" Text="{Binding BuyRate, StringFormat='{0:N4}'}" HorizontalTextAlignment="End" />
                                <Label Grid.Column="2" Text="{Binding SellRate, StringFormat='{0:N4}'}" HorizontalTextAlignment="End" />
                            </Grid>
                        </Border>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>

            <Label Text="{Binding Summary.TotalTransactionsToday, StringFormat='Transactions today: {0}'}"
                   FontSize="16" IsVisible="{Binding TodayRatesExist}" />
            <Label Text="{Binding Summary.TotalHufVolumeToday, StringFormat='Total HUF volume: {0:N0}'}"
                   FontSize="16" IsVisible="{Binding TodayRatesExist}" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

### 3.13 Views/MainView.xaml.cs

```csharp
namespace Exchange.Solution.Views;

public partial class MainView : ContentPage
{
    public static string Name => nameof(MainView);
    public MainView(MainViewModel viewModel)
    {
        this.BindingContext = viewModel;
        InitializeComponent();
    }
}
```

## PHASE 4 — Transactions

### 4.1 Services/ITransactionApiService.cs

```csharp
namespace Exchange.Solution.Services;

public interface ITransactionApiService
{
    Task<ErrorOr<TransactionResponse>> CreateBuyTransactionAsync(CreateTransactionRequest request);
    Task<ErrorOr<TransactionResponse>> CreateSellTransactionAsync(CreateTransactionRequest request);
    Task<ErrorOr<TransactionListResponse>> GetTransactionsAsync(DateOnly? date, Currency? currency, TransactionType? type);
    Task<ErrorOr<TransactionResponse>> GetTransactionByIdAsync(int id);
}
```

### 4.2 Services/TransactionApiService.cs

```csharp
namespace Exchange.Solution.Services;

public class TransactionApiService(IHttpClientFactory httpClientFactory) : ITransactionApiService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ExchangeApi");

    public async Task<ErrorOr<TransactionResponse>> CreateBuyTransactionAsync(CreateTransactionRequest request)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync("api/transaction/buy", request);
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<TransactionResponse>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<TransactionResponse>> CreateSellTransactionAsync(CreateTransactionRequest request)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync("api/transaction/sell", request);
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<TransactionResponse>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<TransactionListResponse>> GetTransactionsAsync(DateOnly? date, Currency? currency, TransactionType? type)
    {
        try
        {
            var client = CreateClient();
            var q = new List<string>();
            if (date.HasValue) q.Add($"date={date.Value:yyyy-MM-dd}");
            if (currency.HasValue) q.Add($"currency={currency.Value}");
            if (type.HasValue) q.Add($"type={type.Value}");
            var qs = q.Count > 0 ? "?" + string.Join("&", q) : "";
            var response = await client.GetAsync($"api/transaction{qs}");
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<TransactionListResponse>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<TransactionResponse>> GetTransactionByIdAsync(int id)
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync($"api/transaction/{id}");
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<TransactionResponse>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }
}
```

### 4.3 Models/TransactionFormModel.cs

```csharp
namespace Exchange.Solution.Models;

public partial class TransactionFormModel : ObservableObject
{
    [ObservableProperty] private TransactionType transactionType = TransactionType.Buy;
    [ObservableProperty] private Currency currency = Currency.USD;
    [ObservableProperty] private decimal? foreignAmount;
    [ObservableProperty] private string customerName = string.Empty;
    [ObservableProperty] private CustomerIdType customerIdType = CustomerIdType.PersonalIdCard;
    [ObservableProperty] private string customerIdNumber = string.Empty;
    [ObservableProperty] private decimal appliedRate;
    [ObservableProperty] private decimal hufAmount;
}
```

### 4.4 Validators/TransactionFormModelValidator.cs

```csharp
namespace Exchange.Solution.Validators;

public class TransactionFormModelValidator : AbstractValidator<TransactionFormModel>
{
    public static string CurrencyProperty => nameof(TransactionFormModel.Currency);
    public static string ForeignAmountProperty => nameof(TransactionFormModel.ForeignAmount);
    public static string CustomerNameProperty => nameof(TransactionFormModel.CustomerName);
    public static string CustomerIdTypeProperty => nameof(TransactionFormModel.CustomerIdType);
    public static string CustomerIdNumberProperty => nameof(TransactionFormModel.CustomerIdNumber);
    public static string TransactionTypeProperty => nameof(TransactionFormModel.TransactionType);

    public TransactionFormModelValidator()
    {
        RuleFor(x => x.Currency).IsInEnum().Must(c => c != Currency.HUF).WithMessage("Must be USD, GBP, or CHF!");
        RuleFor(x => x.ForeignAmount).NotNull().GreaterThan(0).WithMessage("Amount must be > 0!");
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(100).WithMessage("Name required (max 100)!");
        RuleFor(x => x.CustomerIdType).IsInEnum().WithMessage("ID type required!");
        RuleFor(x => x.CustomerIdNumber).NotEmpty().MaximumLength(50).WithMessage("ID number required (max 50)!");
        RuleFor(x => x.TransactionType).IsInEnum().WithMessage("Transaction type required!");
    }
}
```

### 4.5 ViewModels/TransactionViewModel.cs

```csharp
namespace Exchange.Solution.ViewModels;

public partial class TransactionViewModel(
    ITransactionApiService transactionService,
    IExchangeRateApiService exchangeRateService) : TransactionFormModel
{
    [ObservableProperty] private IList<Currency> currencies = [Currency.USD, Currency.GBP, Currency.CHF];
    [ObservableProperty] private IList<TransactionType> transactionTypes = [TransactionType.Buy, TransactionType.Sell];
    [ObservableProperty] private IList<CustomerIdType> customerIdTypes =
        [CustomerIdType.PersonalIdCard, CustomerIdType.Passport, CustomerIdType.DrivingLicense];
    [ObservableProperty] private ValidationResult validationResult = new();
    [ObservableProperty] private bool isLoading;

    private ExchangeRatesResponse? todayRates;
    private readonly TransactionFormModelValidator validator = new();

    public IAsyncRelayCommand AppearingCommand => new AsyncRelayCommand(OnAppearingAsync);
    public IAsyncRelayCommand DisappearingCommand => new AsyncRelayCommand(OnDisappearingAsync);
    public IAsyncRelayCommand SubmitCommand => new AsyncRelayCommand(OnSubmitAsync);
    public IRelayCommand ValidateCommand => new AsyncRelayCommand<string>(OnValidateAsync);
    public IRelayCommand RecalculateCommand => new RelayCommand(Recalculate);

    private async Task OnAppearingAsync()
    {
        var result = await exchangeRateService.GetTodayRatesAsync();
        if (!result.IsError) { todayRates = result.Value; Recalculate(); }
    }
    private async Task OnDisappearingAsync() { }

    private void Recalculate()
    {
        if (todayRates?.Rates is null) return;
        var rate = todayRates.Rates.FirstOrDefault(r => r.Currency == Currency.ToString());
        if (rate is null) return;
        // Buy = customer buys foreign → bank sells → use SellRate
        // Sell = customer sells foreign → bank buys → use BuyRate
        AppliedRate = TransactionType == TransactionType.Buy ? rate.SellRate : rate.BuyRate;
        HufAmount = ForeignAmount.HasValue && ForeignAmount.Value > 0
            ? Math.Round(ForeignAmount.Value * AppliedRate, 2) : 0;
    }

    private async Task OnSubmitAsync()
    {
        ValidationResult = await validator.ValidateAsync(this);
        if (!ValidationResult.IsValid) return;
        IsLoading = true;
        var req = new CreateTransactionRequest
        {
            Currency = Currency, ForeignAmount = ForeignAmount!.Value,
            CustomerName = CustomerName, CustomerIdType = CustomerIdType, CustomerIdNumber = CustomerIdNumber
        };
        var result = TransactionType == TransactionType.Buy
            ? await transactionService.CreateBuyTransactionAsync(req)
            : await transactionService.CreateSellTransactionAsync(req);
        IsLoading = false;
        var msg = result.IsError ? result.FirstError.Description : "Transaction completed.";
        await Application.Current!.MainPage!.DisplayAlert(result.IsError ? "Error" : "Info", msg, "OK");
        if (!result.IsError) ClearForm();
    }

    private void ClearForm()
    {
        ForeignAmount = null; CustomerName = string.Empty; CustomerIdNumber = string.Empty;
        HufAmount = 0; AppliedRate = 0; ValidationResult = new();
    }

    private async Task OnValidateAsync(string? prop)
    {
        if (string.IsNullOrEmpty(prop)) return;
        var r = await validator.ValidateAsync(this, o => o.IncludeProperties(prop));
        ValidationResult.Errors.RemoveAll(x => x.PropertyName == prop);
        ValidationResult.Errors.AddRange(r.Errors);
        OnPropertyChanged(nameof(ValidationResult));
    }
}
```

### 4.6 Views/TransactionView.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
             xmlns:sf="clr-namespace:Syncfusion.Maui.Toolkit.TextInputLayout;assembly=Syncfusion.Maui.Toolkit"
             xmlns:editors="clr-namespace:Syncfusion.Maui.Toolkit.NumericEntry;assembly=Syncfusion.Maui.Toolkit"
             xmlns:viewModels="clr-namespace:Exchange.Solution.ViewModels"
             xmlns:validators="clr-namespace:Exchange.Solution.Validators"
             xmlns:enums="clr-namespace:Solution.Database.Enums;assembly=Solution.Database"
             x:Class="Exchange.Solution.Views.TransactionView"
             x:DataType="viewModels:TransactionViewModel"
             x:Name="this">
    <ContentPage.Behaviors>
        <toolkit:EventToCommandBehavior Command="{Binding AppearingCommand}" EventName="Appearing"
            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}" />
        <toolkit:EventToCommandBehavior Command="{Binding DisappearingCommand}" EventName="Disappearing"
            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}" />
    </ContentPage.Behaviors>

    <ScrollView>
        <VerticalStackLayout Padding="40" Spacing="10" MaximumWidthRequest="500">
            <Label Text="Currency Exchange" Style="{StaticResource PageTitle}" />

            <!-- Transaction Type -->
            <sf:SfTextInputLayout Hint="Transaction Type" ContainerType="Outlined"
                ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                    ConverterParameter={x:Static validators:TransactionFormModelValidator.TransactionTypeProperty}}"
                HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                    ConverterParameter={x:Static validators:TransactionFormModelValidator.TransactionTypeProperty}}">
                <Picker ItemsSource="{Binding TransactionTypes}" SelectedItem="{Binding TransactionType}">
                    <Picker.Behaviors>
                        <toolkit:EventToCommandBehavior EventName="SelectedIndexChanged"
                            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                            Command="{Binding RecalculateCommand}" />
                    </Picker.Behaviors>
                </Picker>
            </sf:SfTextInputLayout>

            <!-- From Currency -->
            <sf:SfTextInputLayout Hint="From Currency" ContainerType="Outlined"
                ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                    ConverterParameter={x:Static validators:TransactionFormModelValidator.CurrencyProperty}}"
                HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                    ConverterParameter={x:Static validators:TransactionFormModelValidator.CurrencyProperty}}">
                <Picker ItemsSource="{Binding Currencies}" SelectedItem="{Binding Currency}">
                    <Picker.Behaviors>
                        <toolkit:EventToCommandBehavior EventName="SelectedIndexChanged"
                            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                            Command="{Binding RecalculateCommand}" />
                    </Picker.Behaviors>
                </Picker>
            </sf:SfTextInputLayout>

            <!-- To Currency (always HUF) -->
            <sf:SfTextInputLayout Hint="To Currency" ContainerType="Outlined">
                <Entry Text="HUF" IsReadOnly="True" />
            </sf:SfTextInputLayout>

            <!-- Amount -->
            <sf:SfTextInputLayout Hint="Amount" ContainerType="Outlined"
                ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                    ConverterParameter={x:Static validators:TransactionFormModelValidator.ForeignAmountProperty}}"
                HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                    ConverterParameter={x:Static validators:TransactionFormModelValidator.ForeignAmountProperty}}">
                <editors:SfNumericEntry Value="{Binding ForeignAmount}" CustomFormat="N2" ShowClearButton="True">
                    <editors:SfNumericEntry.Behaviors>
                        <toolkit:EventToCommandBehavior EventName="ValueChanged"
                            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                            Command="{Binding RecalculateCommand}" />
                        <toolkit:EventToCommandBehavior EventName="ValueChanged"
                            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                            Command="{Binding ValidateCommand}"
                            CommandParameter="{x:Static validators:TransactionFormModelValidator.ForeignAmountProperty}" />
                    </editors:SfNumericEntry.Behaviors>
                </editors:SfNumericEntry>
            </sf:SfTextInputLayout>

            <!-- ID Type -->
            <sf:SfTextInputLayout Hint="ID Type" ContainerType="Outlined"
                ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                    ConverterParameter={x:Static validators:TransactionFormModelValidator.CustomerIdTypeProperty}}"
                HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                    ConverterParameter={x:Static validators:TransactionFormModelValidator.CustomerIdTypeProperty}}">
                <Picker ItemsSource="{Binding CustomerIdTypes}" SelectedItem="{Binding CustomerIdType}">
                    <Picker.Behaviors>
                        <toolkit:EventToCommandBehavior EventName="SelectedIndexChanged"
                            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                            Command="{Binding ValidateCommand}"
                            CommandParameter="{x:Static validators:TransactionFormModelValidator.CustomerIdTypeProperty}" />
                    </Picker.Behaviors>
                </Picker>
            </sf:SfTextInputLayout>

            <!-- ID Number -->
            <sf:SfTextInputLayout Hint="ID Number" ContainerType="Outlined"
                ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                    ConverterParameter={x:Static validators:TransactionFormModelValidator.CustomerIdNumberProperty}}"
                HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                    ConverterParameter={x:Static validators:TransactionFormModelValidator.CustomerIdNumberProperty}}">
                <Entry Text="{Binding CustomerIdNumber}" MaxLength="50">
                    <Entry.Behaviors>
                        <toolkit:EventToCommandBehavior EventName="TextChanged"
                            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                            Command="{Binding ValidateCommand}"
                            CommandParameter="{x:Static validators:TransactionFormModelValidator.CustomerIdNumberProperty}" />
                    </Entry.Behaviors>
                </Entry>
            </sf:SfTextInputLayout>

            <!-- Customer Name -->
            <sf:SfTextInputLayout Hint="Customer Name" ContainerType="Outlined"
                ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                    ConverterParameter={x:Static validators:TransactionFormModelValidator.CustomerNameProperty}}"
                HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                    ConverterParameter={x:Static validators:TransactionFormModelValidator.CustomerNameProperty}}">
                <Entry Text="{Binding CustomerName}" MaxLength="100">
                    <Entry.Behaviors>
                        <toolkit:EventToCommandBehavior EventName="TextChanged"
                            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                            Command="{Binding ValidateCommand}"
                            CommandParameter="{x:Static validators:TransactionFormModelValidator.CustomerNameProperty}" />
                    </Entry.Behaviors>
                </Entry>
            </sf:SfTextInputLayout>

            <!-- Calculated info -->
            <Grid ColumnDefinitions="*,*" Margin="0,10,0,0">
                <Label Grid.Column="0" FontSize="16">
                    <Label.FormattedText>
                        <FormattedString>
                            <Span Text="Rate: " FontAttributes="Bold" />
                            <Span Text="{Binding AppliedRate, StringFormat='{0:N4}'}" />
                        </FormattedString>
                    </Label.FormattedText>
                </Label>
                <Label Grid.Column="1" FontSize="16" HorizontalTextAlignment="End">
                    <Label.FormattedText>
                        <FormattedString>
                            <Span Text="HUF: " FontAttributes="Bold" />
                            <Span Text="{Binding HufAmount, StringFormat='{0:N2}'}" />
                        </FormattedString>
                    </Label.FormattedText>
                </Label>
            </Grid>

            <Button Text="Exchange" Command="{Binding SubmitCommand}"
                    Style="{StaticResource PrimaryButton}" HorizontalOptions="FillAndExpand" Margin="0,15,0,0" />
            <ActivityIndicator IsRunning="{Binding IsLoading}" IsVisible="{Binding IsLoading}" Color="#2E5C8A" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

### 4.7 Views/TransactionView.xaml.cs

```csharp
namespace Exchange.Solution.Views;

public partial class TransactionView : ContentPage
{
    public static string Name => nameof(TransactionView);
    public TransactionView(TransactionViewModel viewModel)
    {
        this.BindingContext = viewModel;
        InitializeComponent();
    }
}
```

---

## PHASE 5 — User Management

### 5.1 Services/IUserApiService.cs

```csharp
namespace Exchange.Solution.Services;

public interface IUserApiService
{
    Task<ErrorOr<UserListResponse>> GetAllUsersAsync();
    Task<ErrorOr<UserResponseModel>> GetUserByIdAsync(string userId);
    Task<ErrorOr<UserResponseModel>> CreateUserAsync(CreateUserRequest request);
    Task<ErrorOr<UserResponseModel>> UpdateUserAsync(string userId, UpdateUserRequest request);
    Task<ErrorOr<Success>> DeleteUserAsync(string userId);
    Task<ErrorOr<Success>> ResetPasswordAsync(string userId, string newPassword);
}
```

### 5.2 Services/UserApiService.cs

```csharp
namespace Exchange.Solution.Services;

public class UserApiService(IHttpClientFactory httpClientFactory) : IUserApiService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ExchangeApi");

    public async Task<ErrorOr<UserListResponse>> GetAllUsersAsync()
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync("api/user");
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<UserListResponse>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<UserResponseModel>> GetUserByIdAsync(string userId)
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync($"api/user/{userId}");
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<UserResponseModel>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<UserResponseModel>> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync("api/user", request);
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<UserResponseModel>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<UserResponseModel>> UpdateUserAsync(string userId, UpdateUserRequest request)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PutAsJsonAsync($"api/user/{userId}", request);
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<UserResponseModel>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<Success>> DeleteUserAsync(string userId)
    {
        try
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"api/user/{userId}");
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return Result.Success;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<Success>> ResetPasswordAsync(string userId, string newPassword)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync($"api/user/{userId}/reset-password", new { NewPassword = newPassword });
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return Result.Success;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }
}
```

### 5.3 Models/UserFormModel.cs

```csharp
namespace Exchange.Solution.Models;

public partial class UserFormModel : ObservableObject
{
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private UserRole role = UserRole.User;
}
```

### 5.4 Validators/UserFormModelValidator.cs

```csharp
namespace Exchange.Solution.Validators;

public class UserFormModelValidator : AbstractValidator<UserFormModel>
{
    public static string NameProperty => nameof(UserFormModel.Name);
    public static string EmailProperty => nameof(UserFormModel.Email);
    public static string PasswordProperty => nameof(UserFormModel.Password);
    public static string RoleProperty => nameof(UserFormModel.Role);

    public UserFormModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage("Name required (max 100)!");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Valid email required!");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage("Password min 8 chars!");
        RuleFor(x => x.Role).IsInEnum().WithMessage("Role required!");
    }
}
```

### 5.5 ViewModels/UserListViewModel.cs

```csharp
namespace Exchange.Solution.ViewModels;

[ObservableObject]
public partial class UserListViewModel(IUserApiService userService, ITokenStorageService tokenStorage)
{
    public IAsyncRelayCommand AppearingCommand => new AsyncRelayCommand(OnAppearingAsync);
    public IAsyncRelayCommand DisappearingCommand => new AsyncRelayCommand(OnDisappearingAsync);
    public IAsyncRelayCommand DeleteCommand => new AsyncRelayCommand<string>(OnDeleteAsync);
    public IAsyncRelayCommand AddNewUserCommand => new AsyncRelayCommand(OnAddNewUserAsync);

    [ObservableProperty] private ObservableCollection<UserResponseModel> users = new();

    private async Task OnAppearingAsync()
    {
        var result = await userService.GetAllUsersAsync();
        if (result.IsError) { await Application.Current!.MainPage!.DisplayAlert("Error", result.FirstError.Description, "OK"); return; }
        Users = new ObservableCollection<UserResponseModel>(result.Value.Users);
    }
    private async Task OnDisappearingAsync() { }

    private async Task OnDeleteAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return;
        bool confirm = await Application.Current!.MainPage!.DisplayAlert("Confirm", "Delete this user?", "Yes", "No");
        if (!confirm) return;
        var result = await userService.DeleteUserAsync(userId);
        var msg = result.IsError ? result.FirstError.Description : "User deleted.";
        if (!result.IsError) { var u = Users.FirstOrDefault(x => x.Id == userId); if (u != null) Users.Remove(u); }
        await Application.Current.MainPage.DisplayAlert(result.IsError ? "Error" : "Info", msg, "OK");
    }

    private async Task OnAddNewUserAsync()
    {
        Shell.Current.ClearNavigationStack();
        await Shell.Current.GoToAsync(CreateOrEditUserView.Name);
    }
}
```

### 5.6 ViewModels/CreateOrEditUserViewModel.cs

```csharp
namespace Exchange.Solution.ViewModels;

public partial class CreateOrEditUserViewModel(IUserApiService userService) : UserFormModel, IQueryAttributable
{
    [ObservableProperty] private ValidationResult validationResult = new();
    [ObservableProperty] private string title = "Add User";
    [ObservableProperty] private bool isEditMode;
    [ObservableProperty] private string userId = string.Empty;
    [ObservableProperty] private IList<UserRole> roles = [UserRole.User, UserRole.Admin];
    [ObservableProperty] private bool showPassword = true;

    public IAsyncRelayCommand AppearingCommand => new AsyncRelayCommand(OnAppearingAsync);
    public IAsyncRelayCommand DisappearingCommand => new AsyncRelayCommand(OnDisappearingAsync);
    public IAsyncRelayCommand SubmitCommand => new AsyncRelayCommand(OnSubmitAsync);
    public IAsyncRelayCommand CancelCommand => new AsyncRelayCommand(OnCancelAsync);
    public IRelayCommand ValidateCommand => new AsyncRelayCommand<string>(OnValidateAsync);

    private readonly UserFormModelValidator validator = new();
    private delegate Task ButtonActionDelegate();
    private ButtonActionDelegate asyncButtonAction;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("User", out object? result) && result is UserResponseModel user)
        {
            UserId = user.Id; Name = user.Name; Email = user.Email;
            Role = user.Roles.Contains("Administrator") || user.Roles.Contains("Admin") ? UserRole.Admin : UserRole.User;
            asyncButtonAction = OnUpdateAsync; Title = "Edit User"; IsEditMode = true; ShowPassword = false;
        }
        else { asyncButtonAction = OnSaveAsync; Title = "Add User"; IsEditMode = false; ShowPassword = true; }
    }

    private async Task OnAppearingAsync() { }
    private async Task OnDisappearingAsync() { }
    private async Task OnSubmitAsync() => await asyncButtonAction();

    private async Task OnSaveAsync()
    {
        ValidationResult = await validator.ValidateAsync(this);
        if (!ValidationResult.IsValid) return;
        var req = new CreateUserRequest { Name = Name, Email = Email, Password = Password, Role = Role };
        var result = await userService.CreateUserAsync(req);
        var msg = result.IsError ? result.FirstError.Description : "User created.";
        await Application.Current!.MainPage!.DisplayAlert(result.IsError ? "Error" : "Info", msg, "OK");
        if (!result.IsError) { Shell.Current.ClearNavigationStack(); await Shell.Current.GoToAsync(UserListView.Name); }
    }

    private async Task OnUpdateAsync()
    {
        // Skip password validation for update
        var updateValidator = new InlineValidator<UserFormModel>();
        updateValidator.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        updateValidator.RuleFor(x => x.Email).NotEmpty().EmailAddress();
        updateValidator.RuleFor(x => x.Role).IsInEnum();
        ValidationResult = await updateValidator.ValidateAsync(this);
        if (!ValidationResult.IsValid) return;
        var req = new UpdateUserRequest { Name = Name, Email = Email, Role = Role };
        var result = await userService.UpdateUserAsync(UserId, req);
        var msg = result.IsError ? result.FirstError.Description : "User updated.";
        await Application.Current!.MainPage!.DisplayAlert(result.IsError ? "Error" : "Info", msg, "OK");
        if (!result.IsError) { Shell.Current.ClearNavigationStack(); await Shell.Current.GoToAsync(UserListView.Name); }
    }

    private async Task OnCancelAsync()
    { Shell.Current.ClearNavigationStack(); await Shell.Current.GoToAsync(UserListView.Name); }

    private async Task OnValidateAsync(string? prop)
    {
        if (string.IsNullOrEmpty(prop)) return;
        var r = await validator.ValidateAsync(this, o => o.IncludeProperties(prop));
        ValidationResult.Errors.RemoveAll(x => x.PropertyName == prop);
        ValidationResult.Errors.AddRange(r.Errors);
        OnPropertyChanged(nameof(ValidationResult));
    }
}
```

### 5.7 Components/UserListComponent.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="Exchange.Solution.Components.UserListComponent"
             x:Name="this">
    <Border Padding="10,8" StrokeThickness="0.5" Stroke="LightGray">
        <Grid ColumnDefinitions="2*,*,2*,80,80" Padding="5,0">
            <Label Grid.Column="0" Text="{Binding User.Name, Source={x:Reference this}}"
                   VerticalTextAlignment="Center" />
            <Label Grid.Column="1" VerticalTextAlignment="Center">
                <Label.Text>
                    <Binding Path="User.Roles[0]" Source="{x:Reference this}" FallbackValue="User" />
                </Label.Text>
            </Label>
            <Label Grid.Column="2" Text="{Binding User.Email, Source={x:Reference this}}"
                   VerticalTextAlignment="Center" />
            <Button Grid.Column="3" Text="Edit"
                    Command="{Binding EditCommand, Source={x:Reference this}}"
                    BackgroundColor="#6C757D" TextColor="White" FontSize="12"
                    Padding="8,4" VerticalOptions="Center" />
            <Button Grid.Column="4" Text="Delete"
                    Command="{Binding DeleteCommand, Source={x:Reference this}}"
                    CommandParameter="{Binding User.Id, Source={x:Reference this}}"
                    BackgroundColor="#DC3545" TextColor="White" FontSize="12"
                    Padding="8,4" VerticalOptions="Center" />
        </Grid>
    </Border>
</ContentView>
```

### 5.8 Components/UserListComponent.xaml.cs

```csharp
namespace Exchange.Solution.Components;

public partial class UserListComponent : ContentView
{
    public static readonly BindableProperty UserProperty = BindableProperty.Create(
        nameof(User), typeof(UserResponseModel), typeof(UserListComponent));
    public UserResponseModel User
    {
        get => (UserResponseModel)GetValue(UserProperty);
        set => SetValue(UserProperty, value);
    }

    public static readonly BindableProperty DeleteCommandProperty = BindableProperty.Create(
        nameof(DeleteCommand), typeof(IAsyncRelayCommand), typeof(UserListComponent));
    public IAsyncRelayCommand DeleteCommand
    {
        get => (IAsyncRelayCommand)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(string), typeof(UserListComponent));
    public string CommandParameter
    {
        get => (string)GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public IAsyncRelayCommand EditCommand => new AsyncRelayCommand(OnEditAsync);

    public UserListComponent() { InitializeComponent(); }

    private async Task OnEditAsync()
    {
        var nav = new ShellNavigationQueryParameters { { "User", this.User } };
        Shell.Current.ClearNavigationStack();
        await Shell.Current.GoToAsync(CreateOrEditUserView.Name, nav);
    }
}
```

### 5.9 Views/UserListView.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
             xmlns:viewModels="clr-namespace:Exchange.Solution.ViewModels"
             xmlns:components="clr-namespace:Exchange.Solution.Components"
             xmlns:models="clr-namespace:Solution.Core.Models.Responses;assembly=Solution.Core"
             x:Class="Exchange.Solution.Views.UserListView"
             x:DataType="viewModels:UserListViewModel"
             x:Name="this">
    <ContentPage.Behaviors>
        <toolkit:EventToCommandBehavior Command="{Binding AppearingCommand}" EventName="Appearing"
            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}" />
        <toolkit:EventToCommandBehavior Command="{Binding DisappearingCommand}" EventName="Disappearing"
            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}" />
    </ContentPage.Behaviors>

    <Grid RowDefinitions="Auto,Auto,*" Padding="40">
        <Grid Grid.Row="0" ColumnDefinitions="*,Auto">
            <Label Grid.Column="0" Text="User Management" Style="{StaticResource PageTitle}" />
            <Button Grid.Column="1" Text="Add User" Command="{Binding AddNewUserCommand}"
                    Style="{StaticResource PrimaryButton}" VerticalOptions="Center" />
        </Grid>

        <!-- Table Header -->
        <Grid Grid.Row="1" BackgroundColor="#1B3A5C" Padding="10,8"
              ColumnDefinitions="2*,*,2*,80,80" Margin="0,0,0,0">
            <Label Grid.Column="0" Text="Username" Style="{StaticResource TableHeader}" />
            <Label Grid.Column="1" Text="Role" Style="{StaticResource TableHeader}" />
            <Label Grid.Column="2" Text="Email" Style="{StaticResource TableHeader}" />
            <Label Grid.Column="3" Text="Actions" Style="{StaticResource TableHeader}" HorizontalTextAlignment="Center" />
        </Grid>

        <ScrollView Grid.Row="2">
            <CollectionView ItemsSource="{Binding Users}">
                <CollectionView.ItemsLayout>
                    <LinearItemsLayout Orientation="Vertical" ItemSpacing="2" />
                </CollectionView.ItemsLayout>
                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="models:UserResponseModel">
                        <components:UserListComponent
                            User="{Binding .}"
                            DeleteCommand="{Binding Source={RelativeSource AncestorType={x:Type viewModels:UserListViewModel}}, Path=DeleteCommand}"
                            CommandParameter="{Binding Id}" />
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </ScrollView>
    </Grid>
</ContentPage>
```

### 5.10 Views/UserListView.xaml.cs

```csharp
namespace Exchange.Solution.Views;

public partial class UserListView : ContentPage
{
    public static string Name => nameof(UserListView);
    public UserListView(UserListViewModel viewModel)
    {
        this.BindingContext = viewModel;
        InitializeComponent();
    }
}
```

### 5.11 Views/CreateOrEditUserView.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
             xmlns:sf="clr-namespace:Syncfusion.Maui.Toolkit.TextInputLayout;assembly=Syncfusion.Maui.Toolkit"
             xmlns:viewModels="clr-namespace:Exchange.Solution.ViewModels"
             xmlns:validators="clr-namespace:Exchange.Solution.Validators"
             x:Class="Exchange.Solution.Views.CreateOrEditUserView"
             x:DataType="viewModels:CreateOrEditUserViewModel"
             x:Name="this">
    <ContentPage.Behaviors>
        <toolkit:EventToCommandBehavior Command="{Binding AppearingCommand}" EventName="Appearing" />
        <toolkit:EventToCommandBehavior Command="{Binding DisappearingCommand}" EventName="Disappearing" />
    </ContentPage.Behaviors>

    <ScrollView>
        <VerticalStackLayout Padding="40" Spacing="10" MaximumWidthRequest="500">
            <Label Text="{Binding Title}" Style="{StaticResource PageTitle}" />

            <!-- Username -->
            <sf:SfTextInputLayout Hint="Username" ContainerType="Outlined"
                ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                    ConverterParameter={x:Static validators:UserFormModelValidator.NameProperty}}"
                HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                    ConverterParameter={x:Static validators:UserFormModelValidator.NameProperty}}">
                <Entry Text="{Binding Name}" MaxLength="100">
                    <Entry.Behaviors>
                        <toolkit:EventToCommandBehavior EventName="TextChanged"
                            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                            Command="{Binding ValidateCommand}"
                            CommandParameter="{x:Static validators:UserFormModelValidator.NameProperty}" />
                    </Entry.Behaviors>
                </Entry>
            </sf:SfTextInputLayout>

            <!-- Role -->
            <sf:SfTextInputLayout Hint="Role" ContainerType="Outlined"
                ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                    ConverterParameter={x:Static validators:UserFormModelValidator.RoleProperty}}"
                HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                    ConverterParameter={x:Static validators:UserFormModelValidator.RoleProperty}}">
                <Picker ItemsSource="{Binding Roles}" SelectedItem="{Binding Role}">
                    <Picker.Behaviors>
                        <toolkit:EventToCommandBehavior EventName="SelectedIndexChanged"
                            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                            Command="{Binding ValidateCommand}"
                            CommandParameter="{x:Static validators:UserFormModelValidator.RoleProperty}" />
                    </Picker.Behaviors>
                </Picker>
            </sf:SfTextInputLayout>

            <!-- Email -->
            <sf:SfTextInputLayout Hint="Email" ContainerType="Outlined"
                ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                    ConverterParameter={x:Static validators:UserFormModelValidator.EmailProperty}}"
                HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                    ConverterParameter={x:Static validators:UserFormModelValidator.EmailProperty}}">
                <Entry Text="{Binding Email}" Keyboard="Email">
                    <Entry.Behaviors>
                        <toolkit:EventToCommandBehavior EventName="TextChanged"
                            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                            Command="{Binding ValidateCommand}"
                            CommandParameter="{x:Static validators:UserFormModelValidator.EmailProperty}" />
                    </Entry.Behaviors>
                </Entry>
            </sf:SfTextInputLayout>

            <!-- Password (only in create mode) -->
            <sf:SfTextInputLayout Hint="Password" ContainerType="Outlined"
                IsVisible="{Binding ShowPassword}"
                ErrorText="{Binding ValidationResult, Converter={StaticResource ValidationResultToErrorMessageConverter},
                    ConverterParameter={x:Static validators:UserFormModelValidator.PasswordProperty}}"
                HasError="{Binding ValidationResult, Converter={StaticResource ValidationResultToHasErrorConverter},
                    ConverterParameter={x:Static validators:UserFormModelValidator.PasswordProperty}}">
                <Entry Text="{Binding Password}" IsPassword="True">
                    <Entry.Behaviors>
                        <toolkit:EventToCommandBehavior EventName="TextChanged"
                            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}"
                            Command="{Binding ValidateCommand}"
                            CommandParameter="{x:Static validators:UserFormModelValidator.PasswordProperty}" />
                    </Entry.Behaviors>
                </Entry>
            </sf:SfTextInputLayout>

            <HorizontalStackLayout Spacing="15" HorizontalOptions="Center" Margin="0,20,0,0">
                <Button Text="Save" Command="{Binding SubmitCommand}"
                        Style="{StaticResource PrimaryButton}" WidthRequest="120" />
                <Button Text="Cancel" Command="{Binding CancelCommand}"
                        Style="{StaticResource SecondaryButton}" WidthRequest="120" />
            </HorizontalStackLayout>
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

### 5.12 Views/CreateOrEditUserView.xaml.cs

```csharp
namespace Exchange.Solution.Views;

public partial class CreateOrEditUserView : ContentPage
{
    public static string Name => nameof(CreateOrEditUserView);
    public CreateOrEditUserView(CreateOrEditUserViewModel viewModel)
    {
        this.BindingContext = viewModel;
        InitializeComponent();
    }
}
```

---

## PHASE 6 — Statistics

### 6.1 Services/IStatisticsApiService.cs

```csharp
namespace Exchange.Solution.Services;

public interface IStatisticsApiService
{
    Task<ErrorOr<List<RateStatisticsResponse>>> GetRateStatisticsAsync(DateOnly startDate, DateOnly endDate);
    Task<ErrorOr<List<TransactionStatisticsResponse>>> GetTransactionStatisticsAsync(DateOnly startDate, DateOnly endDate);
    Task<ErrorOr<SummaryStatisticsResponse>> GetSummaryAsync();
}
```

### 6.2 Services/StatisticsApiService.cs

```csharp
namespace Exchange.Solution.Services;

public class StatisticsApiService(IHttpClientFactory httpClientFactory) : IStatisticsApiService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ExchangeApi");

    public async Task<ErrorOr<List<RateStatisticsResponse>>> GetRateStatisticsAsync(DateOnly startDate, DateOnly endDate)
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync($"api/statistics/rates?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<List<RateStatisticsResponse>>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<List<TransactionStatisticsResponse>>> GetTransactionStatisticsAsync(DateOnly startDate, DateOnly endDate)
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync($"api/statistics/transactions?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<List<TransactionStatisticsResponse>>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }

    public async Task<ErrorOr<SummaryStatisticsResponse>> GetSummaryAsync()
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync("api/statistics/summary");
            if (!response.IsSuccessStatusCode) return Error.Failure(description: await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<SummaryStatisticsResponse>())!;
        }
        catch (Exception ex) { return Error.Failure(description: ex.Message); }
    }
}
```

### 6.3 Models/RateChartDataPoint.cs

```csharp
namespace Exchange.Solution.Models;

public class RateChartDataPoint
{
    public DateTime Date { get; set; }
    public decimal BuyRate { get; set; }
    public decimal SellRate { get; set; }
}
```

### 6.4 ViewModels/StatisticsViewModel.cs

```csharp
namespace Exchange.Solution.ViewModels;

[ObservableObject]
public partial class StatisticsViewModel(IStatisticsApiService statisticsService)
{
    public IAsyncRelayCommand AppearingCommand => new AsyncRelayCommand(OnAppearingAsync);
    public IAsyncRelayCommand DisappearingCommand => new AsyncRelayCommand(OnDisappearingAsync);
    public IAsyncRelayCommand LoadDataCommand => new AsyncRelayCommand(OnLoadDataAsync);

    [ObservableProperty] private DateTime startDate = DateTime.Now.AddDays(-30);
    [ObservableProperty] private DateTime endDate = DateTime.Now;
    [ObservableProperty] private ObservableCollection<RateChartDataPoint> usdRateData = new();
    [ObservableProperty] private ObservableCollection<RateChartDataPoint> gbpRateData = new();
    [ObservableProperty] private ObservableCollection<RateChartDataPoint> chfRateData = new();
    [ObservableProperty] private bool isLoading;

    private async Task OnAppearingAsync() => await OnLoadDataAsync();
    private async Task OnDisappearingAsync() { }

    private async Task OnLoadDataAsync()
    {
        IsLoading = true;
        var result = await statisticsService.GetRateStatisticsAsync(
            DateOnly.FromDateTime(StartDate), DateOnly.FromDateTime(EndDate));
        if (result.IsError)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", result.FirstError.Description, "OK");
            IsLoading = false; return;
        }
        UsdRateData = new(); GbpRateData = new(); ChfRateData = new();
        foreach (var cs in result.Value)
        {
            var pts = cs.DataPoints.Select(dp => new RateChartDataPoint
            {
                Date = dp.Date.ToDateTime(TimeOnly.MinValue),
                BuyRate = dp.BuyRate, SellRate = dp.SellRate
            }).ToList();
            switch (cs.Currency)
            {
                case "USD": UsdRateData = new(pts); break;
                case "GBP": GbpRateData = new(pts); break;
                case "CHF": ChfRateData = new(pts); break;
            }
        }
        IsLoading = false;
    }
}
```

### 6.5 Views/StatisticsView.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
             xmlns:viewModels="clr-namespace:Exchange.Solution.ViewModels"
             xmlns:sfCharts="clr-namespace:Syncfusion.Maui.Toolkit.Charts;assembly=Syncfusion.Maui.Toolkit"
             x:Class="Exchange.Solution.Views.StatisticsView"
             x:DataType="viewModels:StatisticsViewModel"
             x:Name="this">
    <ContentPage.Behaviors>
        <toolkit:EventToCommandBehavior Command="{Binding AppearingCommand}" EventName="Appearing"
            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}" />
        <toolkit:EventToCommandBehavior Command="{Binding DisappearingCommand}" EventName="Disappearing"
            BindingContext="{Binding Source={x:Reference this}, Path=BindingContext}" />
    </ContentPage.Behaviors>

    <Grid RowDefinitions="Auto,Auto,*" Padding="40">
        <Label Grid.Row="0" Text="Exchange Stats" Style="{StaticResource PageTitle}" />

        <!-- Date Range -->
        <HorizontalStackLayout Grid.Row="1" Spacing="10" Margin="0,0,0,20">
            <Label Text="From:" VerticalOptions="Center" />
            <DatePicker Date="{Binding StartDate}" Format="yyyy-MM-dd" />
            <Label Text="to" VerticalOptions="Center" />
            <DatePicker Date="{Binding EndDate}" Format="yyyy-MM-dd" />
            <Button Text="Load" Command="{Binding LoadDataCommand}" Style="{StaticResource PrimaryButton}" />
            <ActivityIndicator IsRunning="{Binding IsLoading}" IsVisible="{Binding IsLoading}" Color="#2E5C8A" />
        </HorizontalStackLayout>

        <!-- Chart -->
        <sfCharts:SfCartesianChart Grid.Row="2">
            <sfCharts:SfCartesianChart.Title>
                <Label Text="Exchange Rate Trends" FontSize="18" FontAttributes="Bold" HorizontalOptions="Center" />
            </sfCharts:SfCartesianChart.Title>

            <sfCharts:SfCartesianChart.XAxes>
                <sfCharts:DateTimeAxis LabelFormat="MMM dd" />
            </sfCharts:SfCartesianChart.XAxes>

            <sfCharts:SfCartesianChart.YAxes>
                <sfCharts:NumericalAxis />
            </sfCharts:SfCartesianChart.YAxes>

            <sfCharts:LineSeries ItemsSource="{Binding UsdRateData}"
                                XBindingPath="Date" YBindingPath="BuyRate"
                                Label="USD to HUF" StrokeWidth="2" />
            <sfCharts:LineSeries ItemsSource="{Binding GbpRateData}"
                                XBindingPath="Date" YBindingPath="BuyRate"
                                Label="GBP to HUF" StrokeWidth="2" />
            <sfCharts:LineSeries ItemsSource="{Binding ChfRateData}"
                                XBindingPath="Date" YBindingPath="BuyRate"
                                Label="CHF to HUF" StrokeWidth="2" />

            <sfCharts:SfCartesianChart.Legend>
                <sfCharts:ChartLegend Placement="Bottom" />
            </sfCharts:SfCartesianChart.Legend>
        </sfCharts:SfCartesianChart>
    </Grid>
</ContentPage>
```

### 6.6 Views/StatisticsView.xaml.cs

```csharp
namespace Exchange.Solution.Views;

public partial class StatisticsView : ContentPage
{
    public static string Name => nameof(StatisticsView);
    public StatisticsView(StatisticsViewModel viewModel)
    {
        this.BindingContext = viewModel;
        InitializeComponent();
    }
}
```

---

## PHASE 7 — API Bug Fixes

### 7.1 Fix UserController route

**File**: `Solution.WebAPI/Controllers/UserController.cs`
Change `[Route("Controller")]` → `[Route("api/user")]`

### 7.2 Fix namespace in CreateUserRequest

**File**: `Solution.Core/Models/Requests/CreateUserRequest.cs`
Change `using Solution.Domain.Enums;` → `using Solution.Database.Enums;`

### 7.3 Register missing services in DI

**File**: `Solution.WebAPI/DependencyInjectionConfiguration.cs`
Add:
```csharp
builder.Services.AddTransient<IExchangeRateService, ExchangeRateService>();
builder.Services.AddTransient<ITransactionService, TransactionService>();
builder.Services.AddTransient<IStatisticsService, StatisticsService>();
builder.Services.AddTransient<IUserManagementService, UserManagementService>();
```

### 7.4 Register validators in DI

Same file, add:
```csharp
builder.Services.AddTransient<IValidator<CreateTransactionRequest>, CreateTransactionRequestValidator>();
builder.Services.AddTransient<IValidator<CreateExchangeRatesRequest>, CreateExchangeRatesRequestValidator>();
builder.Services.AddTransient<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
builder.Services.AddTransient<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();
builder.Services.AddTransient<IValidator<UpdateExchangeRateRequest>, UpdateExchangeRateRequestValidator>();
```

### 7.5 Fix TransactionController

**File**: `Solution.WebAPI/Controllers/TransactionController.cs`
- Fix missing comma in `.Match()` call
- Fix method name: `GetTransactionAsync` → `GetTransactionsAsync`

### 7.6 Download background image

Save to `Resources/Images/background.jpg`

---

## Startup Flow

1. App launches → `LoginView` shown (default ShellContent route)
2. User logs in → `AuthService.LoginAsync()` → `TokenStorageService.Store(token)`
3. Navigate to `MainView`
4. `MainViewModel.OnAppearing` → `GET /api/exchangerate/exists/{today}`
5. If NO → auto-redirect to `ExchangeRateSetView`
6. If YES → show dashboard with summary
7. User navigates via menu bar
8. Logout → `TokenStorageService.Clear()` → back to `LoginView`
9. Any 401 → `AuthenticatedHttpClientHandler` → auto-redirect to `LoginView`

---

## Verification

1. `dotnet build` targeting `net10.0-windows10.0.19041.0`
2. Start Solution.WebAPI
3. Login → check JWT stored → navigate to Main
4. Set rates → verify in API
5. Currency Exchange → buy/sell transaction → check HUF calculation
6. Statistics → date range → chart renders
7. User Management → CRUD users
8. Logout → back to login
9. 401 handling → auto-redirect
