# Quick Start Guide - Yahoo Finance 1-Minute Volatility

Get started in 5 minutes.

## Prerequisites

- .NET 10 SDK
- Docker & Docker Compose (or PostgreSQL 12+)
- Git

## Step 1: Clone and Navigate

```bash
cd C:\Users\sawek\Documents\Git\DotNet\PricePredictor
```

## Step 2: Start Postgres (Choose One)

### Option A: Docker (Recommended)
```bash
docker-compose up postgres -d
# Wait 10 seconds for healthcheck to pass
```

### Option B: Local PostgreSQL
```bash
# Create database
createdb -h localhost -U postgres pricepredictor

# Verify
psql -h localhost -U postgres -d pricepredictor -c "SELECT version();"
```

## Step 3: Verify Configuration

Edit `PricePredicator.App/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=pricepredictor;User Id=postgres;Password=postgres;"
  },
  "YahooFinance": {
    "Symbols": [ "GC=F", "SI=F", "NG=F", "CL=F" ],
    "Interval": "1m",
    "Range": "1d",
    "VolatilityBackupMinutes": 10
  }
}
```

## Step 4: Run Application

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run (migrations run automatically)
dotnet run --project PricePredicator.App

# Expected output:
# info: PricePredicator.App.Finance.YahooFinanceBackgroundService[0]
#       Yahoo Finance Background Service started.
# info: PricePredicator.App.Finance.YahooFinanceBackgroundService[0]
#       Fetching intraday data at 2026-03-03 14:30:00...
```

## Step 5: View Data

Every minute, you'll see:
```
Symbol: GC=F, Timestamp: 2026-03-03 14:31:00, Close: 1925.45, LogReturn: 0.001234,
Vol5: 0.025, Vol15: 0.023, Vol60: 0.021, ShortPanic: 0.823, LongPanic: 0.715
```

Every 10 minutes (configurable via `VolatilityBackupMinutes`), full backup log with all records.

## Step 6: Query Database

```bash
# Install psql if needed, then:
psql -h localhost -U postgres -d pricepredictor

# Query latest Gold prices (last 5 minutes)
SELECT 
  timestamp, 
  close, 
  round(logarithmic_return::numeric, 6) as ret,
  round(rolling_vol_5::numeric, 6) as vol5,
  round(short_panic_score::numeric, 4) as short_panic
FROM "Gold"
WHERE timestamp > now() - interval '5 minutes'
ORDER BY timestamp DESC;
```

## Step 7: Run Tests

```bash
dotnet test PricePredicator.Tests

# Expected: 22 tests passed
```

## Troubleshooting

### Service won't start
```bash
# Check Postgres is running
docker-compose ps

# Check logs
docker-compose logs postgres

# Restart all services
docker-compose down
docker-compose up -d
```

### No data appearing
- Market may be closed (9:30 AM - 4:00 PM EST weekdays)
- Check internet connection
- Verify symbols in settings

### Database errors
```bash
# Drop and recreate
docker-compose down -v
docker-compose up postgres -d
# App will auto-migrate on next run
```

## Architecture at a Glance

```
┌─────────────────────────────────────────────────────────┐
│  YahooFinanceBackgroundService (runs every 1 minute)   │
└────────────────────┬────────────────────────────────────┘
                     │
     ┌───────────────┴───────────────┐
     │                               │
     ▼                               ▼
┌──────────────┐           ┌──────────────────┐
│ YahooFinance │           │ IndicatorCalculator
│   Client     │           │ - log returns
│ (HTTP)       │           │ - rolling vol
└──────────────┘           │ - panic scores
     │                     └──────────────────┘
     └──────────────┬──────────────┘
                    │
                    ▼
        ┌───────────────────────┐
        │ VolatilityRepository  │
        │ (EF Core)             │
        └───────────────┬───────┘
                        │
                        ▼
                  ┌──────────────┐
                  │  PostgreSQL  │
                  │ (4 tables)   │
                  └──────────────┘
```

## Configuration Options

| Setting | Default | Purpose |
|---------|---------|---------|
| `Symbols` | `["GC=F", "SI=F", "NG=F", "CL=F"]` | Which commodities to track |
| `Interval` | `1m` | 1-minute candles (do not change) |
| `Range` | `1d` | Fetch last 1 day of history (1d, 5d, 1mo) |
| `VolatilityBackupMinutes` | `10` | Log all data every N minutes |
| `VolatilityWindows` | `[5, 15, 60]` | Calculate rolling vol for these windows |

## Panic Score Interpretation

| Score | Market Condition | Action |
|-------|------------------|--------|
| < 0.3 | Extremely calm | Low activity, wide spreads |
| 0.3 - 0.7 | Normal | Regular trading |
| 0.7 - 1.5 | Elevated stress | Volatility spike, caution |
| > 1.5 | Panic/crisis | Major news, wide swings |

**How to use:**
- Monitor `ShortPanicScore` for immediate signals
- Use `LongPanicScore` as baseline
- Alert when short > long by 0.5+ = market turning point

## Next Steps

1. Integrate with your trading system
2. Add alerting (Slack, email, SMS)
3. Build ML model on panic scores
4. Add more symbols or data sources
5. Create dashboard (Grafana, Power BI)

See `YAHOO_FINANCE_README.md` for detailed documentation.
