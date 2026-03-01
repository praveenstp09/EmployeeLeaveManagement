using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EmpLeave.Config;
using EmpLeave.Models;
using EmpLeave.Dtos.ApiDto;
using EmpLeave.Dtos.LeaveBalanceDtos;
using System.Security.Claims;

namespace EmpLeave.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeaveBalanceController : ControllerBase
    {
        private readonly EmployeeLeaveDbContext _db;

        public LeaveBalanceController(EmployeeLeaveDbContext db)
        {
            _db = db;
        }

        [Authorize(Roles = "SuperAdmin,HR,DepartmentHead")]
        [HttpPost("allocate")]
        public async Task<IActionResult> AllocateLeaveBalance([FromBody] LeaveBalanceAllocateDto request)
        {
            try
            {
                var callerRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId && e.IsActive);

                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found or inactive" });

                // HR and DepartmentHead can only allocate for employees in their own department
                if (callerRole is "HR" or "DepartmentHead")
                {
                    var callerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var callerId = callerIdStr != null ? int.Parse(callerIdStr) : (int?)null;
                    var caller = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == callerId);
                    if (caller == null || caller.DepartmentId != employee.DepartmentId)
                        return Ok(new ApiResponseDto<object> { Success = false, Message = "You can only allocate leave for employees in your department" });
                }

                var leaveType = await _db.LeaveTypes
                    .FirstOrDefaultAsync(lt => lt.LeaveTypeId == request.LeaveTypeId);

                if (leaveType == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Leave type not found" });

                if (request.TotalAllocated > leaveType.MaxDaysPerYear)
                    return Ok(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = $"Cannot allocate more than {leaveType.MaxDaysPerYear} days for {leaveType.LeaveName}"
                    });

                int year = request.Year ?? DateTime.UtcNow.Year;

                var existing = await _db.LeaveBalances
                    .FirstOrDefaultAsync(lb => lb.EmployeeId == request.EmployeeId
                        && lb.LeaveTypeId == request.LeaveTypeId
                        && lb.Year == year);

                if (existing != null)
                {
                    existing.TotalAllocated = request.TotalAllocated;
                    _db.LeaveBalances.Update(existing);
                    await _db.SaveChangesAsync();

                    return Ok(new ApiResponseDto<LeaveBalanceResponseDto>
                    {
                        Success = true,
                        Message = "Leave balance updated successfully",
                        Data = MapToResponse(existing, employee.UserName, leaveType.LeaveName)
                    });
                }

                var balance = new LeaveBalanceModel
                {
                    EmployeeId = request.EmployeeId,
                    LeaveTypeId = request.LeaveTypeId,
                    Year = year,
                    TotalAllocated = request.TotalAllocated,
                    Used = 0
                };

                _db.LeaveBalances.Add(balance);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<LeaveBalanceResponseDto>
                {
                    Success = true,
                    Message = "Leave balance allocated successfully",
                    Data = MapToResponse(balance, employee.UserName, leaveType.LeaveName)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyLeaveBalances()
        {
            try
            {
                var employeeIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (employeeIdStr == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });
                int? employeeId = int.Parse(employeeIdStr);

                int currentYear = DateTime.UtcNow.Year;

                var balances = await _db.LeaveBalances
                    .Where(lb => lb.EmployeeId == employeeId && lb.Year == currentYear)
                    .ToListAsync();

                var employee = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
                string employeeName = employee?.UserName ?? "";

                var leaveTypeIds = balances.Select(b => b.LeaveTypeId).Distinct().ToList();
                var leaveTypes = await _db.LeaveTypes
                    .Where(lt => leaveTypeIds.Contains(lt.LeaveTypeId))
                    .ToDictionaryAsync(lt => lt.LeaveTypeId, lt => lt.LeaveName);

                var response = balances.Select(b => MapToResponse(
                    b,
                    employeeName,
                    leaveTypes.GetValueOrDefault(b.LeaveTypeId, "")
                )).ToList();

                return Ok(new ApiResponseDto<List<LeaveBalanceResponseDto>>
                {
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetEmployeeLeaveBalances(string employeeId)
        {
            try
            {
                var callerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var callerRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (callerIdStr == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });
                int? callerId = int.Parse(callerIdStr);

                int empId = int.Parse(employeeId);
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == empId);

                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found" });

                // Allow: self, SuperAdmin, HR in same department, DepartmentHead of same department, or direct manager
                bool isSelf = callerId == empId;
                bool isSuperAdmin = callerRole == "SuperAdmin";
                bool isManager = employee.ManagerId == callerId;
                bool isDeptHR = false;
                bool isDeptHead = false;
                if (callerRole is "HR" or "DepartmentHead")
                {
                    var caller = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == callerId);
                    if (callerRole == "HR") isDeptHR = caller?.DepartmentId == employee.DepartmentId;
                    if (callerRole == "DepartmentHead") isDeptHead = caller?.DepartmentId == employee.DepartmentId;
                }

                if (!isSelf && !isSuperAdmin && !isManager && !isDeptHR && !isDeptHead)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "You are not authorized to view this employee's leave balances" });

                int currentYear = DateTime.UtcNow.Year;

                var balances = await _db.LeaveBalances
                    .Where(lb => lb.EmployeeId == empId && lb.Year == currentYear)
                    .ToListAsync();

                var leaveTypeIds = balances.Select(b => b.LeaveTypeId).Distinct().ToList();
                var leaveTypes = await _db.LeaveTypes
                    .Where(lt => leaveTypeIds.Contains(lt.LeaveTypeId))
                    .ToDictionaryAsync(lt => lt.LeaveTypeId, lt => lt.LeaveName);

                var response = balances.Select(b => MapToResponse(
                    b,
                    employee.UserName,
                    leaveTypes.GetValueOrDefault(b.LeaveTypeId, "")
                )).ToList();

                return Ok(new ApiResponseDto<List<LeaveBalanceResponseDto>>
                {
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("allocate-all")]
        public async Task<IActionResult> AllocateBalancesForAllEmployees()
        {
            try
            {

                int currentYear = DateTime.UtcNow.Year;

                var activeEmployees = await _db.Employees
                    .Where(e => e.IsActive)
                    .Select(e => e.EmployeeId)
                    .ToListAsync();

                var leaveTypes = await _db.LeaveTypes.ToListAsync();

                if (leaveTypes.Count == 0)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "No leave types configured" });

                int allocated = 0;

                foreach (var empId in activeEmployees)
                {
                    foreach (var lt in leaveTypes)
                    {
                        var existing = await _db.LeaveBalances
                            .FirstOrDefaultAsync(lb => lb.EmployeeId == empId
                                && lb.LeaveTypeId == lt.LeaveTypeId
                                && lb.Year == currentYear);

                        if (existing == null)
                        {
                            _db.LeaveBalances.Add(new LeaveBalanceModel
                            {
                                EmployeeId = empId,
                                LeaveTypeId = lt.LeaveTypeId,
                                Year = currentYear,
                                TotalAllocated = lt.MaxDaysPerYear,
                                Used = 0
                            });
                            allocated++;
                        }
                    }
                }

                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = $"Leave balances allocated for {activeEmployees.Count} employees. {allocated} new balance records created."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        private static LeaveBalanceResponseDto MapToResponse(LeaveBalanceModel model, string employeeName, string leaveTypeName)
        {
            return new LeaveBalanceResponseDto
            {
                LeaveBalanceId = model.LeaveBalanceId,
                EmployeeId = model.EmployeeId,
                EmployeeName = employeeName,
                LeaveTypeId = model.LeaveTypeId,
                LeaveTypeName = leaveTypeName,
                Year = model.Year,
                TotalAllocated = model.TotalAllocated,
                Used = model.Used
            };
        }
    }
}
