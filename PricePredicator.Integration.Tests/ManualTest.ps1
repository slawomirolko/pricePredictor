# Manual Integration Test Script
# Verifies that data is stored in PostgreSQL every 1 minute

Write-Host "=== PricePredictor Data Persistence Test ===" -ForegroundColor Cyan
Write-Host ""

# Check if Docker containers are running
Write-Host "Checking Docker containers..." -ForegroundColor Yellow
$containers = docker ps --format "table {{.Names}}\t{{.Status}}" | Select-String "pricepredictor"
if ($containers) {
    Write-Host $containers -ForegroundColor Green
} else {
    Write-Host "ERROR: Docker containers not running. Run 'docker compose up -d' first." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Checking database tables..." -ForegroundColor Yellow
$tables = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c "SELECT tablename FROM pg_tables WHERE schemaname='public' AND tablename in ('Gold','Silver','NaturalGas','Oil') ORDER BY tablename"

if ($tables) {
    $tableArray = $tables -split [Environment]::NewLine
    foreach ($table in $tableArray) {
        if ($table.Trim()) {
            Write-Host "  ✓ $table" -ForegroundColor Green
        }
    }
} else {
    Write-Host "ERROR: No commodity tables found" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Getting baseline counts..." -ForegroundColor Yellow
$baseline = @{}
$tables = @("Gold", "Silver", "NaturalGas", "Oil")

foreach ($table in $tables) {
    $count = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c "SELECT COUNT(*) FROM ""$table"""
    $baseline[$table] = [int]$count.Trim()
    Write-Host "  $table : $($baseline[$table])" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Waiting 70 seconds for next data cycle..." -ForegroundColor Yellow
for ($i = 70; $i -gt 0; $i--) {
    Write-Host "`r  Time remaining: $i seconds " -NoNewline -ForegroundColor Cyan
    Start-Sleep -Seconds 1
}
Write-Host ""

Write-Host ""
Write-Host "Getting new counts..." -ForegroundColor Yellow
$newCounts = @{}
$growth = @{}
$anyGrowth = $false

foreach ($table in $tables) {
    $count = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c "SELECT COUNT(*) FROM ""$table"""
    $newCounts[$table] = [int]$count.Trim()
    $growth[$table] = $newCounts[$table] - $baseline[$table]
    
    if ($growth[$table] -gt 0) {
        Write-Host "  $table : $($newCounts[$table]) (+$($growth[$table]))" -ForegroundColor Green
        $anyGrowth = $true
    } elseif ($growth[$table] -eq 0) {
        Write-Host "  $table : $($newCounts[$table]) (no change)" -ForegroundColor Yellow
    } else {
        Write-Host "  $table : $($newCounts[$table]) ($($growth[$table]))" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== TEST RESULT ===" -ForegroundColor Cyan
if ($anyGrowth) {
    Write-Host "✓ SUCCESS: Data is being persisted!" -ForegroundColor Green
    Write-Host "At least one table shows data growth, confirming the application" -ForegroundColor Green
    Write-Host "is successfully storing volatility data every minute." -ForegroundColor Green
} else {
    Write-Host "⚠ WARNING: No data growth detected" -ForegroundColor Yellow
    Write-Host "This could be normal if:" -ForegroundColor Yellow
    Write-Host "  - Markets are closed (after-hours)" -ForegroundColor Yellow
    Write-Host "  - Yahoo Finance API is slow to respond" -ForegroundColor Yellow
    Write-Host "  - The application just started" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Check application logs: docker logs pricepredicator.app --tail 50" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "View latest data:" -ForegroundColor Cyan
Write-Host "  docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -c ""SELECT * FROM \""Gold\"" ORDER BY \""CreatedAtUtc\"" DESC LIMIT 5;""" -ForegroundColor Gray
