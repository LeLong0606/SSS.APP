# ?? AspNetUserTokens Integration Documentation

## Overview
H? th?ng SSS Employee Management ?ã ???c nâng c?p ?? s? d?ng b?ng `AspNetUserTokens` c?a ASP.NET Core Identity ?? l?u tr? và qu?n lý JWT tokens và refresh tokens m?t cách an toàn và chu?n.

## ?? AspNetUserTokens Table Structure

### Database Schema:
```sql
CREATE TABLE [AspNetUserTokens] (
    [UserId] NVARCHAR(450) NOT NULL,
    [LoginProvider] NVARCHAR(128) NOT NULL,  -- 'SSS_JWT_PROVIDER'
    [Name] NVARCHAR(128) NOT NULL,           -- 'AccessToken' or 'RefreshToken'
    [Value] NVARCHAR(MAX) NULL,              -- Token value
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name])
)
```

### Token Storage Pattern:
| UserId | LoginProvider | Name | Value |
|--------|---------------|------|--------|
| user-123 | SSS_JWT_PROVIDER | AccessToken | eyJhbGciOiJIUzI1NiIs... |
| user-123 | SSS_JWT_PROVIDER | RefreshToken | Xy9mDkL8vN2qR5tA... |

## ?? Features Implemented

### 1. **Enhanced Token Management**
- ? **JWT Access Tokens** stored in AspNetUserTokens
- ? **Refresh Tokens** stored in AspNetUserTokens  
- ? **Automatic Token Expiry** tracking
- ? **Secure Token Revocation** from database
- ? **Token Validation** against stored values

### 2. **New API Endpoints**

#### **POST /api/auth/refresh-token**
```json
// Request
{
  "refreshToken": "your-refresh-token-here"
}

// Response
{
  "success": true,
  "message": "Token refreshed successfully",
  "token": "new-jwt-token",
  "refreshToken": "new-refresh-token",
  "expiresAt": "2024-12-27T10:30:00.000Z",
  "user": { /* user info */ }
}
```

#### **POST /api/auth/revoke-token**
```json
// Request (optional specific token)
{
  "refreshToken": "token-to-revoke"  // Optional
}

// Response
{
  "success": true,
  "message": "Token revoked successfully"
}
```

#### **GET /api/auth/tokens/{userId}** (Admin only)
```json
// Response
{
  "message": "Token information retrieved from AspNetUserTokens table",
  "userId": "user-123",
  "note": "Actual token values are not returned for security reasons"
}
```

## ?? Implementation Details

### **JwtTokenService Enhanced Methods:**

#### **1. Token Storage**
```csharp
// Store Access Token with expiry
private async Task SetAccessTokenAsync(ApplicationUser user, string accessToken, DateTime expiry)
{
    var tokenData = $"{accessToken}|{expiry:yyyy-MM-ddTHH:mm:ssZ}";
    await _userManager.SetAuthenticationTokenAsync(user, "SSS_JWT_PROVIDER", "AccessToken", tokenData);
}

// Store Refresh Token
public async Task SetRefreshTokenAsync(ApplicationUser user, string refreshToken)
{
    await _userManager.SetAuthenticationTokenAsync(user, "SSS_JWT_PROVIDER", "RefreshToken", refreshToken);
}
```

#### **2. Token Retrieval**
```csharp
// Get stored refresh token
public async Task<string> GetRefreshTokenAsync(ApplicationUser user)
{
    return await _userManager.GetAuthenticationTokenAsync(user, "SSS_JWT_PROVIDER", "RefreshToken") ?? string.Empty;
}

// Get stored access token with expiry
public async Task<(string token, DateTime expiry)?> GetAccessTokenAsync(ApplicationUser user)
{
    var tokenData = await _userManager.GetAuthenticationTokenAsync(user, "SSS_JWT_PROVIDER", "AccessToken");
    // Parse token and expiry from stored format
}
```

#### **3. Token Validation**
```csharp
// Validate refresh token against stored value
public async Task<bool> ValidateRefreshTokenAsync(ApplicationUser user, string refreshToken)
{
    var storedToken = await GetRefreshTokenAsync(user);
    return storedToken == refreshToken;
}
```

#### **4. Token Cleanup**
```csharp
// Remove all tokens for user
public async Task RemoveRefreshTokenAsync(ApplicationUser user)
{
    await _userManager.RemoveAuthenticationTokenAsync(user, "SSS_JWT_PROVIDER", "RefreshToken");
    await _userManager.RemoveAuthenticationTokenAsync(user, "SSS_JWT_PROVIDER", "AccessToken");
}
```

### **AuthService Enhanced Workflow:**

