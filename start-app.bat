@echo off
echo ================================================
echo   ?? SSS Employee Management System - Startup
echo ================================================
echo.
echo Starting Backend (SSS.BE) on https://localhost:5001...
echo Starting Frontend (SSS.FE) on http://localhost:50503...
echo.
echo ? Backend API: https://localhost:5001/api
echo ? Frontend UI: http://localhost:50503  
echo ? Swagger UI: https://localhost:5001/swagger
echo.
echo Press Ctrl+C in either window to stop the servers
echo ================================================
echo.

REM Start Backend in new window
start "SSS Backend" cmd /k "cd /d SSS.BE && echo Starting Backend... && dotnet run --urls=https://localhost:5001"

REM Wait a bit for backend to start
timeout /t 5 /nobreak >nul

REM Start Frontend in new window  
start "SSS Frontend" cmd /k "cd /d SSS.FE && echo Starting Frontend... && npm start"

echo.
echo ? Both servers are starting...
echo ?? Open browser: http://localhost:50503
echo ?? API Documentation: https://localhost:5001/swagger
echo.
echo Press any key to exit this launcher...
pause >nul