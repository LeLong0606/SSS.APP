#!/bin/bash
# SSS.FE Installation Script

echo "?? Starting SSS.FE Installation..."
echo "=================================================="

# Navigate to frontend directory
cd SSS.FE

echo "?? Cleaning previous installations..."
if [ -d "node_modules" ]; then
    echo "Removing node_modules..."
    rm -rf node_modules
fi

if [ -f "package-lock.json" ]; then
    echo "Removing package-lock.json..."
    rm -f package-lock.json
fi

echo "?? Installing npm packages..."
npm install

if [ $? -eq 0 ]; then
    echo "? npm install completed successfully!"
    
    echo "?? Adding Angular Material..."
    ng add @angular/material --defaults
    
    echo "?? Adding Angular CDK..."
    npm install @angular/cdk
    
    echo "?? Installing additional UI packages..."
    npm install primeng primeicons
    
    echo "?? Building project to verify installation..."
    npm run build
    
    if [ $? -eq 0 ]; then
        echo "? Build successful! Installation completed."
        echo "?? Ready to start development with: npm start"
    else
        echo "? Build failed. Please check the error messages above."
    fi
else
    echo "? npm install failed. Please check your internet connection and try again."
fi

echo "=================================================="
echo "Installation script completed."