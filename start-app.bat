@echo off
echo ?? Starting SSS Employee Management System...
echo ==========================================

echo ?? Starting Backend (SSS.BE) on https://localhost:5001...
start "SSS Backend" cmd /k "cd SSS.BE && dotnet run --urls https://localhost:5001;http://localhost:5000"

timeout /t 3 /nobreak >nul

echo ?? Starting Frontend (SSS.FE) on http://localhost:50503...
start "SSS Frontend" cmd /k "cd SSS.FE && npm start"

echo ? Both applications are starting...
echo ?? Backend API: https://localhost:5001/swagger
echo ?? Frontend App: http://localhost:50503
echo ==========================================