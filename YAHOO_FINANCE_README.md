# Yahoo Finance 1-Minute Intraday Data with Volatility Analysis

Complete .NET 10 implementation for fetching real-time intraday data from Yahoo Finance, calculating volatility metrics, and detecting panic scores using a hosted background service.

## Features

✅ **Free Yahoo Finance API Integration** - No API keys required  
✅ **1-Minute Intraday Data** - Real-time candlestick data (OHLCV)  
✅ **Logarithmic Returns** - Professional volatility calculations  
✅ **Rolling Volatility Windows** - 5, 15, and 60-minute windows  
✅ **Dual Panic Scores** - Short-term (5min) and long-term (60min) market stress detection  
✅ **Postgres Database** - Persistent storage with EF Core migrations  
✅ **Docker Support** - Complete containerization with compose  
✅ **Comprehensive Testing** - 22 unit tests for all calculations  
✅ **Typed HTTP Client** - Clean, testable architecture  

## Architecture

```
PricePredictor.Infrastructure/
├── Data/
│   ├── PricePredictorDbContext.cs (EF Core DbContext)
│   └── PricePredictorDbContextFactory.cs (Design-time factory)
└── Models/
    ├── VolatilityGold.cs
    ├── VolatilitySilver.cs
    ├── VolatilityNaturalGas.cs
    └── VolatilityOil.cs

PricePredictor.App/
├── Finance/
│   ├── YahooFinanceClient.cs (Typed HTTP client)
│   ├── YahooFinanceModels.cs (JSON response DTOs)
│   ├── YahooFinanceSettings.cs (Configuration + symbol mapper)
│   ├── IndicatorsCalculator.cs (All volatility calculations)
│   ├── IVolatilityRepository.cs (Data access interface)
│   └── VolatilityRepository.cs (EF Core repository)
├── YahooFinanceBackgroundService.cs (Hosted service - main loop)
├── Program.cs (DI registration + migration runner)
└── appsettings.json (Configuration)

PricePredictor.Tests/
└── Finance/
    └── IndicatorsCalculatorTests.cs (22 comprehensive unit tests)
```

## Database Schema

Four separate tables, one per commodity:

### Gold / Silver / NaturalGas / Oil

| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Primary key |
| Timestamp | datetime | UTC timestamp of 1-minute candle |
| Open | decimal(18,8) | Opening price |
| High | decimal(18,8) | Highest price in period |
| Low | decimal(18,8) | Lowest price in period |
| Close | decimal(18,8) | Closing price |
| Volume | long? | Trading volume (nullable) |
| LogarithmicReturn | double | ln(Close_t / Close_t-1) |
| RollingVol5 | double | 5-minute rolling volatility (stdev) |
| RollingVol15 | double | 15-minute rolling volatility |
| RollingVol60 | double | 60-minute rolling volatility |
| ShortPanicScore | double | 5-min panic score (uses vol5 vs vol60) |
| LongPanicScore | double | 60-min panic score (uses vol60 vs vol60) |
| CreatedAtUtc | datetime | Insert timestamp |

**Index:** `Timestamp` (non-unique for time-range queries)

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=postgres;Port=5432;Database=pricepredictor;User Id=postgres;Password=postgres;"
  },
  "YahooFinance": {
    "Symbols": [ "GC=F", "SI=F", "NG=F", "CL=F" ],
    "Interval": "1m",
    "Range": "1d",
    "VolatilityBackupMinutes": 10,
    "VolatilityWindows": [ 5, 15, 60 ]
  }
}
```

**Symbols:**
- `GC=F` → Gold (Futures)
- `SI=F` → Silver (Futures)
- `NG=F` → Natural Gas (Futures)
- `CL=F` → Crude Oil (Futures)

**Settings:**
- `VolatilityBackupMinutes`: Log complete 10-minute data every N minutes (default: 10)
- `VolatilityWindows`: Windows for rolling volatility calculation

## Panic Score Calculation

### NormalizedVolatilityPanicScore Formula

```
panic_score = w1 * |return| + w2 * (vol_short / vol_long)
```

**Default Weights:**
- `w1 = 0.3` - Absolute return component
- `w2 = 0.7` - Volatility ratio component

**Two Scenarios:**

1. **ShortPanicScore** (detects sudden spikes)
   - `vol_short` = 5-minute rolling volatility
   - `vol_long` = 60-minute rolling volatility
   - **Interpretation:** Ratio > 1 = short-term volatility exceeds average = panic/stress

2. **LongPanicScore** (baseline stress level)
   - `vol_short` = 60-minute rolling volatility
   - `vol_long` = 60-minute rolling volatility
   - **Interpretation:** Pure volatility measure without ratio effect

**Example:**
- Stable market: short_vol=0.02, long_vol=0.02 → score ≈ 0.70 (ratio=1.0)
- Panic market: short_vol=0.10, long_vol=0.02 → score ≈ 3.50 (ratio=5.0)

## Running the Application

### Docker (Recommended)

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f PricePredictor.app

# Stop
docker-compose down
```

Services will start:
- **PostgreSQL** on `localhost:5432`
- **App (gRPC)** on `localhost:50051`
- Migrations run automatically on startup

### Local Development

```bash
# Prerequisites
# - PostgreSQL running on localhost:5432
# - Database: pricepredictor
# - User: postgres / Password: postgres

# Restore packages
dotnet restore

# Build
dotnet build

# Run migrations (auto-runs at startup, but can also run manually)
dotnet ef database update -p PricePredictor.Infrastructure

# Run
dotnet run --project PricePredictor.App
```

## Output Logging

### Every Minute (Real-time)

```
Symbol: GC=F, Timestamp: 2026-03-03 14:45:00, Close: 1925.45, LogReturn: 0.001234, 
Vol5: 0.025, Vol15: 0.023, Vol60: 0.021, ShortPanic: 0.823, LongPanic: 0.715
```

