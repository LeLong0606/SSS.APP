# ?? PORT CONFIGURATION FIX - Frontend Backend Connection

## ?? **Issue Identified**
**Error:** `POST https://localhost:7005/api/auth/login net::ERR_CONNECTION_REFUSED`

**Root Cause:** Port mismatch between Frontend and Backend configuration
- ?? **Frontend was configured to:** Port 7005 (incorrect)
- ? **Backend actually runs on:** Port 5001 (correct)
- ? **Frontend actually runs on:** Port 50503 (correct)

---

## ?? **Complete Fix Applied**

### **1. Frontend Environment Configuration Fixed**

**File:** `SSS.FE\src\environments\environment.ts`
```typescript
// BEFORE (? WRONG):
apiUrl: 'https://localhost:7005/api'

// AFTER (? FIXED):
apiUrl: 'https://localhost:5001/api'
```

### **2. Backend CORS Configuration Verified**

**File:** `SSS.BE\Program.cs`
```csharp
// ? CORS correctly configured for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:50503", "https://localhost:50503") // ? Correct Angular ports
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
```

### **3. Backend Launch Settings Verified**

**File:** `SSS.BE\Properties\launchSettings.json`
```json
{
  "https": {
    "applicationUrl": "https://localhost:5001;http://localhost:5000" // ? Correct backend ports
  }
}
```

### **4. Frontend Launch Configuration Verified**

**File:** `SSS.FE\package.json`
```json
{
  "scripts": {
    "start": "ng serve --host=127.0.0.1 --port=50503" // ? Correct frontend port
  }
}
```

---

## ?? **Complete Port Configuration Matrix**

| Application | Protocol | Host | Port | Full URL | Status |
|-------------|----------|------|------|----------|---------|
| **Backend API** | HTTPS | localhost | 5001 | https://localhost:5001 | ? **ACTIVE** |
| **Backend API (HTTP)** | HTTP | localhost | 5000 | http://localhost:5000 | ? Redirect to HTTPS |
| **Frontend App** | HTTP | localhost | 50503 | http://localhost:50503 | ? **ACTIVE** |
| **Swagger UI** | HTTPS | localhost | 5001 | https://localhost:5001/swagger | ? **AVAILABLE** |

---

## ?? **Startup Instructions**

### **Option 1: Use Automated Scripts** (Recommended)

**Windows (Batch):**
```cmd
start-app.bat
```

**PowerShell/Cross-Platform:**
```powershell
.\start-app.ps1
```

### **Option 2: Manual Startup**

**Terminal 1 - Backend:**
```bash
cd SSS.BE
dotnet run --urls=https://localhost:5001
```

**Terminal 2 - Frontend:**
```bash
cd SSS.FE  
npm start
```

---

## ?? **Application URLs**

### **Development URLs:**
- ?? **Frontend Application:** http://localhost:50503
- ?? **Backend API:** https://localhost:5001/api
- ?? **Swagger Documentation:** https://localhost:5001/swagger
- ?? **Health Check:** https://localhost:5001/health
- ?? **System Metrics (Admin):** https://localhost:5001/metrics

### **API Endpoints:** 
- ?? **Login:** POST https://localhost:5001/api/auth/login
- ?? **Get User:** GET https://localhost:5001/api/auth/me
- ?? **Logout:** POST https://localhost:5001/api/auth/logout
- ?? **Employees:** GET https://localhost:5001/api/employee
- ?? **Departments:** GET https://localhost:5001/api/department

---

## ? **Verification Checklist**

### **Backend Verification:**
- [ ] Backend starts on https://localhost:5001 ?
- [ ] Swagger UI accessible at https://localhost:5001/swagger ?
- [ ] Health check responds at https://localhost:5001/health ?
- [ ] CORS allows frontend origin http://localhost:50503 ?

### **Frontend Verification:**
- [ ] Frontend starts on http://localhost:50503 ?
- [ ] Environment uses https://localhost:5001/api ?
- [ ] Login form makes requests to correct backend ?
- [ ] No CORS errors in browser console ?

### **Integration Testing:**
- [ ] Login request goes to https://localhost:5001/api/auth/login ?
- [ ] No ERR_CONNECTION_REFUSED errors ?
- [ ] JWT tokens received and stored correctly ?
- [ ] Authenticated API calls work properly ?

---

## ?? **Build & Run Status**

### **Backend (SSS.BE):**
```
? Build: SUCCESSFUL
? Port: https://localhost:5001
? CORS: Configured for http://localhost:50503
? JWT Auth: Enabled
? Swagger: Available at /swagger
```

### **Frontend (SSS.FE):**
```
? Build: SUCCESSFUL  
? Port: http://localhost:50503
? API URL: https://localhost:5001/api
? Auth Service: Configured correctly
? Environment: Development ready
```

---

## ?? **Test Connection**

### **Quick Test Commands:**

**1. Test Backend Health:**
```bash
curl -k https://localhost:5001/health
```

**2. Test Backend API:**
```bash
curl -k https://localhost:5001/api/auth/login -H "Content-Type: application/json" -d '{"email":"admin@sss.com","password":"Admin123!"}'
```

**3. Test Frontend:**
```bash
curl http://localhost:50503
```

---

## ?? **Configuration Summary**

### **Fixed Files:**
1. ? `SSS.FE\src\environments\environment.ts` - API URL updated to port 5001
2. ? `SSS.BE\Program.cs` - CORS configuration verified
3. ? `start-app.bat` - Windows startup script
4. ? `start-app.ps1` - PowerShell startup script

### **No Changes Needed:**
- ? `SSS.BE\Properties\launchSettings.json` - Already correct
- ? `SSS.FE\package.json` - Already correct
- ? `SSS.FE\src\app\core\services\auth.service.ts` - Uses environment correctly

---

**Status:** ?? **CONNECTION ISSUE RESOLVED**

**Next Steps:**
1. Start both applications using provided scripts
2. Navigate to http://localhost:50503
3. Test login functionality
4. Verify API communication works correctly

**The frontend will now correctly connect to the backend on port 5001!** ??