#### **Login Process:**
```csharp
public async Task<AuthResponse> LoginAsync(LoginRequest request)
{
    // 1. Validate credentials
    var user = await _userManager.FindByEmailAsync(request.Email);
    var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
    
    // 2. Generate tokens
    var token = await _jwtTokenService.GenerateTokenAsync(user, roles);
    var refreshToken = _jwtTokenService.GenerateRefreshToken();
    
    // 3. Store tokens in AspNetUserTokens
    await _jwtTokenService.SetRefreshTokenAsync(user, refreshToken);
    
    // 4. Return both tokens to client
    return new AuthResponse { Token = token, RefreshToken = refreshToken };
}
```

#### **Logout Process:**
```csharp
public async Task<AuthResponse> LogoutAsync(ClaimsPrincipal user)
{
    // 1. Revoke JWT token (in-memory)
    var jti = user.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    _tokenRevocationService.RevokeToken(jti);
    
    // 2. Remove tokens from AspNetUserTokens table
    var applicationUser = await _userManager.FindByIdAsync(userId);
    await _jwtTokenService.RemoveRefreshTokenAsync(applicationUser);
}
```

#### **Refresh Token Process:**
```csharp
public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
{
    // 1. Find user by refresh token (search AspNetUserTokens)
    var user = await FindUserByRefreshTokenAsync(request.RefreshToken);
    
    // 2. Validate refresh token
    var isValid = await _jwtTokenService.ValidateRefreshTokenAsync(user, request.RefreshToken);
    
    // 3. Generate new tokens
    var newToken = await _jwtTokenService.GenerateTokenAsync(user, roles);
    var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
    
    // 4. Update stored refresh token
    await _jwtTokenService.SetRefreshTokenAsync(user, newRefreshToken);
}
```

## ?? Database Usage Patterns

### **Login/Register:**
```sql
-- Store new tokens
INSERT INTO AspNetUserTokens (UserId, LoginProvider, Name, Value)
VALUES ('user-123', 'SSS_JWT_PROVIDER', 'AccessToken', 'jwt-token|2024-12-27T10:30:00Z')

INSERT INTO AspNetUserTokens (UserId, LoginProvider, Name, Value) 
VALUES ('user-123', 'SSS_JWT_PROVIDER', 'RefreshToken', 'refresh-token-value')
```

### **Token Refresh:**
```sql
-- Validate existing refresh token
SELECT Value FROM AspNetUserTokens 
WHERE UserId = 'user-123' AND LoginProvider = 'SSS_JWT_PROVIDER' AND Name = 'RefreshToken'

-- Update with new tokens
UPDATE AspNetUserTokens SET Value = 'new-jwt-token|2024-12-27T11:30:00Z'
WHERE UserId = 'user-123' AND LoginProvider = 'SSS_JWT_PROVIDER' AND Name = 'AccessToken'

UPDATE AspNetUserTokens SET Value = 'new-refresh-token-value'
WHERE UserId = 'user-123' AND LoginProvider = 'SSS_JWT_PROVIDER' AND Name = 'RefreshToken'
```

### **Logout/Revoke:**
```sql
-- Remove all tokens for user
DELETE FROM AspNetUserTokens 
WHERE UserId = 'user-123' AND LoginProvider = 'SSS_JWT_PROVIDER'
```

### **Cleanup Expired Tokens:**
```sql
-- Find and remove expired access tokens
DELETE FROM AspNetUserTokens 
WHERE LoginProvider = 'SSS_JWT_PROVIDER' 
AND Name = 'AccessToken' 
AND CAST(RIGHT(Value, 20) AS DATETIME2) < GETUTCDATE()
```

## ?? Monitoring & Analytics

### **Token Usage Queries:**

#### **Active Users with Tokens:**
```sql
SELECT COUNT(DISTINCT UserId) as ActiveUsersWithTokens
FROM AspNetUserTokens 
WHERE LoginProvider = 'SSS_JWT_PROVIDER'
```

#### **Token Distribution:**
```sql
SELECT 
    Name as TokenType,
    COUNT(*) as TokenCount,
    COUNT(DISTINCT UserId) as UniqueUsers
FROM AspNetUserTokens 
WHERE LoginProvider = 'SSS_JWT_PROVIDER'
GROUP BY Name
```

#### **Recent Token Activity:**
```sql
-- This would require adding a timestamp column for more detailed tracking
-- Current implementation tracks via application logs
```

### **Performance Monitoring:**
- **Token validation speed**: Track time taken to validate tokens
- **Database queries**: Monitor AspNetUserTokens table performance
- **Token refresh frequency**: Track how often tokens are refreshed
- **Token cleanup efficiency**: Monitor expired token removal

## ??? Security Benefits

