# ?? Work Shift Management System - Feature Documentation

## ?? **Overview**

The Work Shift Management System has been added to the Employee Management System, providing comprehensive shift scheduling capabilities with strict authorization controls and audit trails.

## ??? **New Database Tables**

### **WorkLocations**
- **Purpose**: Define available work locations where shifts can be assigned
- **Key Fields**: Name, LocationCode, Address, Description, IsActive
- **Sample Data**: Main Office, Branch Office, Remote Work, Client Sites, Training Center

### **WorkShifts**  
- **Purpose**: Store individual work shift assignments
- **Key Fields**: EmployeeCode, WorkLocationId, ShiftDate, StartTime, EndTime, TotalHours
- **Relationships**: Employee, WorkLocation, AssignedByEmployee, ModifiedByEmployee
- **Constraints**: Max 8 hours per shift, Max 8 hours total per day

### **WorkShiftLogs**
- **Purpose**: Complete audit trail of all shift changes
- **Key Fields**: WorkShiftId, Action (CREATE/UPDATE/DELETE), PerformedBy, OriginalValues, NewValues
- **Features**: JSON storage of before/after values, modification reasons, comments

## ?? **Authorization Matrix**

| **Action** | **Employee** | **TeamLeader** | **Director** | **Administrator** |
|------------|-------------|---------------|-------------|------------------|
| **View Own Shifts** | ? | ? | ? | ? |
| **View Dept Shifts** | ? | ? | ? | ? |
| **View All Shifts** | ? | ? | ? | ? |
| **Assign Shifts (Dept)** | ? | ? | ? | ? |
| **Assign Shifts (All)** | ? | ? | ? | ? |
| **Modify Own Assigned** | ? | ? | ? | ? |
| **Override Any Shift** | ? | ? | ? | ? |
| **Delete Shifts** | ? | ? | ? | ? |
| **View Audit Logs** | ? | ? (Dept) | ? | ? |

## ?? **New API Endpoints**

### **Work Location Management**
```bash
GET    /api/worklocation              # View all locations
GET    /api/worklocation/{id}         # View location details  
POST   /api/worklocation              # Create location (Director+)
PUT    /api/worklocation/{id}         # Update location (Director+)
DELETE /api/worklocation/{id}         # Delete location (Admin only)
```

### **Work Shift Management**
```bash
GET    /api/workshift                 # View shifts (filtered by authorization)
GET    /api/workshift/weekly/{code}   # View weekly shifts for employee
POST   /api/workshift/validate        # Validate shift timing/conflicts
POST   /api/workshift/weekly          # Create weekly shifts (TeamLeader+)
PUT    /api/workshift/{id}            # Update shift (TeamLeader+)
DELETE /api/workshift/{id}            # Delete shift (Director+)
GET    /api/workshift/{id}/logs       # View audit logs (TeamLeader+)
```

## ?? **Business Rules Implementation**

### **? Shift Assignment Rules**
1. **TeamLeader Authority**: Can only assign shifts to employees in their department
2. **Self-Assignment**: TeamLeaders can assign shifts to themselves
3. **Director Override**: Directors can modify any shift with audit trail
4. **Department Boundary**: TeamLeaders cannot cross department boundaries

### **? Time Constraints**
1. **Daily Limit**: Maximum 8 hours per day per employee
2. **Shift Duration**: Individual shifts cannot exceed 8 hours
3. **Conflict Prevention**: No overlapping shifts for same employee
4. **Week Structure**: Monday to Sunday (7-day week)

### **?? Validation System**
1. **Real-time Validation**: Immediate conflict detection
2. **Time Logic**: End time must be after start time
3. **Daily Hour Check**: Total daily hours validation
4. **Overlap Detection**: Automatic shift conflict detection

