# ?? Custom Middleware Documentation

## Overview
H? th?ng SSS Employee Management ?� ???c trang b? 5 middleware t�y ch?nh ?? c?i thi?n hi?u su?t, b?o m?t v� kh? n?ng gi�m s�t:

## ?? Middleware Components

### 1. ??? Request Validation Middleware
**File**: `Infrastructure/Middleware/RequestValidationMiddleware.cs`

**Ch?c n?ng:**
- Ki?m tra v� ng?n ch?n c�c request ??c h?i (XSS, SQL Injection, etc.)
- Validate k�ch th??c request, headers, query parameters
- Ki?m tra Content-Type cho POST/PUT/PATCH requests
- Ph�t hi?n c�c pattern nguy hi?m trong request body

**C?u h�nh trong `appsettings.json`:**
```json
"RequestValidation": {
  "MaxRequestSize": 10485760,        // 10MB
  "MaxHeaderValueLength": 4096,      // 4KB
  "MaxQueryParameterLength": 2048,   // 2KB
  "MaxQueryStringLength": 8192,      // 8KB
  "RequireContentTypeHeader": true
}
```

**Malicious Patterns Detected:**
- Script injection: `<script`, `javascript:`, `vbscript:`
- SQL injection: `DROP TABLE`, `UNION SELECT`, `--`, `/*`
- Path traversal: `../`, `..\\`, `/etc/passwd`
- Code execution: `eval(`, `exec(`, `xp_cmdshell`

### 2. ? Rate Limiting Middleware
**File**: `Infrastructure/Middleware/RateLimitingMiddleware.cs`

**Ch?c n?ng:**
- Gi?i h?n s? l??ng request per user/IP
- Theo d�i t? ??ng user authenticated v� IP address
- Group c�c endpoint t??ng t? (e.g., `/api/employee/1` ? `/api/employee/{id}`)
- Th�m rate limit headers cho client

**C?u h�nh:**
```json
"RateLimit": {
  "MaxRequests": 100,      // 100 requests
  "TimeWindowMinutes": 1   // per 1 minute
}
```

**HTTP Headers ???c th�m khi rate limit:**
- `X-RateLimit-Limit`: Gi?i h?n t?i ?a
- `X-RateLimit-Remaining`: S? requests c�n l?i
- `X-RateLimit-Reset`: Th?i gian reset (Unix timestamp)
- `Retry-After`: S? gi�y c?n ??i

### 3. ?? Global Exception Middleware
**File**: `Infrastructure/Middleware/GlobalExceptionMiddleware.cs`

**Ch?c n?ng:**
- X? l� t?t c? exceptions kh�ng ???c handle
- Tr? v? response nh?t qu�n v?i format chu?n
- Log chi ti?t l?i v?i Request ID
- ?n sensitive information ? production

**Exception Handling:**
- `UnauthorizedAccessException` ? 403 Forbidden
- `ArgumentException`/`ArgumentNullException` ? 400 Bad Request
- `InvalidOperationException` ? 400 Bad Request
- `KeyNotFoundException` ? 404 Not Found
- `TimeoutException`/`TaskCanceledException` ? 408 Request Timeout
- `Exception` (generic) ? 500 Internal Server Error

**Development vs Production:**
- **Development**: Hi?n th? stack trace v� chi ti?t l?i
- **Production**: Ch? hi?n th? th�ng b�o generic + Request ID

### 4. ?? Performance Monitoring Middleware
**File**: `Infrastructure/Middleware/PerformanceMonitoringMiddleware.cs`

**Ch?c n?ng:**
- Theo d�i response time cho t?ng endpoint
- Gi�m s�t memory usage v� garbage collection
- Log slow requests (>2s warning, >5s error)
- Performance summary m?i 5 ph�t
- T? ??ng cleanup metrics c?

**Metrics ???c track:**
- Total requests per endpoint
- Average/Min/Max response times
- Success rate (%)
- Memory usage
- Recent requests (rolling window)

**HTTP Headers ???c th�m:**
- `X-Response-Time`: Th?i gian x? l� (ms)
- `X-Server-Time`: Server timestamp
- `X-Memory-Usage`: Memory usage hi?n t?i

**Automatic Actions:**
- Force garbage collection khi memory > 300MB
- Cleanup old metrics sau 30 ph�t
- Log performance warnings cho slow requests

### 5. ?? Request Logging Middleware
**File**: `Infrastructure/Middleware/RequestLoggingMiddleware.cs`

**Ch?c n?ng:**
- Log t?t c? HTTP requests v� responses
- Theo d�i performance v?i Stopwatch
- Capture request/response body (with size limits)
- Th�m Request ID cho easy tracing
- Log user information v� IP address

**Information Logged:**
- Request: Method, Path, Query, Body, User, IP
- Response: Status Code, Body, Processing Time
- Performance warnings cho slow requests
- User activity tracking

**Log Levels:**
- `Information`: Normal requests (200-299 status codes)
- `Warning`: Client errors (400-499 status codes)
- `Error`: Server errors (500+ status codes)
- `Warning`: Slow requests (>2000ms)

