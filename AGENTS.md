# Copilot Instructions for PricePredictor

## PERSISTENCE REPOSITORY RULES
- ✅ Each repository must operate on a single table only
- ✅ Repository implementation names must come from the table name they operate on
- ✅ Exception: if resources share a common abstract parent, use the parent name and that repository may operate on all child tables
- ✅ Prefer EF Core LINQ queries/updates for repository logic
- ❌ Do not call `ExecuteSqlInterpolatedAsync` unless the user explicitly asks for raw SQL in that change request
- ✅ Cross-table use cases must be orchestrated via a Unit of Work or application service, not by a single repository

## UNIT OF WORK
- ✅ Use Unit of Work to coordinate multiple repositories and dbvector operations in one application flow
- ✅ Unit of Work must expose `SaveChangesAsync(CancellationToken)` with DbContext-equivalent behavior
- ✅ Repositories participating in Unit of Work should stage tracked changes and let Unit of Work commit them

## NO MARKDOWN GENERATION
- ❌ DO NOT create markdown files automatically
- ❌ DO NOT write documentation files
- ❌ DO NOT create README, SUMMARY, or INDEX files
- ✅ Only create markdown if explicitly requested: "create a file called X.md"

## PROMPT CENTRALIZATION
- ✅ Store all prompt text used in `.cs` files in `PricePredictor.Application/PromptHelper.cs`
- ✅ Reuse prompt constants/builders from `PromptHelper` instead of duplicating prompt strings in services/clients
- ✅ Keep `NormalizeForEmbedding` in `PromptHelper` and call it from other classes
- ❌ Do not declare prompt literals in other `.cs` files unless explicitly requested

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

## TEST DOUBLES
- ✅ Unit tests in `PricePredictor.Tests` must mock dependencies with NSubstitute
- ❌ Do not create internal stubs for integration tests
- ✅ Integration tests in `PricePredictor.Tests.Integration` must use real implementations wired through `WebApplicationFactory`

## HTTP CLIENTS
- ✅ Use typed HTTP clients (wrapper client classes) instead of injecting `HttpClient` directly into services
- ✅ Register typed clients with `AddHttpClient<Interface, Implementation>`
- ✅ Each client must have its own extension method (e.g., `AddGoogleNewsRssClient`)
- ✅ All DI and setup should be in extension methods extending `IServiceCollection`
- ✅ Keep HTTP client extension methods in the Infrastructure project (`PricePredicator.Infrastructure/ClientsExtensions.cs`)
- ✅ All client extension methods must be called explicitly in `Program.cs` (not inside `AddAppServices`)
- ❌ Do not introduce adapter patterns for clients
- ✅ **CRITICAL: HTTP client methods MUST THROW exceptions on failure, NEVER return null**
  - ❌ Do NOT silently catch HTTP errors and return null
  - ❌ Do NOT swallow HttpRequestException or other transient failures
  - ✅ Let exceptions propagate to caller so failures are visible
  - ✅ Add diagnostic context to exceptions (model name, chunk count, prompt size, etc.)
  - Example: If Ollama returns 404 or empty response, throw `InvalidOperationException` with full context
  - Rationale: Silent `null` returns hide bugs; explicit exceptions force caller to handle failures visibly

## SERIALIZATION & DTOS
- ✅ Application layer models NEVER have `System.Text.Json.Serialization` attributes
- ✅ Create DTOs in Infrastructure layer with all serialization attributes
- ✅ Use mappers to convert Infrastructure DTOs → Application models, mappers should be extensions blocks
- ✅ Keep serialization concerns isolated in Infrastructure layer
- Example pattern:
  - Application: `WeatherForecast` (plain model)
  - Infrastructure: `WeatherForecastResponse` (dto) (with `[JsonPropertyName]` attributes)
  - Infrastructure: `WeatherMapper.MapToModel(dto)` to convert

## PERSISTENCE LAYER
- ✅ Database entity models (EF Core models) are plain C# classes without special naming suffixes
- ✅ Example: `ArticleLink`, `User`, `Transaction` (not `ArticleLinkEntity`, `UserEntity`, etc.)
- ✅ Create models in Persistence layer under `Models/` directory (mirrors Application layer Models directory)
- ✅ Configure models using `IEntityTypeConfiguration<T>` pattern:
  - `T` (generic type parameter) MUST be the model class from **Application layer** (`PricePredictor.Application.Models` or similar)
  - Create configuration classes in `Persistence/Configurations/` directory
  - Example: Application model `ArticleLink` → `ArticleLinkConfiguration : IEntityTypeConfiguration<ArticleLink>`
  - Each configuration class implements `void Configure(EntityTypeBuilder<T> builder)`
  - Apply all Fluent API configuration there (indexes, constraints, required properties, table names, etc.)
  - Register configurations in `DbContext.OnModelCreating()` using `modelBuilder.ApplyConfigurationsFromAssembly()`
  - This keeps entity models plain and configuration separate
