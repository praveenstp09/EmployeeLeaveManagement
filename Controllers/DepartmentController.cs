using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmpLeave.Config;
using EmpLeave.Models;
using EmpLeave.Dtos;

namespace EmpLeave.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentController : ControllerBase
    {
        private readonly EmployeeLeaveDbContext _db;

        public DepartmentController(EmployeeLeaveDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepartment([FromBody] DepartmentModel request)
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

                return Ok(new ApiResponseDto<DepartmentModel>
                {
                    Success = true,
                    Message = "Department created successfully",
                    Data = newDepartment
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

                return Ok(new ApiResponseDto<List<DepartmentModel>>
                {
                    Success = true,
                    Data = departments
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

                return Ok(new ApiResponseDto<DepartmentModel>
                {
                    Success = true,
                    Data = department
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDepartment(string id, [FromBody] DepartmentModel request)
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

                return Ok(new ApiResponseDto<DepartmentModel>
                {
                    Success = true,
                    Message = "Department updated successfully",
                    Data = department
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

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
    }
}
