# SSS Employee Management System - Startup Script
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   ?? SSS Employee Management System - Startup" -ForegroundColor Yellow  
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Starting Backend (SSS.BE) on https://localhost:5001..." -ForegroundColor Green
Write-Host "Starting Frontend (SSS.FE) on http://localhost:50503..." -ForegroundColor Green
Write-Host ""
Write-Host "? Backend API: https://localhost:5001/api" -ForegroundColor White
Write-Host "? Frontend UI: http://localhost:50503" -ForegroundColor White
Write-Host "? Swagger UI: https://localhost:5001/swagger" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C in either window to stop the servers" -ForegroundColor Yellow
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Start Backend in new PowerShell window
$backendScript = @"
Write-Host 'Starting SSS Backend...' -ForegroundColor Green
Set-Location 'SSS.BE'
dotnet run --urls=https://localhost:5001
"@

Start-Process PowerShell -ArgumentList "-NoExit", "-Command", $backendScript

# Wait a bit for backend to start
Write-Host "? Waiting for backend to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Start Frontend in new PowerShell window
$frontendScript = @"
Write-Host 'Starting SSS Frontend...' -ForegroundColor Green  
Set-Location 'SSS.FE'
npm start
"@

Start-Process PowerShell -ArgumentList "-NoExit", "-Command", $frontendScript

Write-Host ""
Write-Host "? Both servers are starting..." -ForegroundColor Green
Write-Host "?? Open browser: http://localhost:50503" -ForegroundColor Cyan
Write-Host "?? API Documentation: https://localhost:5001/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to exit this launcher..." -ForegroundColor Yellow
$host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | Out-Null