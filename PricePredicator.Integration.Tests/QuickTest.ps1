# Simple Manual Test for Data Persistence
# Run this after docker compose up -d

Write-Host "Checking baseline counts..." -ForegroundColor Cyan
$gold1 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM "Gold"'
$silver1 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM "Silver"'
$gas1 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM "NaturalGas"'
$oil1 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM "Oil"'

Write-Host "Gold: $gold1"
Write-Host "Silver: $silver1"
Write-Host "NaturalGas: $gas1"
Write-Host "Oil: $oil1"

Write-Host ""
Write-Host "Waiting 70 seconds..." -ForegroundColor Yellow
Start-Sleep -Seconds 70

Write-Host ""
Write-Host "Checking new counts..." -ForegroundColor Cyan
$gold2 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM "Gold"'
$silver2 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM "Silver"'
$gas2 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM "NaturalGas"'
$oil2 = docker exec pricepredictor.postgres psql -U postgres -d pricepredictor -At -c 'SELECT COUNT(*) FROM "Oil"'

Write-Host "Gold: $gold2 (was $gold1)"
Write-Host "Silver: $silver2 (was $silver1)"
Write-Host "NaturalGas: $gas2 (was $gas1)"
Write-Host "Oil: $oil2 (was $oil1)"

Write-Host ""
if ([int]$gold2 -gt [int]$gold1 -or [int]$silver2 -gt [int]$silver1 -or [int]$gas2 -gt [int]$gas1 -or [int]$oil2 -gt [int]$oil1) {
    Write-Host "SUCCESS: Data is growing!" -ForegroundColor Green
} else {
    Write-Host "WARNING: No growth detected" -ForegroundColor Yellow
}
