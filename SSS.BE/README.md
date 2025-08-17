# SSS Authentication API with Hierarchical Authorization

Authentication and Employee Management system with JWT Token and hierarchical role-based authorization for SSS application, fully configured in English.

## ??? **Hierarchical Authorization System**

### **Role Hierarchy: Administrator > Director > TeamLeader > Employee**

```
Level 1: Administrator (Highest Authority)
    ? Can do everything Directors can do, plus:
    - Delete departments
    - System-wide administrative functions
    
Level 2: Director (Cross-Department Management)
    ? Can do everything TeamLeaders can do, plus:
    - Create/Update/Delete departments  
    - Create/Update/Delete employees across all departments
    - Assign/Remove team leaders
    - Activate/Deactivate employees
    
Level 3: TeamLeader (Department-Level Management)
    ? Can do everything Employees can do, plus:
    - Create/Update employees within scope
    - Manage team members
    
Level 4: Employee (Basic Access)
    - View employees and departments
    - Access own profile
    - Change own password
```

## Features

### Authentication
- ? User registration with hierarchical role selection
- ? Login with JWT Token
- ? Logout with token revocation
- ? Password hashing with ASP.NET Identity
- ? **Hierarchical role system**: Administrator > Director > TeamLeader > Employee
- ? **Policy-based authorization** with inheritance
- ? Password change functionality
- ? Current user information retrieval

### Employee & Department Management
- ? **Department Management** - Role-based CRUD operations
- ? **Employee Management** - Hierarchical permission control
- ? **Team Leader Assignment** - Director+ can assign/remove team leaders
- ? **Employee-User Linking** - Employees linked via EmployeeCode to AspNetUsers
- ? **Department Hierarchy** - Employees belong to departments with team leadership
- ? **Status Management** - Director+ can activate/deactivate employees

## API Endpoints with Hierarchical Authorization

### Authentication Endpoints

| Method | Endpoint | Description | Required Level |
|--------|----------|-------------|----------------|
| POST | `/api/auth/register` | Register new user | Public |
| POST | `/api/auth/login` | Login and get JWT token | Public |
| POST | `/api/auth/logout` | Logout and revoke token | Employee+ |
| GET | `/api/auth/me` | Get current user information | Employee+ |
| POST | `/api/auth/change-password` | Change password | Employee+ |
| GET | `/api/auth/roles` | Get available roles | Director+ |

### Employee Management Endpoints

| Method | Endpoint | Description | Required Level |
|--------|----------|-------------|----------------|
| GET | `/api/employee` | Get all employees (paginated) | Employee+ |
| GET | `/api/employee/{id}` | Get employee by ID | Employee+ |
| GET | `/api/employee/code/{code}` | Get employee by code | Employee+ |
| POST | `/api/employee` | Create new employee | TeamLeader+ |
| PUT | `/api/employee/{id}` | Update employee | TeamLeader+ |
| DELETE | `/api/employee/{id}` | Delete employee (soft delete) | Director+ |
| PATCH | `/api/employee/{id}/status` | Activate/Deactivate employee | Director+ |

### Department Management Endpoints

| Method | Endpoint | Description | Required Level |
|--------|----------|-------------|----------------|
| GET | `/api/department` | Get all departments | Employee+ |
| GET | `/api/department/{id}` | Get department by ID | Employee+ |
| GET | `/api/department/{id}/employees` | Get department employees | Employee+ |
| POST | `/api/department` | Create new department | Director+ |
| PUT | `/api/department/{id}` | Update department | Director+ |
| DELETE | `/api/department/{id}` | Delete department | Administrator |
| POST | `/api/department/{id}/assign-team-leader` | Assign team leader | Director+ |
| DELETE | `/api/department/{id}/remove-team-leader` | Remove team leader | Director+ |

### Authorization Test Endpoints

| Method | Endpoint | Required Level | Description |
|--------|----------|----------------|-------------|
| GET | `/api/auth/test/admin` | Administrator | Highest level access test |
| GET | `/api/auth/test/director` | Director+ | Cross-department management test |
| GET | `/api/auth/test/teamleader` | TeamLeader+ | Department-level management test |
| GET | `/api/auth/test/employee` | Employee+ | Basic system access test |
| GET | `/api/auth/test/management` | Management Level | All management roles test |

## Authorization Policies Explained

### **Hierarchical Policies**
```csharp
// Level-based access (higher levels include lower levels)
"AdminOnly"           -> Administrator only
"DirectorAndAbove"    -> Administrator + Director  
"TeamLeaderAndAbove"  -> Administrator + Director + TeamLeader
"EmployeeAndAbove"    -> All authenticated users

// Specific role policies
"DirectorOnly"        -> Director role only
"TeamLeaderOnly"      -> TeamLeader role only  
"EmployeeOnly"        -> Employee role only

// Combined policies
"ManagementLevel"     -> Administrator + Director + TeamLeader
"NonAdmin"           -> Director + TeamLeader + Employee
```

### **Permission Logic Examples**

