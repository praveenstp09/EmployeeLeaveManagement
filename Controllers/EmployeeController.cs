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

                // You should store and check hashed passwords in production
                // For now, assuming password is stored hashed
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, employee.Password);
                if (!isPasswordValid)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Invalid credentials" });

                var token = _tokenService.GenerateToken(employee.EmployeeId, employee.Email, employee.UserName);
                return Ok(new ApiResponseDto<EmployeeResponseDto>
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    Data = new EmployeeResponseDto
                    {
                        EmployeeId = employee.EmployeeId,
                        EmployeeCode = employee.EmployeeCode,
                        UserName = employee.UserName,
                        Email = employee.Email
                    }
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

                //if (string.IsNullOrEmpty(request.Email) || !request.Email.Contains('@'))
                //    return Ok(new ApiResponseDto<object> { Success = false, Message = "Please enter a valid email" });


                //if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 8)
                //    return Ok(new ApiResponseDto<object> { Success = false, Message = "Please enter a strong password" });

                var department = await _db.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentId == request.DepartmentId);

                if (department == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Department not found" });

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
                    DepartmentId = request.DepartmentId,
                    ManagerId = request.ManagerId
                };

                _db.Employees.Add(newEmployee);
                await _db.SaveChangesAsync();

                var createdEmployee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.Email == request.Email);

                if (createdEmployee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Failed to create employee" });

                var token = _tokenService.GenerateToken(createdEmployee.EmployeeId, createdEmployee.Email, createdEmployee.UserName);
                return Ok(new ApiResponseDto<EmployeeResponseDto>
                {
                    Success = true,
                    Message = "Employee registered successfully",
                    Token = token,
                    Data = new EmployeeResponseDto
                    {
                        EmployeeId = createdEmployee.EmployeeId,
                        EmployeeCode = createdEmployee.EmployeeCode,
                        UserName = createdEmployee.UserName,
                        Email = createdEmployee.Email
                    }
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
                int Id = int.Parse(id);
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == Id);

                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found" });

                return Ok(new ApiResponseDto<EmployeeResponseDto>
                {
                    Success = true,
                    Data = new EmployeeResponseDto
                    {
                        EmployeeId = employee.EmployeeId,
                        EmployeeCode = employee.EmployeeCode,
                        UserName = employee.UserName,
                        Email = employee.Email
                    }
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
                var employees = await _db.Employees
                    .Where(e => e.IsActive)
                    .ToListAsync();

                var employeeResponses = employees.Select(e => new EmployeeResponseDto
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeCode = e.EmployeeCode,
                    UserName = e.UserName,
                    Email = e.Email
                }).ToList();

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
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == Id);

                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found" });

                employee.UserName = request.UserName ?? employee.UserName;
                employee.Designation = request.Designation ?? employee.Designation;
                employee.DepartmentId = request.DepartmentId ?? employee.DepartmentId;
                employee.ManagerId = request.ManagerId ?? employee.ManagerId;

                _db.Employees.Update(employee);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<object> 
                { 
                    Success = true, 
                    Message = "Employee updated successfully" 
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
                // Check if file exists
                if (file == null || file.Length == 0)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "No file uploaded" });

                // Validate file type (only images allowed for profile)
                var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedImageTypes.Contains(file.ContentType))
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Only JPEG, PNG, GIF, and WebP images are allowed" });

                // Check if employee exists
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found" });

                // Upload file to Supabase
                var imageUrl = await _fileStorageService.UploadFileAsync(file, $"profile-images/{employeeId}");

                // Update employee with image URL
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
    }
}
