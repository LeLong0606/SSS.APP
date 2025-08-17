# ?? GitHub Upload Commands

# Run these commands in your project root directory (D:\ThS\SSS.APP\)

## Step 1: Initialize Git repository
git init

## Step 2: Add all files
git add .

## Step 3: Make initial commit
git commit -m "Initial commit: Employee Management System with JWT Auth

- Complete .NET 8 Web API with JWT authentication
- 4-level role system: Administrator > Director > TeamLeader > Employee  
- Employee and Department CRUD operations
- Simple role-based authorization
- Entity Framework with SQL Server
- Swagger UI with JWT integration
- Angular frontend placeholder
- Comprehensive documentation and setup guide"

## Step 4: Create GitHub repository
# Go to https://github.com/new and create a new repository with name:
# employee-management-system
# Make it PUBLIC
# Don't initialize with README (we already have one)

## Step 5: Add remote origin (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/employee-management-system.git

## Step 6: Set default branch to main (modern GitHub standard)
git branch -M main

## Step 7: Push to GitHub
git push -u origin main

## Alternative repository names if you prefer:
# - emp-management-jwt-auth
# - staff-management-system
# - employee-dept-manager
# - workforce-management-api