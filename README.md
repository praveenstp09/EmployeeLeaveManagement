# Employee Leave Management API

An ASP.NET Core Web API for managing:
- Employees and authentication
- Departments and department heads
- Leave types and yearly allocations
- Leave requests with role-based approval workflows
- Employee profile image uploads via Supabase Storage

## Tech Stack

- ASP.NET Core Web API (`net10.0`)
- Entity Framework Core + PostgreSQL (Npgsql)
- JWT Bearer authentication
- BCrypt password hashing
- Swagger / OpenAPI (`Swashbuckle`)
- Supabase Storage for file uploads

## Project Structure

- `Controllers/` API endpoints and business rules
- `Models/` Entity models
- `Dtos/` Request and response contracts
- `Config/EmployeeLeaveDbContext.cs` EF Core DbContext and relationships
- `Services/TokenService.cs` JWT generation
- `Services/SupabaseServices/` Supabase initialization and file storage service
- `Migrations/` EF Core migrations
- `Program.cs` DI, auth, CORS, Swagger, middleware pipeline

## Core Features

- JWT-based login and protected APIs
- First registered user can bootstrap any role (typically `SuperAdmin`)
- Later anonymous registrations are forced to `Employee`
- Department lifecycle management
- Department head assignment and role updates
- Leave type management (`RequiresApproval`, yearly max days)
- Leave balance allocation (single employee or bulk for all active employees)
- Leave request apply/approve/reject/cancel flow
- Approval hierarchy based on requester and approver roles
- Profile image upload to Supabase bucket

## Roles

Roles used in the system:
- `SuperAdmin`
- `DepartmentHead`
- `HR`
- `Manager`
- `Employee`

### Notable Authorization Rules

- `SuperAdmin` can manage departments, leave types, deactivate employees, bulk allocate balances, and view all leave requests.
- `DepartmentHead` can allocate leave balances within their department and approve requests based on hierarchy rules.
- `HR` can allocate and approve within their own department (as defined in controller logic).
- `Manager` can approve direct subordinates' requests (for `Employee` role subordinates).
- Employees can view/apply/cancel their own leave requests and view their own balances.

## Data Model (High Level)

- `Department` has many `Employees`.
- A `Department` can have one `DepartmentHead` (`DepartmentHeadId`, unique when not null).
- `Employee` can have a self-referencing `Manager` and many `Subordinates`.
- `LeaveRequest` belongs to `Employee` and `LeaveType`, with optional approver (`ApprovedById`).
- `LeaveBalance` is unique per (`EmployeeId`, `LeaveTypeId`, `Year`).

## Configuration

Update `appsettings.json` before running:

```json
{
	"ConnectionStrings": {
		"Default": "Host=localhost;Port=5432;Database=employee_leave_db;Username=postgres;Password=your_password"
	},
	"Jwt": {
		"Secret": "your-long-random-secret-at-least-32-characters"
	},
	"Supabase": {
		"Url": "https://your-project-id.supabase.co",
		"Key": "your-supabase-anon-or-service-key",
		"Bucket": "your-bucket-name"
	}
}
```

## Prerequisites

- .NET SDK 10
- PostgreSQL
- Supabase project and storage bucket (for profile image upload)

## Getting Started

1. Restore dependencies

```bash
dotnet restore
```

2. Update database

```bash
dotnet ef database update
```

If `dotnet ef` is not installed:

```bash
dotnet tool install --global dotnet-ef
```

3. Run the API

```bash
dotnet run
```

Default local URLs (from launch settings):
- `http://localhost:5299`
- `https://localhost:7126`

4. Open Swagger UI (Development)

- `https://localhost:7126/swagger`
- or `http://localhost:5299/swagger`

Use `Authorize` in Swagger with:

```text
Bearer <jwt-token>
```

## Authentication

- JWT token lifetime: 60 minutes
- Claims included:
	- `NameIdentifier` (employee id)
	- `Email`
	- `Name` (username)
	- `Role`

## API Response Format

All endpoints wrap output in:

```json
{
	"success": true,
	"message": "optional",
	"token": "optional",
	"data": {}
}
```

Note: Many failures return HTTP `200 OK` with `success: false` and an error message.

## API Endpoints

Base route prefix: `/api`

### Employee

- `POST /api/Employee/register` (anonymous)
- `POST /api/Employee/login` (anonymous)
- `GET /api/Employee` (auth)
- `GET /api/Employee/{id}` (auth)
- `PUT /api/Employee/{id}` (auth, with role/self restrictions)
- `DELETE /api/Employee/{id}` (`SuperAdmin`)
- `POST /api/Employee/profileImage` (auth, multipart/form-data)

### Department

