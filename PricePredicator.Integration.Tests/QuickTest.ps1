# Simple Manual Test for Data Persistence
# Run this after docker compose up -d

Write-Host "Checking baseline counts..." -ForegroundColor Cyan
$gold1 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM \"Volatility_Gold\"'
$silver1 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM \"Volatility_Silver\"'
$gas1 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM \"Volatility_NaturalGas\"'
$oil1 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM \"Volatility_Oil\"'

Write-Host "Volatility_Gold: $gold1"
Write-Host "Volatility_Silver: $silver1"
Write-Host "Volatility_NaturalGas: $gas1"
Write-Host "Volatility_Oil: $oil1"

Write-Host ""
Write-Host "Waiting 70 seconds..." -ForegroundColor Yellow
Start-Sleep -Seconds 70

Write-Host ""
Write-Host "Checking new counts..." -ForegroundColor Cyan
$gold2 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM \"Volatility_Gold\"'
$silver2 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM \"Volatility_Silver\"'
$gas2 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM \"Volatility_NaturalGas\"'
$oil2 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM \"Volatility_Oil\"'

Write-Host "Volatility_Gold: $gold2 (was $gold1)"
Write-Host "Volatility_Silver: $silver2 (was $silver1)"
Write-Host "Volatility_NaturalGas: $gas2 (was $gas1)"
Write-Host "Volatility_Oil: $oil2 (was $oil1)"

Write-Host ""
if ([int]$gold2 -gt [int]$gold1 -or [int]$silver2 -gt [int]$silver1 -or [int]$gas2 -gt [int]$gas1 -or [int]$oil2 -gt [int]$oil1) {
    Write-Host "SUCCESS: Data is growing!" -ForegroundColor Green
} else {
    Write-Host "WARNING: No growth detected" -ForegroundColor Yellow
}



