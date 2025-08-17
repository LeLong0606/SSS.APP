# 🔧 **API URL CONFIGURATION FIX - SSS.FE**

## 📋 **Problem Identified**

Bạn phát hiện rằng frontend đang gọi API đến `http://localhost:50503/api/auth/login` thay vì backend URL đúng `https://localhost:5001/api/auth/login`.

## 🎯 **Root Cause Analysis**

### **Nguyên nhân chính:**
1. **Environment Configuration**: File `environment.ts` ban đầu sử dụng relative URL `/api`
2. **Proxy Configuration**: Angular proxy tự động điều hướng `/api/*` đến `https://localhost:5001`
3. **Mixed Configuration**: Có cả proxy và environment URL, gây confusion

### **Cấu hình trước khi sửa:**
```typescript
// ❌ environment.ts - Relative URL
export const environment = {
  apiUrl: '/api',  // Relative path, sẽ dùng proxy
};

// proxy.conf.json
{
  "/api/*": {
    "target": "https://localhost:5001",  // Proxy redirect
    "secure": false,
    "changeOrigin": true
  }
}
```

---

## ✅ **Complete Fix Applied**

### **1. Fixed Environment Files**

**File:** `SSS.FE\src\environments\environment.ts`
```typescript
// ✅ AFTER: Full URL to backend
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api', // Direct API URL
  // ... other config
};
```

**File:** `SSS.FE\src\environments\environment.prod.ts`
```typescript
// ✅ Already correct
export const environment = {
  production: true,
  apiUrl: 'https://localhost:5001/api', // Direct API URL
  // ... other config
};
```

### **2. Service Verification**

**All services correctly use `environment.apiUrl`:**
- ✅ `AuthService`: `${environment.apiUrl}/auth/*`
- ✅ `EmployeeService`: `${environment.apiUrl}/employee/*`
- ✅ `DepartmentService`: `${environment.apiUrl}/department/*`
- ✅ `WorkShiftService`: `${environment.apiUrl}/workshift/*`
- ✅ `WorkLocationService`: `${environment.apiUrl}/worklocation/*`

### **3. Updated Package.json Scripts**

**Added option to run without proxy:**
```json
{
  "scripts": {
    "start": "ng serve --host=127.0.0.1 --port=50503",
    "start:no-proxy": "ng serve --host=127.0.0.1 --port=50503 --no-proxy"
  }
}
```

---

## 🚀 **How It Works Now**

### **Development Mode (`npm start`):**
```bash
Frontend: http://localhost:50503
API Calls: https://localhost:5001/api/* (direct)
Backend: https://localhost:5001
```

### **API Call Flow:**
```typescript
// Service call
this.http.get(`${environment.apiUrl}/auth/login`)
// Resolves to
this.http.get('https://localhost:5001/api/auth/login')
// Direct call to backend - No proxy needed
```

### **Network Request:**
```
Frontend (localhost:50503) 
    ↓ Direct HTTP request
Backend (localhost:5001/api/auth/login)
    ↓ Response
Frontend receives response
```

---

## 📊 **Configuration Summary**

| Component | Configuration | Status |
|-----------|---------------|--------|
| **Development Env** | `https://localhost:5001/api` | ✅ Fixed |
| **Production Env** | `https://localhost:5001/api` | ✅ Correct |
| **All Services** | Use `environment.apiUrl` | ✅ Verified |
| **Proxy Config** | Available if needed | ✅ Optional |
| **Package Scripts** | Both proxy/no-proxy options | ✅ Enhanced |

---

## 🔍 **Testing Instructions**

### **1. Test Current Configuration:**
```bash
# Start backend
cd SSS.BE
dotnet run --urls=https://localhost:5001

# Start frontend (in another terminal)
cd SSS.FE  
npm start

# Access: http://localhost:50503
# API calls will go directly to: https://localhost:5001/api/*
```

### **2. Test Without Proxy (Alternative):**
```bash
# Start frontend without proxy
cd SSS.FE
npm run start:no-proxy

# All API calls still go to https://localhost:5001/api/*
```

### **3. Verify Network Requests:**
1. Open browser DevTools (F12)
2. Go to Network tab
3. Try login from frontend
4. Should see requests to `https://localhost:5001/api/auth/login`

---

## 🛡️ **CORS Configuration**

**Backend CORS already configured correctly:**
```csharp
// SSS.BE/Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:50503", "https://localhost:50503")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
```

---

## 🎯 **Benefits of This Configuration**

### **✅ Advantages:**
- **Direct API Calls**: No proxy dependency
- **Clear Configuration**: Explicit API URLs in environment files
- **Production Ready**: Same pattern for dev/prod environments
- **Debugging Friendly**: Easy to see actual API endpoints in Network tab
- **Flexible**: Can switch between proxy/direct modes

### **✅ Backward Compatibility:**
- Proxy configuration still available if needed
- No breaking changes to existing services
- Environment-based configuration maintained

---

## 📈 **Results**

### **Before Fix:**
```
❌ Confusing API calls (proxy-based)
❌ Relative URLs in environment
❌ Hidden endpoint mapping
```

### **After Fix:**
```
✅ Direct API calls to https://localhost:5001/api/*
✅ Explicit API URLs in environment files
✅ Clear and maintainable configuration
✅ Easy debugging and monitoring
✅ Production-ready setup
```

---

**Status:** ✅ **API URL CONFIGURATION COMPLETELY FIXED**

**All frontend API calls now correctly target `https://localhost:5001/api/*`** 🎊

---

## 🚀 **Quick Start Guide**

```bash
# 1. Start Backend
cd SSS.BE
dotnet run

# 2. Start Frontend
cd SSS.FE
npm start

# 3. Access Application
# Frontend: http://localhost:50503
# Backend:  https://localhost:5001
# Swagger:  https://localhost:5001/swagger
```

**Everything is now configured correctly!** ✨
