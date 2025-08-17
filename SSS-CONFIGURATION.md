# ??? SSS Application Configuration Check

## ?? **Current Configuration:**

### **Backend (SSS.BE):**
- **HTTPS Port:** 5001
- **HTTP Port:** 5000  
- **API Base:** https://localhost:5001/api
- **Swagger UI:** https://localhost:5001/swagger
- **Health Check:** https://localhost:5001/health

### **Frontend (SSS.FE):**
- **Port:** 50503
- **App URL:** http://localhost:50503
- **API URL:** https://localhost:5001/api ? Updated

### **CORS Configuration:**
- **Allowed Origins:** http://localhost:50503, https://localhost:50503
- **Methods:** All methods allowed
- **Headers:** All headers allowed
- **Credentials:** Enabled

## ?? **Start Commands:**

### **Backend Only:**
```bash
cd SSS.BE
dotnet run --urls "https://localhost:5001;http://localhost:5000"
```

### **Frontend Only:**
```bash
cd SSS.FE
npm start
```

### **Both Applications:**
```bash
# Use PowerShell script
.\start-app.ps1

# Or use Batch script  
start-app.bat
```

## ?? **Troubleshooting:**

### **If Backend won't start:**
1. Check if port 5001 is already in use
2. Run `netstat -ano | findstr 5001` to check
3. Kill any process using the port
4. Ensure SSL certificate is trusted

### **If Frontend can't connect:**
1. Verify backend is running on port 5001
2. Check CORS configuration in Program.cs
3. Check browser console for detailed errors
4. Ensure firewall isn't blocking the connection

### **Default Admin Account:**
- **Email:** admin@sss.com
- **Password:** Admin123!

## ? **Expected Flow:**
1. Start Backend ? Swagger available at https://localhost:5001/swagger
2. Start Frontend ? App available at http://localhost:50503
3. Login with admin credentials ? Should redirect to dashboard
4. API calls should work without CORS errors

---

**Status:** ? **Configuration Updated**  
**Next:** ?? **Test the application with the start scripts**