- `POST /api/Department` (`SuperAdmin`)
- `GET /api/Department` (auth)
- `GET /api/Department/{id}` (auth)
- `PUT /api/Department/{id}` (`SuperAdmin`)
- `DELETE /api/Department/{id}` (`SuperAdmin`)
- `PUT /api/Department/{id}/assign-head` (`SuperAdmin`)

### Leave Type

- `POST /api/LeaveType` (`SuperAdmin`)
- `GET /api/LeaveType` (auth)
- `GET /api/LeaveType/{id}` (auth)
- `PUT /api/LeaveType/{id}` (`SuperAdmin`)
- `DELETE /api/LeaveType/{id}` (`SuperAdmin`)

### Leave Balance

- `POST /api/LeaveBalance/allocate` (`SuperAdmin`, `HR`, `DepartmentHead`)
- `POST /api/LeaveBalance/allocate-all` (`SuperAdmin`)
- `GET /api/LeaveBalance/my` (auth)
- `GET /api/LeaveBalance/employee/{employeeId}` (auth, with access checks)

### Leave Request

- `POST /api/LeaveRequest` (apply leave, auth)
- `GET /api/LeaveRequest/my` (auth)
- `GET /api/LeaveRequest/{id}` (auth)
- `GET /api/LeaveRequest/pending` (auth, role-aware visibility)
- `PUT /api/LeaveRequest/{id}/approve` (auth, role-aware authorization)
- `PUT /api/LeaveRequest/{id}/cancel` (owner only)
- `GET /api/LeaveRequest/all` (`SuperAdmin`)

## Sample Requests

### Register

```http
POST /api/Employee/register
Content-Type: application/json

{
	"employeeCode": "EMP001",
	"userName": "Alice",
	"email": "alice@company.com",
	"password": "Password1",
	"designation": "Software Engineer",
	"dateOfJoining": "2026-01-10",
	"departmentId": 1,
	"managerId": null,
	"role": "Employee"
}
```

### Login

```http
POST /api/Employee/login
Content-Type: application/json

{
	"email": "alice@company.com",
	"password": "Password1"
}
```

### Create Leave Type

```http
POST /api/LeaveType
Authorization: Bearer <token>
Content-Type: application/json

{
	"leaveName": "Casual Leave",
	"maxDaysPerYear": 12,
	"requiresApproval": true
}
```

### Allocate Leave Balance

```http
POST /api/LeaveBalance/allocate
Authorization: Bearer <token>
Content-Type: application/json

{
	"employeeId": 2,
	"leaveTypeId": 1,
	"totalAllocated": 12,
	"year": 2026
}
```

### Apply Leave

```http
POST /api/LeaveRequest
Authorization: Bearer <token>
Content-Type: application/json

{
	"leaveTypeId": 1,
	"fromDate": "2026-05-12",
	"toDate": "2026-05-14",
	"reason": "Family function"
}
```

### Approve/Reject Leave

```http
PUT /api/LeaveRequest/10/approve
Authorization: Bearer <token>
Content-Type: application/json

{
	"status": "Approved"
}
```

### Upload Profile Image

```http
POST /api/Employee/profileImage
Authorization: Bearer <token>
Content-Type: multipart/form-data

Form fields:
- employeeId: 2
- file: <image file>
```

Supported image MIME types:
- `image/jpeg`
- `image/png`
- `image/gif`
- `image/webp`

## Business Rules Highlights

- Leave application validation:
	- `fromDate <= toDate`
	- no past-dated leave application
	- no overlap with existing non-rejected/non-cancelled requests
	- enough balance required
- If leave type does not require approval, leave is auto-approved and balance is deducted immediately.
- Approving an already pending request deducts balance.
- Cancelling an approved request restores used leave days.
- Department deletion is blocked if active employees exist in that department.
- Leave type deletion is blocked if pending requests exist.

## Development Notes

- Swagger is enabled in `Development` environment.
- CORS is currently permissive (`AllowAnyOrigin`, `AllowAnyHeader`, `AllowAnyMethod`).
- JSON serialization uses camelCase and ignores nulls in responses.
- Retry policy is enabled for PostgreSQL connection failures.

## Recommended Next Improvements

- Return proper HTTP status codes for failures instead of always `200`.
- Add refresh tokens / configurable JWT lifetime.
- Add FluentValidation or centralized model validation responses.
- Add unit and integration tests for approval workflows.
- Add paging/filtering for list endpoints.
- Add structured logging and exception middleware.





***Forntend format to hit uploadImage Api:***

```
const formData = new FormData();
formData.append('employeeId', 123);
formData.append('file', imageFile);

const response = await axios.post('/api/employee/profileImage', formData, {
  headers: {
    'Content-Type': 'multipart/form-data',
    'Authorization': `Bearer ${token}`
  }
});
```