# ?? Employee Management System

A comprehensive Employee Management System with JWT Authentication, built with .NET 8 Web API and Angular frontend.

## ?? **Key Features**

### ?? **Authentication & Authorization**
- **JWT Token Authentication** with automatic expiration
- **4-Level Role System**: Administrator > Director > TeamLeader > Employee
- **Simple Role-Based Authorization** - Direct role checking in controllers
- **Token Revocation** on logout for enhanced security
- **Password Management** with secure hashing

### ????? **Employee Management**
- **Complete CRUD Operations** for employees
- **Department Assignment** with team leader relationships
- **Employee Search & Filtering** with pagination
- **Status Management** (Active/Inactive employees)
- **Employee-User Linking** via unique employee codes

### ?? **Department Management**
- **Department CRUD Operations** with role-based permissions
- **Team Leader Assignment** - One team leader per department
- **Department Hierarchy** management
- **Employee Count** tracking per department

### ?? **Technical Features**
- **Clean Architecture** with proper separation of concerns
- **Entity Framework Core** with SQL Server
- **Soft Delete** pattern for data integrity
- **Comprehensive Validation** with English error messages
- **Swagger UI** with integrated JWT authentication
- **Health Check** endpoints
- **Logging** with structured data

## ??? **Project Structure**

```
employee-management-system/
??? SSS.BE/                          # .NET 8 Web API Backend
?   ??? Controllers/                 # API Controllers
?   ?   ??? AuthController.cs        # Authentication endpoints
?   ?   ??? EmployeeController.cs    # Employee management
?   ?   ??? DepartmentController.cs  # Department management
?   ??? Domain/
?   ?   ??? Entities/               # Domain entities
?   ??? Infrastructure/
?   ?   ??? Auth/                   # JWT services
?   ?   ??? Configuration/          # App configuration
?   ?   ??? Data/                   # Data seeding
?   ?   ??? Identity/               # User identity
?   ??? Models/                     # DTOs and request/response models
?   ??? Persistence/                # Database context
?   ??? Program.cs                  # Application entry point
??? SSS.FE/                         # Angular Frontend (Placeholder)
    ??? src/                        # Angular source code
```

## ?? **Quick Start**

### **Prerequisites**
- **.NET 8 SDK**
- **SQL Server** (LocalDB or full instance)
- **Node.js 18+** (for Angular frontend)
- **Angular CLI** (`npm install -g @angular/cli`)

### **Backend Setup (.NET API)**

1. **Clone the repository**
   ```bash
   git clone https://github.com/[your-username]/employee-management-system.git
   cd employee-management-system
   ```

2. **Navigate to backend**
   ```bash
   cd SSS.BE
   ```

3. **Update connection string** in `appsettings.json`
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EmployeeManagementDB;Trusted_Connection=true;MultipleActiveResultSets=true"
     }
   }
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access Swagger UI**
   - Open: `https://localhost:5001/swagger`
   - Use the "Authorize" button to test with JWT tokens

### **Frontend Setup (Angular)**

1. **Navigate to frontend**
   ```bash
   cd SSS.FE
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```

3. **Start development server**
   ```bash
   ng serve
   ```

4. **Access application**
   - Open: `http://localhost:4200`

## ?? **Default Test Accounts**

| Email | Password | Role | Employee Code | Permissions |
|-------|----------|------|---------------|-------------|
| admin@sss.com | Admin@123456 | Administrator | ADMIN001 | Full system control |
| director@sss.com | Director@123 | Director | DIR001 | Cross-department management |
| teamlead@sss.com | TeamLead@123 | TeamLeader | TL001 | Department-level management |
| employee@sss.com | Employee@123 | Employee | EMP001 | Basic system access |

## ?? **API Endpoints**

### **Authentication**
- `POST /api/auth/login` - Login and get JWT token
- `POST /api/auth/register` - Register new user
- `POST /api/auth/logout` - Logout and revoke token
- `GET /api/auth/me` - Get current user info
- `POST /api/auth/change-password` - Change password