## ?? Usage & Configuration

### 1. Middleware Pipeline Order
```csharp
// Program.cs - Middleware ???c apply theo th? t?:
app.UseCustomMiddleware(builder.Configuration);
// 1. RequestValidationMiddleware    (early security)
// 2. RateLimitingMiddleware        (before auth)
// 3. GlobalExceptionMiddleware     (catch all errors)
// 4. RequestLoggingMiddleware      (after error handling)
// 5. PerformanceMonitoringMiddleware (measure total time)

// Then standard ASP.NET middleware:
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

### 2. Individual Middleware Usage
```csharp
// S? d?ng t?ng middleware ri�ng l?
app.UseRequestValidation(new RequestValidationOptions 
{ 
    MaxRequestSize = 5 * 1024 * 1024 // 5MB
});

app.UseRateLimiting(new RateLimitOptions 
{ 
    MaxRequests = 50, 
    TimeWindow = TimeSpan.FromMinutes(1) 
});

app.UseGlobalExceptionHandling();
app.UseRequestLogging();
app.UsePerformanceMonitoring();
```

### 3. Configuration Toggles
```json
"Middleware": {
  "EnableRequestLogging": true,
  "EnablePerformanceMonitoring": true,
  "EnableRateLimiting": true,
  "EnableRequestValidation": true
}
```

## ?? Monitoring Endpoints

### Health Check Endpoint
```
GET /health
```
**Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2024-12-26T10:30:00.000Z",
  "culture": "en-US",
  "version": "1.1.0",
  "environment": "Development",
  "memoryUsage": "45MB",
  "machineName": "DEV-SERVER",
  "processorCount": 8,
  "uptime": "02:15:30"
}
```

### System Metrics Endpoint (Admin only)
```
GET /metrics
Authorization: Bearer <admin-token>
```
**Response:**
```json
{
  "memory": {
    "totalMemoryMB": 45,
    "gen0Collections": 12,
    "gen1Collections": 5,
    "gen2Collections": 2
  },
  "system": {
    "processorCount": 8,
    "workingSetMB": 52,
    "threadCount": 23
  },
  "application": {
    "version": "1.1.0",
    "environment": "Development",
    "startTime": "2024-12-26T08:15:00.000Z",
    "uptime": "02:15:30"
  }
}
```

## ?? Benefits

### ??? Security
- **Request Validation**: Ng?n ch?n XSS, SQL Injection, Path Traversal
- **Rate Limiting**: Ch?ng spam v� DDoS attacks
- **Input Sanitization**: Validate headers, body, query parameters

### ?? Observability
- **Detailed Logging**: T?t c? requests ???c log v?i Request ID
- **Performance Tracking**: Response times, success rates, memory usage
- **Error Tracking**: Chi ti?t exceptions v?i stack trace (dev mode)

### ? Performance
- **Memory Management**: Auto GC khi memory cao
- **Slow Request Detection**: Warning cho requests > 2s
- **Metrics Cleanup**: T? ??ng d?n d?p old data

### ?? Maintainability
- **Consistent Error Format**: Unified error response structure
- **Request Tracing**: Request ID cho easy debugging
- **Configuration-Driven**: D? d�ng config qua appsettings.json

## ?? Example Logs

### Request Log
```
[12:30:45] [a1b2c3d4] REQUEST: POST /api/auth/login | Anonymous | IP: 192.168.1.100 | Body: {"email":"user@sss.com","password":"***"}
[12:30:46] [a1b2c3d4] RESPONSE: ? 200 | 245ms | Size: 856B | Body: {"success":true,"message":"Login successful"...}
```

### Performance Summary
```
[12:35:00] === PERFORMANCE SUMMARY ===
[12:35:00] ?? POST /api/auth/login: 25 reqs, Avg: 180.5ms, Min: 120ms, Max: 450ms, Success: 96.0%
[12:35:00] ?? GET /api/employee: 45 reqs, Avg: 95.2ms, Min: 45ms, Max: 200ms, Success: 100.0%
[12:35:00] ?? Memory Usage: 42MB
[12:35:00] ==============================
```

### Rate Limit Exceeded
```json
{
  "success": false,
  "message": "Rate limit exceeded",
  "errors": [
    "Maximum 100 requests allowed per 1 minutes",
    "Please try again in 60 seconds",
    "Request ID: a1b2c3d4"
  ]
}
```

## ?? Future Enhancements

1. **Redis Integration**: Distributed rate limiting cho multiple instances
2. **Metrics Export**: Prometheus/Grafana integration
3. **Circuit Breaker**: Auto-disable slow endpoints
4. **Request Correlation**: Distributed tracing v?i OpenTelemetry
5. **Smart Rate Limiting**: AI-based adaptive limits
6. **Real-time Dashboards**: Live performance monitoring

---

**T?t c? middleware ???c thi?t k? ?? ch?y hi?u qu? v� kh�ng ?nh h??ng ??n performance c?a h? th?ng ch�nh!** ??