#### **Employee Operations:**
- **View Employees**: Employee+ (Everyone can view)
- **Create Employee**: TeamLeader+ (Management can create)
- **Update Employee**: TeamLeader+ (Management can update)  
- **Delete Employee**: Director+ (Senior management can delete)
- **Change Status**: Director+ (Senior management controls activation)

#### **Department Operations:**
- **View Departments**: Employee+ (Everyone can view)
- **Create Department**: Director+ (Cross-department authority needed)
- **Update Department**: Director+ (Cross-department authority needed)
- **Delete Department**: Administrator (System-wide authority needed)
- **Manage Team Leaders**: Director+ (Senior management assigns leaders)

## Sample Data

### Test Accounts (Hierarchical Order)
| Email | Password | Role | Level | Employee Code |
|-------|----------|------|-------|---------------|
| admin@sss.com | Admin@123456 | Administrator | 1 | ADMIN001 |
| director@sss.com | Director@123 | Director | 2 | DIR001 |
| teamlead@sss.com | TeamLead@123 | TeamLeader | 3 | TL001 |
| employee@sss.com | Employee@123 | Employee | 4 | EMP001 |

### Sample Department Structure
1. **Information Technology** (IT) - Team Leader: Jane Doe (TL001)
2. **Human Resources** (HR) - Team Leader: Alice Wilson (TL002)  
3. **Finance** (FIN) - No team leader assigned

## Usage Examples

### 1. **Administrator** - Full System Control
```bash
# Can do everything - create departments, delete employees, etc.
curl -H "Authorization: Bearer <admin-token>" \
     -X DELETE https://localhost:5001/api/department/1
```

### 2. **Director** - Cross-Department Management
```bash
# Can create departments and manage all employees
curl -H "Authorization: Bearer <director-token>" \
     -H "Content-Type: application/json" \
     -X POST https://localhost:5001/api/department \
     -d '{"name":"Marketing","departmentCode":"MKT"}'
```

### 3. **TeamLeader** - Department-Level Management  
```bash
# Can create employees but cannot delete departments
curl -H "Authorization: Bearer <teamleader-token>" \
     -H "Content-Type: application/json" \
     -X POST https://localhost:5001/api/employee \
     -d '{"employeeCode":"EMP005","fullName":"John Smith"}'
```

### 4. **Employee** - Basic Access
```bash
# Can view data but cannot create/update/delete
curl -H "Authorization: Bearer <employee-token>" \
     https://localhost:5001/api/employee
```

## Business Rules

### **Hierarchical Authority**
1. **Higher roles inherit lower role permissions**
2. **Administrators can override any business rule**
3. **Directors manage across departments**  
4. **TeamLeaders manage within their scope**
5. **Employees have read-only access to most data**

### **Team Leadership Rules**
- Only Directors+ can assign/remove team leaders
- Each department has exactly one team leader
- Team leaders must be employees
- Removing team leader clears department reference

### **Employee Management Rules**
- TeamLeaders+ can create/update employees
- Directors+ can delete/activate/deactivate employees
- Soft delete preserves data integrity
- Team leader status automatically managed

## Security Features

- ? **Hierarchical authorization** with role inheritance
- ? **JWT token** with configurable expiration
- ? **Token revocation** on logout
- ? **Policy-based authorization** with clear hierarchy
- ? **Input validation** with English messages
- ? **Unique constraints** on critical fields
- ? **Soft delete** for data integrity
- ? **Role-based API access** control

## Testing the Hierarchy

### **Role Inheritance Testing**
```bash
# Test Administrator (should access all endpoints)
curl -H "Authorization: Bearer <admin-token>" \
     https://localhost:5001/api/auth/test/admin      # ? Success

# Test Director (should fail admin-only, pass director+)  
curl -H "Authorization: Bearer <director-token>" \
     https://localhost:5001/api/auth/test/admin      # ? Forbidden
curl -H "Authorization: Bearer <director-token>" \
     https://localhost:5001/api/auth/test/director   # ? Success

# Test TeamLeader (should fail director+, pass teamleader+)
curl -H "Authorization: Bearer <teamleader-token>" \
     https://localhost:5001/api/auth/test/director   # ? Forbidden  
curl -H "Authorization: Bearer <teamleader-token>" \
     https://localhost:5001/api/auth/test/teamleader # ? Success

# Test Employee (should fail management, pass employee+)
curl -H "Authorization: Bearer <employee-token>" \
     https://localhost:5001/api/auth/test/teamleader # ? Forbidden
curl -H "Authorization: Bearer <employee-token>" \
     https://localhost:5001/api/auth/test/employee   # ? Success
```

## Swagger UI

Access Swagger UI at: `https://localhost:5001/swagger`

- **Complete API documentation** with authorization requirements
- **JWT authentication** support with "Authorize" button
- **Test all endpoints** with role-based access control
- **Interactive testing** of hierarchical permissions
- **Clear indication** of required authorization levels

The **hierarchical authorization system** ensures proper access control while maintaining flexibility and security! ??