### **Employee Management**
- `GET /api/employee` - List employees (All authenticated users)
- `POST /api/employee` - Create employee (TeamLeader+)
- `PUT /api/employee/{id}` - Update employee (TeamLeader+)
- `DELETE /api/employee/{id}` - Delete employee (Director+)
- `PATCH /api/employee/{id}/status` - Change status (Director+)

### **Department Management**
- `GET /api/department` - List departments (All authenticated users)
- `POST /api/department` - Create department (Director+)
- `PUT /api/department/{id}` - Update department (Director+)
- `DELETE /api/department/{id}` - Delete department (Administrator only)
- `POST /api/department/{id}/assign-team-leader` - Assign team leader (Director+)

## ??? **Role-Based Authorization**

### **Simple Authorization Pattern**
```csharp
[Authorize]                                    // Any authenticated user
[Authorize(Roles = "Administrator")]           // Administrator only
[Authorize(Roles = "Administrator,Director")]  // Administrator OR Director
```

### **Permission Matrix**

| Operation | Employee | TeamLeader | Director | Administrator |
|-----------|----------|------------|----------|---------------|
| View Data | ? | ? | ? | ? |
| Create Employee | ? | ? | ? | ? |
| Delete Employee | ? | ? | ? | ? |
| Create Department | ? | ? | ? | ? |
| Delete Department | ? | ? | ? | ? |

## ?? **Database Schema**

### **Core Tables**
- **AspNetUsers** - Authentication and user data
- **Employees** - Employee information and relationships
- **Departments** - Department data with team leader references
- **AspNetRoles** - System roles (Administrator, Director, TeamLeader, Employee)

### **Key Relationships**
- `AspNetUsers.EmployeeCode` ? `Employees.EmployeeCode` (One-to-One)
- `Departments.Id` ? `Employees.DepartmentId` (One-to-Many)
- `Departments.TeamLeaderId` ? `Employees.EmployeeCode` (One-to-One)

## ?? **Technologies Used**

### **Backend**
- **.NET 8** - Web API framework
- **Entity Framework Core** - ORM with SQL Server
- **ASP.NET Identity** - Authentication and user management
- **JWT Bearer Authentication** - Stateless authentication
- **Swagger/OpenAPI** - API documentation
- **Serilog** - Structured logging
- **FluentValidation** - Request validation

### **Frontend (Planned)**
- **Angular 18** - Frontend framework
- **TypeScript** - Type-safe JavaScript
- **Angular Material** - UI component library
- **RxJS** - Reactive programming
- **Karma/Jasmine** - Testing framework

## ??? **Security Features**

- **JWT Token Security** with configurable expiration
- **Password Hashing** using ASP.NET Identity (PBKDF2)
- **Token Revocation** on logout
- **Role-based Authorization** at controller level
- **Input Validation** with comprehensive error handling
- **Unique Constraints** on critical fields (EmployeeCode, Email)
- **Soft Delete** pattern for data integrity

## ?? **Development Guide**

### **Adding New Endpoints**
1. Create DTO models in `Models/` directory
2. Add controller with appropriate authorization
3. Use simple role-based authorization: `[Authorize(Roles = "Role1,Role2")]`
4. Follow REST conventions for HTTP methods
5. Return consistent `ApiResponse<T>` or `PagedResponse<T>` objects

### **Database Changes**
1. Update entity models in `Domain/Entities/`
2. Configure relationships in `ApplicationDbContext`
3. Add migration: `dotnet ef migrations add MigrationName`
4. Update database: `dotnet ef database update`

### **Testing API**
1. Use Swagger UI at `https://localhost:5001/swagger`
2. Login with test accounts to get JWT token
3. Use "Authorize" button to set authentication
4. Test role-based access with different user accounts

## ?? **Contributing**

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## ?? **License**

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ?? **Support**

For support and questions:
- Create an issue in this repository
- Check the [Wiki](../../wiki) for detailed documentation
- Review the API documentation in Swagger UI

---

## ?? **Ready to Use!**

This system provides a complete foundation for employee management with authentication, authorization, and CRUD operations. The simple role-based authorization makes it easy to understand and extend! ??