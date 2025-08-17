# Entity Framework Relationship Fix

## Problem
The relationship configuration between `Department.TeamLeader` and `Employee` was failing with the following error:

```
System.InvalidOperationException: The relationship from 'Department.TeamLeader' to 'Employee' with foreign key properties {'TeamLeaderId' : string} cannot target the primary key {'Id' : int} because it is not compatible.
```

## Root Cause
- `Department.TeamLeaderId` is defined as `string` (to store EmployeeCode)  
- `Employee.Id` is defined as `int` (primary key)
- Entity Framework was trying to create a foreign key relationship between incompatible types

## Solution
Updated the relationship configuration in `ApplicationDbContext.cs` to use `EmployeeCode` as the principal key instead of the primary key `Id`:

```csharp
// Configure Team Leader relationship using EmployeeCode as principal key
entity.HasOne(d => d.TeamLeader)
      .WithOne()
      .HasForeignKey<Department>(d => d.TeamLeaderId)
      .HasPrincipalKey<Employee>(e => e.EmployeeCode)  // <- Added this line
      .OnDelete(DeleteBehavior.SetNull);
```

## Result
- Department.TeamLeaderId (string) now correctly references Employee.EmployeeCode (string)
- Maintains business logic where departments reference employees by their employee codes
- Preserves the one-to-one relationship between departments and team leaders
- Database can be created successfully without errors

## Database Schema
```
Departments
??? Id (int, PK)
??? Name (string)
??? TeamLeaderId (string, FK -> Employee.EmployeeCode)
??? ...

Employees  
??? Id (int, PK)
??? EmployeeCode (string, UK) <- Principal Key for relationship
??? FullName (string)
??? ...
```