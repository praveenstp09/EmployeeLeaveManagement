using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmpLeave.Config;
using EmpLeave.Models;
using EmpLeave.Dtos.ApiDto;
using EmpLeave.Dtos.LeaveTypeDtos;

namespace EmpLeave.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveTypeController : ControllerBase
    {
        private readonly EmployeeLeaveDbContext _db;

        public LeaveTypeController(EmployeeLeaveDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> CreateLeaveType([FromBody] LeaveTypeCreateDto request)
        {
            try
            {
                var callerRole = HttpContext.Items["Role"] as string;
                if (callerRole != "SuperAdmin")
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Only SuperAdmin can create leave types" });

                var existing = await _db.LeaveTypes
                    .FirstOrDefaultAsync(lt => lt.LeaveName == request.LeaveName);

                if (existing != null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Leave type already exists" });

                var leaveType = new LeaveTypeModel
                {
                    LeaveName = request.LeaveName,
                    MaxDaysPerYear = request.MaxDaysPerYear,
                    RequiresApproval = request.RequiresApproval
                };

                _db.LeaveTypes.Add(leaveType);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<LeaveTypeResponseDto>
                {
                    Success = true,
                    Message = "Leave type created successfully",
                    Data = MapToResponse(leaveType)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLeaveTypes()
        {
            try
            {
                var employeeId = HttpContext.Items["EmployeeId"] as int?;
                if (employeeId == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });

                var leaveTypes = await _db.LeaveTypes.ToListAsync();

                var response = leaveTypes.Select(MapToResponse).ToList();

                return Ok(new ApiResponseDto<List<LeaveTypeResponseDto>>
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLeaveType(string id)
        {
            try
            {
                var employeeId = HttpContext.Items["EmployeeId"] as int?;
                if (employeeId == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });

                int leaveTypeId = int.Parse(id);
                var leaveType = await _db.LeaveTypes
                    .FirstOrDefaultAsync(lt => lt.LeaveTypeId == leaveTypeId);

                if (leaveType == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Leave type not found" });

                return Ok(new ApiResponseDto<LeaveTypeResponseDto>
                {
                    Success = true,
                    Data = MapToResponse(leaveType)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLeaveType(string id, [FromBody] LeaveTypeUpdateDto request)
        {
            try
            {
                var callerRole = HttpContext.Items["Role"] as string;
                if (callerRole != "SuperAdmin")
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Only SuperAdmin can update leave types" });

                int leaveTypeId = int.Parse(id);
                var leaveType = await _db.LeaveTypes
                    .FirstOrDefaultAsync(lt => lt.LeaveTypeId == leaveTypeId);

                if (leaveType == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Leave type not found" });

                if (!string.IsNullOrWhiteSpace(request.LeaveName))
                {
                    var duplicate = await _db.LeaveTypes
                        .FirstOrDefaultAsync(lt => lt.LeaveName == request.LeaveName && lt.LeaveTypeId != leaveTypeId);

                    if (duplicate != null)
                        return Ok(new ApiResponseDto<object> { Success = false, Message = "Leave type name already exists" });

                    leaveType.LeaveName = request.LeaveName;
                }

                leaveType.MaxDaysPerYear = request.MaxDaysPerYear ?? leaveType.MaxDaysPerYear;
                leaveType.RequiresApproval = request.RequiresApproval ?? leaveType.RequiresApproval;

                _db.LeaveTypes.Update(leaveType);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<LeaveTypeResponseDto>
                {
                    Success = true,
                    Message = "Leave type updated successfully",
                    Data = MapToResponse(leaveType)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLeaveType(string id)
        {
            try
            {
                var callerRole = HttpContext.Items["Role"] as string;
                if (callerRole != "SuperAdmin")
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Only SuperAdmin can delete leave types" });

                int leaveTypeId = int.Parse(id);
                var leaveType = await _db.LeaveTypes
                    .FirstOrDefaultAsync(lt => lt.LeaveTypeId == leaveTypeId);

                if (leaveType == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Leave type not found" });

                var activeRequests = await _db.LeaveRequests
                    .AnyAsync(lr => lr.LeaveTypeId == leaveTypeId && lr.Status == "Pending");

                if (activeRequests)
                    return Ok(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Cannot delete leave type with pending requests"
                    });

                _db.LeaveTypes.Remove(leaveType);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Leave type deleted successfully"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        private static LeaveTypeResponseDto MapToResponse(LeaveTypeModel model)
        {
            return new LeaveTypeResponseDto
            {
                LeaveTypeId = model.LeaveTypeId,
                LeaveName = model.LeaveName,
                MaxDaysPerYear = model.MaxDaysPerYear,
                RequiresApproval = model.RequiresApproval
            };
        }
    }
}