- ✅ Example pattern:
  ```csharp
  // Application/Models/ArticleLink.cs - domain model
  namespace PricePredictor.Application.Models;
  public sealed class ArticleLink
  {
      public Guid Id { get; set; }
      public required string Url { get; set; }
      public required DateTime PublishedAtUtc { get; set; }
  }

  // Persistence/Models/ArticleLink.cs - EF Core entity (mirrors Application model)
  namespace PricePredictor.Persistence.Models;
  public sealed class ArticleLink
  {
      public Guid Id { get; set; }
      public required string Url { get; set; }
      public required DateTime PublishedAtUtc { get; set; }
  }

  // Persistence/Configurations/ArticleLinkConfiguration.cs
  // NOTE: T parameter references Application.Models.ArticleLink, NOT Persistence.Models.ArticleLink
  using PricePredictor.Application.Models;
  public sealed class ArticleLinkConfiguration : IEntityTypeConfiguration<ArticleLink>
  {
      public void Configure(EntityTypeBuilder<ArticleLink> builder)
      {
          builder.ToTable("ArticleLinks");
          builder.HasKey(e => e.Id);
          builder.Property(e => e.Url).IsRequired();
          builder.HasIndex(e => e.Url).IsUnique();
      }
  }

  // Persistence/PricePredictorDbContext.cs
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
      base.OnModelCreating(modelBuilder);
      modelBuilder.ApplyConfigurationsFromAssembly(typeof(PricePredictorDbContext).Assembly);
  }
  ```

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
  - Contains Domain layer in directory Domain, where all domain models are defined
  
- ✅ **Persistence Layer** (`PricePredictor.Persistence`):
  - Contains DbContext and EF Core configuration
  - Contains migrations in `/Migrations` directory
  - Contains repository implementations that depend on DbContext
  - Has ONE extension method: `AddPersistence(this IServiceCollection services, IConfiguration configuration)`
  - This extension registers DbContext and repositories

- ✅ **Infrastructure Layer** (`PricePredictor.Infrastructure`):
  - Contains external HTTP clients
  - Contains infrastructure-specific DTOs and mappers
  - Contains settings classes
  - Has extension methods for infrastructure setup (e.g., `AddGoogleNewsRssClient`, `AddNtfyClient`)
  
- ✅ **API Layer** (`PricePredictor.Api`):
  - Contains controllers and gRPC endpoints
  - Contains background services (e.g., `YahooFinanceBackgroundService`)
  - Contains `Program.cs` and startup configuration
  - Minimal business logic - delegates to Application layer

## DEPENDENCY DIRECTION
- ✅ API → Application
- ✅ API → Infrastructure
- ✅ Application defines interfaces
- ❌ Never: Application → API or Application → Infrastructure

## TEMP FILE CLEANUP
- ✅ Remove all temporary `.txt` and `.log` files created for the task before finishing
- ✅ Keep only files explicitly requested by the user
- ❌ Do not leave test output or debug logs in the repository root

## WHEN TASK IS DONE
- Say "Done" or brief summary
- ❌ DO NOT create documentation files
- ❌ DO NOT create summary or index files
- List files changed, that's it

## METHOD NAMING
- ❌ Do not create method names containing `And` (e.g., `ValidateAndSave`, `FetchAndStore`)
- ✅ Method names should represent a single responsibility
- ✅ Prefer general names that describe one cohesive action (e.g., `Validate`, `Save`, `Fetch`, `Store`)

## ID POLICY
- ✅ All IDs (except enums) must use `Guid` generated as GUIDv7
- ❌ Do not introduce new `int` or legacy random `Guid` IDs for entities/models
- ✅ Generate new IDs with `Guid.CreateVersion7()`

## APPLICATION MODELS
- ✅ Static factory methods in models must return `ErrorOr<TModel>` (for example `ErrorOr<ArticleLink>`)
- ✅ Use a single `Create(..., Guid? id = null)` factory pattern for Guid-based models (`id == null` means generate a new ID; provided `id` means rehydrate)
- ❌ Do not add `CreateFrom(...)` methods
- ✅ If a business model property changes, add/update a domain method on the model to perform that change (DDD style)
- ✅ Persist model state changes by saving through Unit of Work (`SaveChangesAsync`) instead of ad-hoc setters or direct mutation outside the model
- ❌ Do not throw exceptions for validation/domain errors from model factory methods; return `ErrorOr` errors instead

## ERROR HANDLING (APPLICATION LAYERS)
- ✅ Use `ErrorOr<T>` to return expected/domain/application errors in Application layer services and domain logic
- ❌ Do not use exceptions for normal business-rule failures in Application layer
- ✅ Reserve exceptions for truly exceptional/technical failures (e.g. infrastructure outage, unexpected runtime faults)
- ✅ All models in `PricePredictor.Application` must set values only via static factory methods
- ❌ Do not expose public setters or public constructors for creating mutable state
- ✅ Prefer private constructors + static `Create(...)` methods that enforce invariants