### Every 10 Minutes (Backup)

```
=== VOLATILITY BACKUP LOG (Last 10 minutes) - 2026-03-03 14:50:00 ===
--- GC=F (Gold) ---
  TS: 2026-03-03 14:40:00, O: 1925.30, H: 1926.60, L: 1924.10, C: 1925.45, V: 1500000, 
  Ret: 0.001234, V5: 0.025, V15: 0.023, V60: 0.021, SP: 0.823, LP: 0.715
  TS: 2026-03-03 14:41:00, O: 1925.45, H: 1926.70, L: 1925.35, C: 1925.50, V: 1400000, 
  Ret: 0.000261, V5: 0.024, V15: 0.023, V60: 0.021, SP: 0.792, LP: 0.715
--- SI=F (Silver) ---
  ...
=== END BACKUP LOG ===
```

## Unit Tests

Run all 22 tests:

```bash
dotnet test PricePredictor.Tests
```

**Test Coverage:**

| Category | Tests | Coverage |
|----------|-------|----------|
| Logarithmic Returns | 4 | Edge cases, zero prices, up/down |
| Rolling Volatility | 6 | Empty, single value, constant, variable |
| Standard Deviation | 3 | Valid, constant, empty |
| Panic Score | 7 | Stable market, panic, zero division, negatives, custom weights, 5/60 windows |
| Integration | 2 | Full pipeline with synthetic data |
| **Total** | **22** | **100% pass** |

## Volatility Calculations

### Logarithmic Return
```csharp
return = ln(P_t / P_t-1)
```
- Professional traders prefer log returns (log-normal distribution)
- Handles edge case: P_t-1 = 0 returns 0

### Rolling Volatility (Standard Deviation)
```csharp
volatility = sqrt( Σ(r_i - mean)² / n )
```
- Measures price variability in a time window
- 5-min: uses 5 most recent 1-min returns
- 60-min: uses all 60 most recent returns

### Normalized Panic Score
Trades off:
- **Immediate shock** (price movement magnitude)
- **Market stress** (volatility spike vs baseline)

This metric is superior to simple volatility because:
- Same volatility = different panic if baseline is different
- Ratio > 1 = market has changed character
- Weights (0.3/0.7) can be tuned for trading strategy

## Dependencies

### Infrastructure
- `Microsoft.EntityFrameworkCore` (10.0.0)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (10.0.0)

### App
- `Microsoft.Extensions.Hosting` (10.0.3)
- `Microsoft.Extensions.Http` (10.0.3)
- `Grpc.AspNetCore` (2.76.0)
- `System.Net.Http.Json` (.NET 10 built-in)
- `System.Text.Json` (.NET 10 built-in)

### Tests
- `xunit` (2.9.3)
- `FluentAssertions` (6.12.1)
- `xunit.runner.visualstudio` (2.8.0)

## Key Design Decisions

### ✅ Typed HTTP Client
- Single `YahooFinanceClient` registered with DI
- Clean separation: HTTP concerns isolated
- Testable: can mock for unit tests

### ✅ Repository Pattern
- `IVolatilityRepository` abstracts EF Core
- Easy to swap: SQL Server, SQLite, etc.
- `SaveChanges()` called per insert for immediate persistence

### ✅ In-Memory Return Buffers
- `Dictionary<string, List<double>> _returnsBuffer` per symbol
- Efficient rolling window: `TakeLast(window)`
- Survives service restarts (last 10-60 min lost, but re-fetched)

### ✅ Separate Tables per Commodity
- Query one symbol without joins
- Partition-ready for large datasets
- No NULL commodity columns

### ✅ EF Core Migrations
- Run automatically via `dbContext.Database.Migrate()` at startup
- No manual script execution needed
- Version-controlled schema

## Metrics & Monitoring

Track these to understand market conditions:

| Metric | Interpretation |
|--------|-----------------|
| ShortPanicScore > LongPanicScore | Sudden spike = trading opportunity or risk |
| Vol5 > Vol15 > Vol60 | Increasing volatility = deteriorating conditions |
| LogarithmicReturn spikes | Large single-minute move = liquidity event |
| RollingVol60 trend | Volatility regime change (stable→turbulent) |

## Troubleshooting

### "No candles received for {Symbol}"
- Market may be closed (check trading hours)
- Yahoo Finance may have returned sparse data
- Check internet connectivity

### "Cannot connect to PostgreSQL"
- Verify `ConnectionStrings:DefaultConnection`
- Run `docker-compose up postgres -d` to start container
- Check: `psql -h localhost -U postgres -d pricepredictor`

### Migration Failed
- Drop database: `dropdb -h localhost -U postgres pricepredictor`
- Let migrations re-create schema

### Duplicate Key Error
- Data fetched twice in same minute
- Safe: EF inserts new row (no conflict on timestamp+symbol)
- Deduplicate in cleanup routine if needed

## Future Enhancements

- [ ] Multiple data sources (Alpha Vantage, IEX Cloud)
- [ ] Alerting when panic score exceeds threshold
- [ ] WebSocket for real-time updates (eliminate 1-min delay)
- [ ] Machine learning on panic scores → prediction model
- [ ] HTTP API to query historical volatility
- [ ] Time-zone aware scheduling (skip market holidays)
- [ ] Multi-symbol batching (reduce API calls)

## License

This implementation is provided as-is for educational and trading purposes.

## References

- Yahoo Finance JSON API: `https://query1.finance.yahoo.com/v8/finance/chart/`
- Volatility Metrics: https://en.wikipedia.org/wiki/Volatility_(finance)
- Logarithmic Returns: https://en.wikipedia.org/wiki/Rate_of_return#Logarithmic_return


