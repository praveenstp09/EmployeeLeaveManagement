using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmpLeave.Config;
using EmpLeave.Models;
using EmpLeave.Services;
using EmpLeave.Dtos;

namespace EmpLeave.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly EmployeeLeaveDbContext _db;
        private readonly TokenService _tokenService;

        public EmployeeController(EmployeeLeaveDbContext db, TokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] EmployeeRegisterRequest request)
        {
            try
            {
                var existingEmployee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.Email == request.Email || e.EmployeeCode == request.EmployeeCode);

                if (existingEmployee != null)
                    return Ok(new ApiResponse<object> { Success = false, Message = "Employee already exists" });

                if (string.IsNullOrEmpty(request.Email) || !request.Email.Contains('@'))
                    return Ok(new ApiResponse<object> { Success = false, Message = "Please enter a valid email" });


                if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 8)
                    return Ok(new ApiResponse<object> { Success = false, Message = "Please enter a strong password" });

                var department = await _db.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentId == request.DepartmentId);

                if (department == null)
                    return Ok(new ApiResponse<object> { Success = false, Message = "Department not found" });

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var newEmployee = new Employee
                {
                    EmployeeCode = request.EmployeeCode,
                    UserName = request.UserName,
                    Email = request.Email,
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

                var token = _tokenService.GenerateToken(int.Parse(createdEmployee.EmployeeId), createdEmployee.Email, createdEmployee.UserName);
                return Ok(new ApiResponse<EmployeeResponse>
                {
                    Success = true,
                    Message = "Employee registered successfully",
                    Token = token,
                    Data = new EmployeeResponse
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
                return Ok(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployee(string id)
        {
            try
            {
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == id);

                if (employee == null)
                    return Ok(new ApiResponse<object> { Success = false, Message = "Employee not found" });

                return Ok(new ApiResponse<EmployeeResponse>
                {
                    Success = true,
                    Data = new EmployeeResponse
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
                return Ok(new ApiResponse<object> { Success = false, Message = ex.Message });
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

                var employeeResponses = employees.Select(e => new EmployeeResponse
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeCode = e.EmployeeCode,
                    UserName = e.UserName,
                    Email = e.Email
                }).ToList();

                return Ok(new ApiResponse<List<EmployeeResponse>>
                {
                    Success = true,
                    Data = employeeResponses
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(string id, [FromBody] EmployeeUpdateRequest request)
        {
            try
            {
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == id);

                if (employee == null)
                    return Ok(new ApiResponse<object> { Success = false, Message = "Employee not found" });

                employee.UserName = request.UserName ?? employee.UserName;
                employee.Designation = request.Designation ?? employee.Designation;
                employee.DepartmentId = request.DepartmentId ?? employee.DepartmentId;
                employee.ManagerId = request.ManagerId ?? employee.ManagerId;

                _db.Employees.Update(employee);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponse<object> 
                { 
                    Success = true, 
                    Message = "Employee updated successfully" 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateEmployee(string id)
        {
            try
            {
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == id);

                if (employee == null)
                    return Ok(new ApiResponse<object> { Success = false, Message = "Employee not found" });

                employee.IsActive = false;
                _db.Employees.Update(employee);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponse<object> 
                { 
                    Success = true, 
                    Message = "Employee deactivated successfully" 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }
    }
}
