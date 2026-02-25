using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmpLeave.Config;
using EmpLeave.Models;
using EmpLeave.Services;
using EmpLeave.Dtos.EmployeeDtos;
using EmpLeave.Dtos.ApiDto;
using EmpLeave.Services.SupabaseServices;

namespace EmpLeave.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly EmployeeLeaveDbContext _db;
        private readonly TokenService _tokenService;
        private readonly IFileStorageService _fileStorageService;

        public EmployeeController(EmployeeLeaveDbContext db, TokenService tokenService, IFileStorageService fileStorageService)
        {
            _db = db;
            _tokenService = tokenService;
            _fileStorageService = fileStorageService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] EmployeeLoginRequestDto request)
        {
            try
            {
                var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Email == request.Email);
                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found" });

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, employee.Password);
                if (!isPasswordValid)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Invalid credentials" });

                var token = _tokenService.GenerateToken(employee.EmployeeId, employee.Email, employee.UserName, employee.Role);
                return Ok(new ApiResponseDto<EmployeeResponseDto>
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    Data = MapToResponse(employee)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] EmployeeRegisterRequestDto request)
        {
            try
            {
                var existingEmployee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.Email == request.Email || e.EmployeeCode == request.EmployeeCode);

                if (existingEmployee != null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee already exists" });

                var department = await _db.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentId == request.DepartmentId);

                if (department == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Department not found" });

                // Role assignment rules:
                // SuperAdmin → can assign any role
                // DepartmentHead → can assign "Manager" to employees in their department
                var callerRole = HttpContext.Items["Role"] as string;
                var callerId = HttpContext.Items["EmployeeId"] as int?;
                string assignedRole = request.Role;

                if (assignedRole != "Employee" && callerRole != "SuperAdmin")
                {
                    if (assignedRole == "Manager" && callerRole == "DepartmentHead" && callerId != null)
                    {
                        var caller = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == callerId);
                        if (caller == null || caller.DepartmentId != request.DepartmentId)
                            assignedRole = "Employee"; // Not same department, demote to Employee
                    }
                    else
                    {
                        assignedRole = "Employee"; // Not authorized for this role
                    }
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var newEmployee = new EmployeeModel
                {
                    EmployeeCode = request.EmployeeCode,
                    UserName = request.UserName,
                    Email = request.Email,
                    Password = hashedPassword,
                    Designation = request.Designation,
                    DateOfJoining = request.DateOfJoining,
                    IsActive = true,
                    Role = assignedRole,
                    DepartmentId = request.DepartmentId,
                    ManagerId = request.ManagerId
                };

                _db.Employees.Add(newEmployee);
                await _db.SaveChangesAsync();

                var token = _tokenService.GenerateToken(newEmployee.EmployeeId, newEmployee.Email, newEmployee.UserName, newEmployee.Role);
                return Ok(new ApiResponseDto<EmployeeResponseDto>
                {
                    Success = true,
                    Message = "Employee registered successfully",
                    Token = token,
                    Data = MapToResponse(newEmployee)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployee(string id)
        {
            try
            {
                var employeeId = HttpContext.Items["EmployeeId"] as int?;
                if (employeeId == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });

                int Id = int.Parse(id);
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == Id);

                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found" });

                return Ok(new ApiResponseDto<EmployeeResponseDto>
                {
                    Success = true,
                    Data = MapToResponse(employee)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEmployees()
        {
            try
            {
                var employeeId = HttpContext.Items["EmployeeId"] as int?;
                if (employeeId == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });

                var employees = await _db.Employees
                    .Where(e => e.IsActive)
                    .ToListAsync();

                var employeeResponses = employees.Select(MapToResponse).ToList();

                return Ok(new ApiResponseDto<List<EmployeeResponseDto>>
                {
                    Success = true,
                    Data = employeeResponses
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(string id, [FromBody] EmployeeUpdateRequestDto request)
        {
            int Id = int.Parse(id);
            try
            {
                var callerId = HttpContext.Items["EmployeeId"] as int?;
                var callerRole = HttpContext.Items["Role"] as string;
                if (callerId == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });

                // Self, SuperAdmin, or DepartmentHead (same dept) can update
                if (callerId != Id && callerRole != "SuperAdmin" && callerRole != "DepartmentHead")
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "You are not authorized to update this employee" });

                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == Id);

                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found" });

                // DepartmentHead can only update employees in their own department
                if (callerRole == "DepartmentHead" && callerId != Id)
                {
                    var caller = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == callerId);
                    if (caller == null || caller.DepartmentId != employee.DepartmentId)
                        return Ok(new ApiResponseDto<object> { Success = false, Message = "You can only update employees in your department" });
                }

                employee.UserName = request.UserName ?? employee.UserName;
                employee.Designation = request.Designation ?? employee.Designation;
                employee.DepartmentId = request.DepartmentId ?? employee.DepartmentId;
                employee.ManagerId = request.ManagerId ?? employee.ManagerId;

                // Role change rules:
                // SuperAdmin → can assign any role
                // DepartmentHead → can promote to "Manager" within their department
                if (request.Role != null)
                {
                    if (callerRole == "SuperAdmin")
                    {
                        employee.Role = request.Role;
                    }
                    else if (callerRole == "DepartmentHead" && request.Role == "Manager")
                    {
                        var caller = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == callerId);
                        if (caller?.DepartmentId == employee.DepartmentId)
                            employee.Role = "Manager";
                    }
                }

                _db.Employees.Update(employee);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<EmployeeResponseDto>
                {
                    Success = true,
                    Message = "Employee updated successfully",
                    Data = MapToResponse(employee)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateEmployee(string id)
        {
            try
            {
                var callerRole = HttpContext.Items["Role"] as string;
                if (callerRole != "SuperAdmin")
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Only SuperAdmin can deactivate employees" });

                int Id = int.Parse(id);
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == Id);

                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found" });

                employee.IsActive = false;
                _db.Employees.Update(employee);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Employee deactivated successfully"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("profileImage")]
        public async Task<IActionResult> UploadProfileImage([FromForm] int employeeId, [FromForm] IFormFile file)
        {
            try
            {
                var callerId = HttpContext.Items["EmployeeId"] as int?;
                var callerRole = HttpContext.Items["Role"] as string;
                if (callerId == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });

                if (callerId != employeeId && callerRole != "SuperAdmin")
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "You are not authorized to update this profile image" });

                if (file == null || file.Length == 0)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "No file uploaded" });

                var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedImageTypes.Contains(file.ContentType))
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Only JPEG, PNG, GIF, and WebP images are allowed" });

                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found" });

                var imageUrl = await _fileStorageService.UploadFileAsync(file, $"profile-images/{employeeId}");

                employee.ImageUrl = imageUrl;
                _db.Employees.Update(employee);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Profile image uploaded successfully",
                    Data = new { EmployeeId = employeeId, ImageUrl = imageUrl }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        private static EmployeeResponseDto MapToResponse(EmployeeModel employee)
        {
            return new EmployeeResponseDto
            {
                EmployeeId = employee.EmployeeId,
                EmployeeCode = employee.EmployeeCode,
                UserName = employee.UserName,
                Email = employee.Email,
                Designation = employee.Designation,
                Role = employee.Role,
                DepartmentId = employee.DepartmentId,
                ManagerId = employee.ManagerId,
                IsActive = employee.IsActive,
                DateOfJoining = employee.DateOfJoining,
                ImageUrl = employee.ImageUrl
            };
        }
    }
}
