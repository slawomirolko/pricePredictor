# Copilot Instructions for PricePredictor

## NO MARKDOWN GENERATION
- ❌ DO NOT create markdown files automatically
- ❌ DO NOT write documentation files
- ❌ DO NOT create README, SUMMARY, or INDEX files
- ✅ Only create markdown if explicitly requested: "create a file called X.md"

## FOCUS ON CODE ONLY
- ✅ Modify code files (.cs, .csproj, .json, .xml, etc.)
- ✅ Create executable scripts (.ps1, .bat)
- ✅ Create test files
- ❌ Stop at markdown - don't auto-generate docs

## TEST ASSERTIONS
- ✅ Use Shouldly for all test assertions
- ❌ DO NOT use FluentAssertions
- Examples:
  - `result.ShouldBe(expected);`
  - `result.ShouldBeGreaterThan(0);`
  - `result.ShouldBe(0.5, 0.001); // with tolerance`
  - `collection.Count.ShouldBe(4);`

## HTTP CLIENTS
- ✅ Use typed HTTP clients (wrapper client classes) instead of injecting `HttpClient` directly into services
- ✅ Register typed clients with `AddHttpClient<Interface, Implementation>`
- ✅ Each client must have its own extension method (e.g., `AddGoogleNewsRssClient`)
- ✅ All DI and setup should be in extension methods extending `IServiceCollection`
- ✅ Keep HTTP client extension methods in the Infrastructure project (`PricePredicator.Infrastructure/ClientsExtensions.cs`)
- ✅ All client extension methods must be called explicitly in `Program.cs` (not inside `AddAppServices`)

## SETTINGS
- ✅ Settings types must be sealed `record` with `init` properties
- ✅ Each settings type must declare `public const string SectionName = "...";`
- ✅ Add settings sections to `appsettings.json`, `appsettings.Development.json`, and `appsettings.Test.json`
- ✅ Wire settings in `Program.cs` with `Configure<T>(builder.Configuration.GetSection(T.SectionName))`

## ARCHITECTURE LAYERS
- ✅ **Application Layer** (`PricePredictor.Application`):
  - Contains all service interfaces (e.g., `IWeatherService`, `IGatewayService`)
  - Contains all service implementations (e.g., `WeatherService`, `GatewayService`)
  - Contains all repository interfaces (e.g., `IVolatilityRepository`, `IGoldNewsRepository`)
  - Contains application-specific DTOs and domain logic
  - Has ONE extension method: `AddApplication(this IServiceCollection services)`
  - This extension registers ALL application services and their interfaces
  - Call `AddApplication()` explicitly in `Program.cs`
  
- ✅ **Infrastructure Layer** (`PricePredictor.Infrastructure`):
  - Contains repository implementations (EF Core)
  - Contains database context and migrations
  - Contains external HTTP clients
  - Contains infrastructure-specific models
  - Has extension methods for infrastructure setup (e.g., `AddGoogleNewsRssClient`, `AddNtfyClient`)
  
- ✅ **API Layer** (`PricePredictor.Api`):
  - Contains controllers and gRPC endpoints
  - Contains background services (e.g., `YahooFinanceBackgroundService`)
  - Contains `Program.cs` and startup configuration
  - Minimal business logic - delegates to Application layer

- ✅ **Domain Layer** (`PricePredictor.Domain`):
  - Contains all domain entities/models (e.g., `Commodity`, `Volatility*`)
  - No infrastructure concerns (no EF Core, no HTTP, no DI)
  - Shared by Application and Infrastructure for persistence/model mapping

## DEPENDENCY DIRECTION
- ✅ API → Application
- ✅ API → Infrastructure
- ✅ Application defines interfaces
- ✅ Never: Application → API or Application → Infrastructure

## WHEN TASK IS DONE
- Say "Done" or brief summary
- ❌ DO NOT create documentation files
- ❌ DO NOT create summary or index files
- List files changed, that's it