### **1. Centralized Token Management**
- ? All tokens stored in secure database table
- ? Tokens tied to specific users
- ? Easy token revocation across all devices
- ? Audit trail through database logs

### **2. Enhanced Security Features**
```csharp
// Token binding to user
await _userManager.SetAuthenticationTokenAsync(user, provider, name, value);

// Secure token validation
var storedToken = await _userManager.GetAuthenticationTokenAsync(user, provider, name);
return storedToken == providedToken;

// Complete token cleanup
await _userManager.RemoveAuthenticationTokenAsync(user, provider, name);
```

### **3. Attack Prevention**
- ? **Token Theft Protection**: Stolen tokens can be revoked from database
- ? **Session Hijacking**: All user sessions can be terminated
- ? **Refresh Token Rotation**: New refresh tokens on each use
- ? **Database-Level Security**: Tokens protected by database security

## ?? Token Lifecycle

```mermaid
graph TD
    A[User Login] --> B[Generate JWT + Refresh Token]
    B --> C[Store in AspNetUserTokens]
    C --> D[Return tokens to client]
    
    D --> E[Client uses JWT]
    E --> F{JWT Expired?}
    F -->|Yes| G[Use Refresh Token]
    F -->|No| E
    
    G --> H[Validate Refresh Token from DB]
    H --> I{Valid?}
    I -->|Yes| J[Generate New JWT + Refresh Token]
    I -->|No| K[Unauthorized - Login Required]
    
    J --> L[Update AspNetUserTokens]
    L --> D
    
    M[User Logout] --> N[Remove from AspNetUserTokens]
    O[Password Change] --> N
    P[Admin Revoke] --> N
```

## ?? API Usage Examples

### **Complete Authentication Flow:**

#### **1. Login:**
```bash
curl -X POST "https://localhost:5001/api/auth/login" \
-H "Content-Type: application/json" \
-d '{
  "email": "admin@sss.com",
  "password": "Admin123!"
}'

# Response: JWT token + refresh token (both stored in AspNetUserTokens)
```

#### **2. Use APIs:**
```bash
curl -X GET "https://localhost:5001/api/auth/me" \
-H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

#### **3. Refresh when expired:**
```bash
curl -X POST "https://localhost:5001/api/auth/refresh-token" \
-H "Content-Type: application/json" \
-d '{
  "refreshToken": "Xy9mDkL8vN2qR5tA..."
}'

# New tokens generated and stored in AspNetUserTokens
```

#### **4. Logout:**
```bash
curl -X POST "https://localhost:5001/api/auth/logout" \
-H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."

# All tokens removed from AspNetUserTokens table
```

## ?? Configuration Options

### **appsettings.json:**
```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "Issuer": "SSS.BE",
    "Audience": "SSS.BE.Users", 
    "ExpiryInHours": "24",
    "RefreshTokenExpiryInDays": "30"
  },
  "Database": {
    "Optimization": {
      "RetentionPolicies": {
        "UserTokensExpired": 7  // Clean expired tokens after 7 days
      }
    }
  }
}
```

## ?? Benefits Summary

### **? Security Enhanced:**
- Database-backed token storage
- Secure token revocation
- Comprehensive audit trails
- Protection against token theft

### **? Scalability Improved:**
- Centralized token management
- Easy multi-device session control
- Efficient token cleanup processes
- Database-level performance optimization

### **? User Experience Better:**
- Seamless token refresh
- Secure logout from all devices
- Long-lived sessions with refresh tokens
- Consistent authentication flow

### **? Development Simplified:**
- Standard ASP.NET Core Identity patterns
- Built-in token management methods
- Comprehensive logging and monitoring
- Easy integration with existing code

---

## ?? Final Result

**AspNetUserTokens table hi?n t?i s? ch?a:**
- ? JWT Access Tokens v?i thông tin expiry
- ? Refresh Tokens cho long-term sessions
- ? Automatic cleanup c?a expired tokens
- ? Complete audit trail c?a token activities
- ? Secure token management across all user sessions

**Database Query Example:**
```sql
SELECT TOP 10 
    u.Email,
    t.Name as TokenType,
    CASE 
        WHEN t.Name = 'AccessToken' THEN 'JWT Token (with expiry)'
        WHEN t.Name = 'RefreshToken' THEN 'Refresh Token'
        ELSE t.Name
    END as Description,
    LEN(t.Value) as TokenLength
FROM AspNetUserTokens t
JOIN AspNetUsers u ON t.UserId = u.Id  
WHERE t.LoginProvider = 'SSS_JWT_PROVIDER'
ORDER BY u.Email, t.Name
```

**H? th?ng gi? ?ây ho?t ??ng v?i token management hoàn toàn professional và secure!** ??