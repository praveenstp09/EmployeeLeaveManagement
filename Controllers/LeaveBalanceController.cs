using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmpLeave.Config;
using EmpLeave.Models;
using EmpLeave.Dtos.ApiDto;
using EmpLeave.Dtos.LeaveBalanceDtos;

namespace EmpLeave.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveBalanceController : ControllerBase
    {
        private readonly EmployeeLeaveDbContext _db;

        public LeaveBalanceController(EmployeeLeaveDbContext db)
        {
            _db = db;
        }

        [HttpPost("allocate")]
        public async Task<IActionResult> AllocateLeaveBalance([FromBody] LeaveBalanceAllocateDto request)
        {
            try
            {
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId && e.IsActive);

                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found or inactive" });

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

                var existing = await _db.LeaveBalances
                    .FirstOrDefaultAsync(lb => lb.EmployeeId == request.EmployeeId && lb.LeaveTypeId == request.LeaveTypeId);

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
                var employeeId = HttpContext.Items["EmployeeId"] as int?;
                if (employeeId == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });

                var balances = await _db.LeaveBalances
                    .Where(lb => lb.EmployeeId == employeeId)
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
                int empId = int.Parse(employeeId);
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == empId);

                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found" });

                var balances = await _db.LeaveBalances
                    .Where(lb => lb.EmployeeId == empId)
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

        [HttpPost("allocate-all")]
        public async Task<IActionResult> AllocateBalancesForAllEmployees()
        {
            try
            {
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
                            .FirstOrDefaultAsync(lb => lb.EmployeeId == empId && lb.LeaveTypeId == lt.LeaveTypeId);

                        if (existing == null)
                        {
                            _db.LeaveBalances.Add(new LeaveBalanceModel
                            {
                                EmployeeId = empId,
                                LeaveTypeId = lt.LeaveTypeId,
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
                TotalAllocated = model.TotalAllocated,
                Used = model.Used
            };
        }
    }
}