### **?? Audit Trail Features**
1. **Complete History**: Every action (CREATE/UPDATE/DELETE) logged
2. **Before/After Values**: JSON storage of all changes
3. **Modification Tracking**: Who changed what and when
4. **Reason Documentation**: Required reasons for modifications
5. **Director Override Logging**: Special marking when Directors override TeamLeader assignments

## ?? **Usage Examples**

### **1. TeamLeader Assigns Weekly Shifts**
```json
POST /api/workshift/weekly
{
  "employeeCode": "EMP001",
  "weekStartDate": "2024-12-23", // Monday
  "dailyShifts": [
    {
      "dayOfWeek": 1, // Monday
      "workLocationId": 1,
      "startTime": "09:00",
      "endTime": "18:00"
    },
    {
      "dayOfWeek": 5, // Friday  
      "workLocationId": 3, // Remote work
      "startTime": "08:00", 
      "endTime": "16:00"
    }
  ]
}
```

### **2. Director Overrides TeamLeader Assignment**
```json
PUT /api/workshift/123
{
  "workLocationId": 2,
  "startTime": "10:00",
  "endTime": "19:00",
  "modificationReason": "Client meeting requires later schedule"
}
```

### **3. View Weekly Schedule**
```bash
GET /api/workshift/weekly/EMP001?weekStartDate=2024-12-23

Response:
{
  "employeeCode": "EMP001",
  "employeeName": "Bob Johnson",
  "weekStartDate": "2024-12-23",
  "weekEndDate": "2024-12-29", 
  "totalWeeklyHours": 40.0,
  "dailyShifts": [...]
}
```

## ?? **Sample Data Structure**

### **Work Locations**
- **Main Office**: Primary office with all departments
- **Branch Office**: Secondary location  
- **Remote Work**: Work from home option
- **Client Site A**: On-site client work
- **Training Center**: Corporate training facility

### **Sample Weekly Schedule**
- **IT Team Leader (TL001)**: 8 hours/day, Mix of office/remote
- **Developer (EMP001)**: 8 hours/day, Friday remote
- **HR Leader (TL002)**: 8 hours/day, Always in main office
- **HR Specialist (EMP002)**: 7 hours/day, Flexible schedule

## ?? **Security Features**

### **Role-Based Access Control**
- **Employee**: View own shifts only
- **TeamLeader**: Manage department shifts, view department logs
- **Director**: Override any shift, view all logs, cross-department access
- **Administrator**: Full system access

### **Audit & Compliance**
- **Complete Audit Trail**: Every change tracked
- **Modification Detection**: Automatic flagging of Director overrides
- **Reason Documentation**: Required explanations for changes
- **JSON Value Storage**: Before/after state preservation

## ?? **Getting Started**

### **1. Database Migration**
The new tables will be automatically created when you run the application. Sample data includes:
- 5 work locations
- Weekly shifts for current week
- Audit logs for all sample shifts

### **2. Testing the System**
Use the existing test accounts:
- **TL001** (teamlead@sss.com) - Can manage IT department shifts
- **TL002** (teamlead2@sss.com) - Can manage HR department shifts  
- **DIR001** (director@sss.com) - Can manage all shifts
- **EMP001** (employee@sss.com) - Can view own shifts only

### **3. API Testing via Swagger**
1. Login with appropriate role
2. Use the "Authorize" button with JWT token
3. Test shift assignment and validation endpoints
4. View audit logs to see tracking in action

## ?? **Key Benefits**

? **Strict Authorization**: Role-based permissions prevent unauthorized access  
? **Department Boundaries**: TeamLeaders limited to their own departments  
? **Director Override**: Senior management can intervene with full audit trail  
? **Conflict Prevention**: Automatic validation prevents scheduling conflicts  
? **Complete Audit**: Every change tracked for compliance and accountability  
? **Flexible Scheduling**: Support for office, remote, and client-site work  
? **Time Management**: Enforced 8-hour daily limits with validation  

The system now provides comprehensive work shift management with enterprise-grade authorization and audit capabilities! ??