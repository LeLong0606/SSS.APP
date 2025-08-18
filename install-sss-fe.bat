@echo off
REM SSS.FE Installation Script for Windows

echo ?? Starting SSS.FE Installation...
echo ==================================================

REM Navigate to frontend directory
cd SSS.FE

echo ?? Cleaning previous installations...
if exist "node_modules" (
    echo Removing node_modules...
    rmdir /s /q node_modules
)

if exist "package-lock.json" (
    echo Removing package-lock.json...
    del package-lock.json
)

echo ?? Installing npm packages...
call npm install

if %ERRORLEVEL% EQU 0 (
    echo ? npm install completed successfully!
    
    echo ?? Adding Angular Material...
    call ng add @angular/material --defaults --skip-confirmation
    
    echo ?? Installing additional UI packages...
    call npm install primeng primeicons
    
    echo ?? Building project to verify installation...
    call npm run build
    
    if %ERRORLEVEL% EQU 0 (
        echo ? Build successful! Installation completed.
        echo ?? Ready to start development with: npm start
    ) else (
        echo ? Build failed. Please check the error messages above.
    )
) else (
    echo ? npm install failed. Please check your internet connection and try again.
)

echo ==================================================
echo Installation script completed.
pause