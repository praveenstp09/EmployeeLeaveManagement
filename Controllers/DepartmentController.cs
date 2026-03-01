using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EmpLeave.Config;
using EmpLeave.Models;
using EmpLeave.Dtos.ApiDto;
using EmpLeave.Dtos.DepartmentDtos;
using System.Security.Claims;

namespace EmpLeave.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepartmentController : ControllerBase
    {
        private readonly EmployeeLeaveDbContext _db;

        public DepartmentController(EmployeeLeaveDbContext db)
        {
            _db = db;
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> CreateDepartment([FromBody] DepartmentRequestDto request)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(request.DepartmentName))
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Department name is required" });

                var existingDepartment = await _db.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentName == request.DepartmentName);

                if (existingDepartment != null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Department already exists" });

                var newDepartment = new DepartmentModel
                {
                    DepartmentName = request.DepartmentName
                };

                _db.Departments.Add(newDepartment);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<DepartmentResponseDto>
                {
                    Success = true,
                    Message = "Department created successfully",
                    Data = MapToResponse(newDepartment)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDepartments()
        {
            try
            {
                var departments = await _db.Departments.ToListAsync();

                return Ok(new ApiResponseDto<List<DepartmentResponseDto>>
                {
                    Success = true,
                    Data = departments.Select(MapToResponse).ToList()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartment(string id)
        {
            try
            {
                int departmentId = int.Parse(id);
                var department = await _db.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentId == departmentId);

                if (department == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Department not found" });

                return Ok(new ApiResponseDto<DepartmentResponseDto>
                {
                    Success = true,
                    Data = MapToResponse(department)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDepartment(string id, [FromBody] DepartmentRequestDto request)
        {
            try
            {

                int departmentId = int.Parse(id);
                var department = await _db.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentId == departmentId);

                if (department == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Department not found" });

                if (string.IsNullOrWhiteSpace(request.DepartmentName))
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Department name is required" });

                var existingDepartment = await _db.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentName == request.DepartmentName && d.DepartmentId != departmentId);

                if (existingDepartment != null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Department name already exists" });

                department.DepartmentName = request.DepartmentName;

                _db.Departments.Update(department);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<DepartmentResponseDto>
                {
                    Success = true,
                    Message = "Department updated successfully",
                    Data = MapToResponse(department)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartment(string id)
        {
            try
            {

                int departmentId = int.Parse(id);
                var department = await _db.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentId == departmentId);

                if (department == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Department not found" });

                var employeesInDepartment = await _db.Employees
                    .Where(e => e.DepartmentId == departmentId && e.IsActive)
                    .CountAsync();

                if (employeesInDepartment > 0)
                    return Ok(new ApiResponseDto<object> 
                    { 
                        Success = false, 
                        Message = "Cannot delete department with active employees" 
                    });

                _db.Departments.Remove(department);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Department deleted successfully"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPut("{id}/assign-head")]
        public async Task<IActionResult> AssignDepartmentHead(string id, [FromBody] AssignDepartmentHeadDto request)
        {
            try
            {

                int departmentId = int.Parse(id);
                var department = await _db.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentId == departmentId);

                if (department == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Department not found" });

                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId && e.IsActive);

                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found or inactive" });

                if (employee.DepartmentId != departmentId)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee does not belong to this department" });

                // Check if this employee already heads another department
                var existingHead = await _db.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentHeadId == request.EmployeeId && d.DepartmentId != departmentId);

                if (existingHead != null)
                    return Ok(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = $"This employee is already the head of '{existingHead.DepartmentName}'"
                    });

                // Remove DepartmentHead role from the previous head
                if (department.DepartmentHeadId != null)
                {
                    var previousHead = await _db.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeId == department.DepartmentHeadId);

                    if (previousHead != null && previousHead.Role == "DepartmentHead")
                    {
                        previousHead.Role = "Employee";
                        _db.Employees.Update(previousHead);
                    }
                }

                // Assign new head
                department.DepartmentHeadId = request.EmployeeId;
                employee.Role = "DepartmentHead";

                _db.Departments.Update(department);
                _db.Employees.Update(employee);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = $"'{employee.UserName}' assigned as head of '{department.DepartmentName}'"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        private static DepartmentResponseDto MapToResponse(DepartmentModel department)
        {
            return new DepartmentResponseDto
            {
                DepartmentId = department.DepartmentId,
                DepartmentName = department.DepartmentName,
                DepartmentHeadId = department.DepartmentHeadId
            };
        }
    }
}
