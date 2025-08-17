# ?? Start Full Stack SSS Application
# Run this script to start both Backend and Frontend

Write-Host "?? Starting SSS Employee Management System..." -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan

# Start Backend (SSS.BE)
Write-Host "?? Starting Backend (SSS.BE) on https://localhost:5001..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '.\SSS.BE\'; dotnet run --urls 'https://localhost:5001;http://localhost:5000'"

# Wait for backend to start
Start-Sleep -Seconds 3

# Start Frontend (SSS.FE)
Write-Host "?? Starting Frontend (SSS.FE) on http://localhost:50503..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '.\SSS.FE\'; npm start"

Write-Host "? Both applications are starting..." -ForegroundColor Green
Write-Host "?? Backend API: https://localhost:5001/swagger" -ForegroundColor Cyan
Write-Host "?? Frontend App: http://localhost:50503" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Green