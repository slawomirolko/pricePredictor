# Integration Tests for PricePredictor

This project contains integration tests that verify the application correctly stores volatility data in PostgreSQL every 1 minute.

## Test Suites

### 1. LiveSystemDataPersistenceTests
Tests that connect to an already-running Docker Compose stack. These are the simplest and fastest tests.

**Prerequisites:**
- Docker Compose must be running: `docker compose up -d`
- PostgreSQL must be accessible on `localhost:5432`

**Run these tests:**
```powershell
dotnet test --filter "FullyQualifiedName~LiveSystemDataPersistenceTests"
```

**Tests included:**
- `LiveSystem_ShouldHaveAllVolatilityTables` - Verifies all 4 volatility tables exist
- `LiveSystem_ShouldStoreDataEveryMinute` - Waits 70 seconds and verifies data increased
- `LiveSystem_ShouldHaveValidDataStructure` - Validates column structure in each table
- `LiveSystem_ShouldAccumulateDataOverTime` - Takes 3 measurements over 3 minutes
- `LiveSystem_ShouldHaveRecentData` - Verifies data is fresh (< 5 minutes old)

### 2. YahooFinanceDataPersistenceTests
Full integration tests using TestContainers. These spin up isolated PostgreSQL and application containers.

**Prerequisites:**
- Docker daemon must be running
- Application image must be built: `docker build -t pricepredicator.app:latest -f PricePredicator.App/Dockerfile .`

**Run these tests:**
```powershell
dotnet test --filter "FullyQualifiedName~YahooFinanceDataPersistenceTests"
```

**Tests included:**
- `ShouldStoreVolatilityDataInDatabase_AfterOneMinute` - Full 70-second test with isolated containers
- `ShouldHaveCorrectSchemaAndTables` - Schema validation
- `ShouldStoreDataWithCorrectStructure` - Row structure validation
- `ShouldPersistDataAfterMultipleCycles` - 3-minute test verifying progressive accumulation

## Quick Start

1. Start the application:
```powershell
docker compose up -d
```

2. Wait 10 seconds for initial data load

3. Run the fast tests:
```powershell
cd PricePredicator.Integration.Tests
dotnet test --filter "LiveSystem_ShouldHaveAllVolatilityTables"
```

4. Run the full suite (takes ~3-4 minutes):
```powershell
dotnet test
```

## Manual Verification

Check current row counts:
```powershell
docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -F "," -c "select relname, n_live_tup from pg_stat_user_tables where schemaname='public' and relname in ('Gold','Silver','NaturalGas','Oil') order by relname;"
```

View latest records:
```powershell
docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -c "SELECT \"Timestamp\", \"Close\", \"LogarithmicReturn\", \"RollingVol5\" FROM \"Gold\" ORDER BY \"CreatedAtUtc\" DESC LIMIT 5;"
```

## Configuration Override

The tests override settings using environment variables to ensure 1-minute intervals:
- `YahooFinance__Interval=1m`
- `YahooFinance__Range=1d`
- `YahooFinance__Symbols__0=GC=F` (Gold Futures)
- `YahooFinance__Symbols__1=SI=F` (Silver Futures)
- `YahooFinance__Symbols__2=NG=F` (Natural Gas Futures)
- `YahooFinance__Symbols__3=CL=F` (Oil Futures)

## Expected Results

After 1 minute of operation, each volatility table should have:
- At least 1 new row per minute per symbol
- Valid price data (Close > 0)
- Calculated metrics (LogarithmicReturn, RollingVol5/15/60, Panic Scores)
- Recent timestamps (CreatedAtUtc within last 5 minutes)

## Troubleshooting

**Tests fail with connection timeout:**
- Ensure Docker Compose is running
- Check PostgreSQL is accessible: `docker ps`

**No data growth observed:**
- Check application logs: `docker logs pricepredicator.app --tail 50`
- Verify Yahoo Finance API is accessible
- Markets might be closed (after-hours testing may show slower updates)

**TestContainers tests fail:**
- Rebuild the app image: `docker compose build`
- Check Docker has sufficient resources (memory/CPU)
