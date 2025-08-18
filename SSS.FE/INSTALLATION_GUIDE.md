# 🚀 SSS.FE Installation Guide

## 📋 Prerequisites
- **Node.js**: Version 18.19.0 or higher
- **npm**: Version 10.2.0 or higher  
- **Angular CLI**: Version 17.3.8 or higher

## 🔧 Quick Installation (Recommended)

### **Windows Users:**
```bash
# Run the installation batch script
./install-sss-fe.bat
```

### **Linux/Mac Users:**
```bash
# Make script executable and run
chmod +x install-sss-fe.sh
./install-sss-fe.sh
```

## 📦 Manual Installation

### **Step 1: Clean Installation**
```bash
cd SSS.FE

# Remove old dependencies
rm -rf node_modules
rm package-lock.json

# Install npm packages
npm install
```

### **Step 2: Add Angular Material**
```bash
ng add @angular/material --theme=indigo-pink --typography=true --animations=true
```

### **Step 3: Install UI Libraries**
```bash
# Install PrimeNG for advanced components
npm install primeng primeicons

# Install additional packages
npm install ngx-spinner ngx-pagination
```

### **Step 4: Verify Installation**
```bash
# Build the project
npm run build

# If successful, start development server
npm start
```

## 🔧 Package Overview

### **Core Angular Packages**
- `@angular/core@^17.3.12` - Angular framework
- `@angular/material@^17.3.10` - Material Design components
- `@angular/cdk@^17.3.10` - Component Dev Kit
- `@angular/animations@^17.3.12` - Animation system

### **UI & Styling**
- `bootstrap@^5.3.3` - CSS framework
- `primeng` + `primeicons` - Advanced UI components
- `animate.css@^4.1.1` - CSS animations
- `aos@^2.3.4` - Animate on scroll

### **Charts & Visualization**
- `chart.js@^4.4.6` - Chart library
- `ng2-charts@^6.0.1` - Angular Chart wrapper

### **Utilities**
- `lodash@^4.17.21` - Utility functions
- `moment@^2.30.1` - Date manipulation
- `sweetalert2@^11.14.5` - Beautiful alerts
- `socket.io-client@^4.8.1` - Real-time communication

## 🚀 Development Commands

```bash
# Start development server
npm start                    # Runs on http://127.0.0.1:50503

# Build for production
npm run build:prod

# Run tests
npm test

# Lint code
npm run lint

# Serve production build
npm run serve:prod
```

## 🎯 Project Structure After Installation

```
SSS.FE/
├── src/
│   ├── app/
│   │   ├── core/           # Services, guards, interceptors
│   │   ├── shared/         # Reusable components
│   │   ├── features/       # Feature modules (lazy-loaded)
│   │   ├── layouts/        # Layout components
│   │   └── app.module.ts   # Root module
│   ├── assets/             # Static files
│   ├── environments/       # Environment configs
│   └── styles/             # Global styles
├── node_modules/           # Installed packages
└── dist/                   # Built application
```

## 🔍 Troubleshooting

### **Common Issues:**

1. **Node Version Mismatch**
   ```bash
   node --version  # Should be 18.19.0+
   npm --version   # Should be 10.2.0+
   ```

2. **Angular CLI Not Found**
   ```bash
   npm install -g @angular/cli@17.3.8
   ```

3. **Port Already In Use**
   ```bash
   # Kill process on port 50503
   netstat -ano | findstr :50503
   taskkill /PID <PID> /F
   ```

4. **Memory Issues During Build**
   ```bash
   # Increase Node memory
   set NODE_OPTIONS=--max_old_space_size=8192
   npm run build
   ```

## ✅ Post-Installation Checklist

- [ ] `npm install` completed without errors
- [ ] `ng add @angular/material` successful
- [ ] `npm run build` successful
- [ ] `npm start` runs without errors
- [ ] Application loads at `http://127.0.0.1:50503`
- [ ] All dependencies installed correctly

## 🎉 Ready for Development!

Once installation is complete, you can:

1. **Start Development**: `npm start`
2. **Open Browser**: `http://127.0.0.1:50503`
3. **Begin Coding**: All dependencies are ready!

## 📞 Need Help?

If you encounter issues:
1. Check the error messages carefully
2. Verify Node.js and npm versions
3. Try clearing cache: `npm cache clean --force`
4. Delete `node_modules` and run `npm install` again

---

**Installation Status:** ✅ **READY FOR SETUP**  
**Next Step:** Run installation script or follow manual steps above.
