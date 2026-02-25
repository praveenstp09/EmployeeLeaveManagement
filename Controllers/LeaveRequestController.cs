using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmpLeave.Config;
using EmpLeave.Models;
using EmpLeave.Dtos.ApiDto;
using EmpLeave.Dtos.LeaveRequestDtos;

namespace EmpLeave.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveRequestController : ControllerBase
    {
        private readonly EmployeeLeaveDbContext _db;

        public LeaveRequestController(EmployeeLeaveDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> ApplyLeave([FromBody] LeaveRequestCreateDto request)
        {
            try
            {
                var employeeId = HttpContext.Items["EmployeeId"] as int?;
                if (employeeId == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });

                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.IsActive);

                if (employee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee not found or inactive" });

                var leaveType = await _db.LeaveTypes
                    .FirstOrDefaultAsync(lt => lt.LeaveTypeId == request.LeaveTypeId);

                if (leaveType == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Leave type not found" });

                if (request.FromDate > request.ToDate)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "From date cannot be after to date" });

                if (request.FromDate.Date < DateTime.UtcNow.Date)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Cannot apply leave for past dates" });

                int totalDays = (request.ToDate.Date - request.FromDate.Date).Days + 1;
                int leaveYear = request.FromDate.Year;

                // Check leave balance for the year
                var balance = await _db.LeaveBalances
                    .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId
                        && lb.LeaveTypeId == request.LeaveTypeId
                        && lb.Year == leaveYear);

                if (balance == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "No leave balance allocated for this leave type in the requested year" });

                int remaining = balance.TotalAllocated - balance.Used;
                if (totalDays > remaining)
                    return Ok(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = $"Insufficient leave balance. Available: {remaining} days, Requested: {totalDays} days"
                    });

                // Check for overlapping leave requests
                var overlapping = await _db.LeaveRequests
                    .AnyAsync(lr => lr.EmployeeId == employeeId
                        && lr.Status != "Rejected" && lr.Status != "Cancelled"
                        && lr.FromDate <= request.ToDate
                        && lr.ToDate >= request.FromDate);

                if (overlapping)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "You already have a leave request overlapping with these dates" });

                var leaveRequest = new LeaveRequestModel
                {
                    EmployeeId = employeeId.Value,
                    LeaveTypeId = request.LeaveTypeId,
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    TotalDays = totalDays,
                    Reason = request.Reason,
                    Status = leaveType.RequiresApproval ? "Pending" : "Approved",
                    CreatedAt = DateTime.UtcNow
                };

                _db.LeaveRequests.Add(leaveRequest);

                // Auto-approve: deduct balance immediately if no approval required
                if (!leaveType.RequiresApproval)
                {
                    balance.Used += totalDays;
                    _db.LeaveBalances.Update(balance);
                }

                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<LeaveRequestResponseDto>
                {
                    Success = true,
                    Message = leaveType.RequiresApproval
                        ? "Leave request submitted for approval"
                        : "Leave approved automatically",
                    Data = MapToResponse(leaveRequest, employee.UserName, leaveType.LeaveName)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyLeaveRequests()
        {
            try
            {
                var employeeId = HttpContext.Items["EmployeeId"] as int?;
                if (employeeId == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });

                var requests = await _db.LeaveRequests
                    .Where(lr => lr.EmployeeId == employeeId)
                    .OrderByDescending(lr => lr.CreatedAt)
                    .ToListAsync();

                var leaveTypeIds = requests.Select(r => r.LeaveTypeId).Distinct().ToList();
                var leaveTypes = await _db.LeaveTypes
                    .Where(lt => leaveTypeIds.Contains(lt.LeaveTypeId))
                    .ToDictionaryAsync(lt => lt.LeaveTypeId, lt => lt.LeaveName);

                var employee = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
                string employeeName = employee?.UserName ?? "";

                var response = requests.Select(r => MapToResponse(
                    r,
                    employeeName,
                    leaveTypes.GetValueOrDefault(r.LeaveTypeId, "")
                )).ToList();

                return Ok(new ApiResponseDto<List<LeaveRequestResponseDto>>
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
        public async Task<IActionResult> GetLeaveRequest(string id)
        {
            try
            {
                var callerId = HttpContext.Items["EmployeeId"] as int?;
                if (callerId == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });

                int requestId = int.Parse(id);
                var leaveRequest = await _db.LeaveRequests
                    .FirstOrDefaultAsync(lr => lr.LeaveRequestId == requestId);

                if (leaveRequest == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Leave request not found" });

                var employee = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == leaveRequest.EmployeeId);
                var leaveType = await _db.LeaveTypes.FirstOrDefaultAsync(lt => lt.LeaveTypeId == leaveRequest.LeaveTypeId);

                return Ok(new ApiResponseDto<LeaveRequestResponseDto>
                {
                    Success = true,
                    Data = MapToResponse(
                        leaveRequest,
                        employee?.UserName ?? "",
                        leaveType?.LeaveName ?? ""
                    )
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            try
            {
                var approverId = HttpContext.Items["EmployeeId"] as int?;
                var approverRole = HttpContext.Items["Role"] as string;
                if (approverId == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });

                var approver = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == approverId);
                if (approver == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Approver not found" });

                List<int> authorizedEmployeeIds;

                if (approverRole == "SuperAdmin")
                {
                    // SuperAdmin sees all pending requests
                    authorizedEmployeeIds = await _db.Employees
                        .Where(e => e.IsActive && e.EmployeeId != approverId)
                        .Select(e => e.EmployeeId)
                        .ToListAsync();
                }
                else if (approverRole == "DepartmentHead")
                {
                    // DepartmentHead sees pending from Manager, HR, and Employee in their department
                    authorizedEmployeeIds = await _db.Employees
                        .Where(e => e.DepartmentId == approver.DepartmentId
                            && e.IsActive
                            && e.EmployeeId != approverId
                            && (e.Role == "Employee" || e.Role == "Manager" || e.Role == "HR"))
                        .Select(e => e.EmployeeId)
                        .ToListAsync();
                }
                else if (approverRole == "HR")
                {
                    // HR sees pending from Employee role in their department
                    authorizedEmployeeIds = await _db.Employees
                        .Where(e => e.DepartmentId == approver.DepartmentId
                            && e.IsActive
                            && e.Role == "Employee")
                        .Select(e => e.EmployeeId)
                        .ToListAsync();
                }
                else if (approverRole == "Manager")
                {
                    // Manager sees pending from their direct subordinates who are Employee role
                    authorizedEmployeeIds = await _db.Employees
                        .Where(e => e.ManagerId == approverId
                            && e.IsActive
                            && e.Role == "Employee")
                        .Select(e => e.EmployeeId)
                        .ToListAsync();
                }
                else
                {
                    // Regular employees cannot approve
                    authorizedEmployeeIds = [];
                }

                if (authorizedEmployeeIds.Count == 0)
                    return Ok(new ApiResponseDto<List<LeaveRequestResponseDto>>
                    {
                        Success = true,
                        Message = "No pending requests to review",
                        Data = new List<LeaveRequestResponseDto>()
                    });

                var pendingRequests = await _db.LeaveRequests
                    .Where(lr => authorizedEmployeeIds.Contains(lr.EmployeeId) && lr.Status == "Pending")
                    .OrderBy(lr => lr.CreatedAt)
                    .ToListAsync();

                var employeeIds = pendingRequests.Select(r => r.EmployeeId).Distinct().ToList();
                var employees = await _db.Employees
                    .Where(e => employeeIds.Contains(e.EmployeeId))
                    .ToDictionaryAsync(e => e.EmployeeId, e => e.UserName);

                var leaveTypeIds = pendingRequests.Select(r => r.LeaveTypeId).Distinct().ToList();
                var leaveTypes = await _db.LeaveTypes
                    .Where(lt => leaveTypeIds.Contains(lt.LeaveTypeId))
                    .ToDictionaryAsync(lt => lt.LeaveTypeId, lt => lt.LeaveName);

                var response = pendingRequests.Select(r => MapToResponse(
                    r,
                    employees.GetValueOrDefault(r.EmployeeId, ""),
                    leaveTypes.GetValueOrDefault(r.LeaveTypeId, "")
                )).ToList();

                return Ok(new ApiResponseDto<List<LeaveRequestResponseDto>>
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

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveOrRejectLeave(string id, [FromBody] LeaveRequestApprovalDto request)
        {
            try
            {
                var approverId = HttpContext.Items["EmployeeId"] as int?;
                var approverRole = HttpContext.Items["Role"] as string;
                if (approverId == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });

                int requestId = int.Parse(id);
                var leaveRequest = await _db.LeaveRequests
                    .FirstOrDefaultAsync(lr => lr.LeaveRequestId == requestId);

                if (leaveRequest == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Leave request not found" });

                if (leaveRequest.Status != "Pending")
                    return Ok(new ApiResponseDto<object> { Success = false, Message = $"Leave request is already {leaveRequest.Status}" });

                var requestingEmployee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == leaveRequest.EmployeeId);

                if (requestingEmployee == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Requesting employee not found" });

                // Get requester's department to check if approver is their dept head
                var requesterDept = await _db.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentId == requestingEmployee.DepartmentId);

                bool isSuperAdmin = approverRole == "SuperAdmin";
                bool isDeptHead = approverRole == "DepartmentHead" && requesterDept?.DepartmentHeadId == approverId;

                // Apply hierarchical approval rules
                bool canApprove;

                if (requestingEmployee.Role == "DepartmentHead")
                {
                    // DepartmentHead leave → only SuperAdmin
                    canApprove = isSuperAdmin;
                }
                else if (requestingEmployee.Role is "Manager" or "HR")
                {
                    // Manager/HR leave → DepartmentHead (same dept) or SuperAdmin
                    canApprove = isDeptHead || isSuperAdmin;
                }
                else
                {
                    // Employee leave → Manager, HR (same dept), DepartmentHead (same dept), SuperAdmin
                    bool isManager = requestingEmployee.ManagerId == approverId;
                    bool isDeptHR = false;
                    if (approverRole == "HR")
                    {
                        var approver = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == approverId);
                        isDeptHR = approver?.DepartmentId == requestingEmployee.DepartmentId;
                    }
                    canApprove = isManager || isDeptHR || isDeptHead || isSuperAdmin;
                }

                if (!canApprove)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "You are not authorized to approve this request" });

                leaveRequest.Status = request.Status;
                leaveRequest.ApprovedById = approverId;
                leaveRequest.ApprovedAt = DateTime.UtcNow;

                // If approved, deduct from leave balance
                if (request.Status == "Approved")
                {
                    int leaveYear = leaveRequest.FromDate.Year;
                    var balance = await _db.LeaveBalances
                        .FirstOrDefaultAsync(lb => lb.EmployeeId == leaveRequest.EmployeeId
                            && lb.LeaveTypeId == leaveRequest.LeaveTypeId
                            && lb.Year == leaveYear);

                    if (balance == null)
                        return Ok(new ApiResponseDto<object> { Success = false, Message = "Employee has no leave balance for this leave type" });

                    int remaining = balance.TotalAllocated - balance.Used;
                    if (leaveRequest.TotalDays > remaining)
                        return Ok(new ApiResponseDto<object>
                        {
                            Success = false,
                            Message = $"Insufficient leave balance. Available: {remaining} days, Requested: {leaveRequest.TotalDays} days"
                        });

                    balance.Used += leaveRequest.TotalDays;
                    _db.LeaveBalances.Update(balance);
                }

                _db.LeaveRequests.Update(leaveRequest);
                await _db.SaveChangesAsync();

                var leaveType = await _db.LeaveTypes.FirstOrDefaultAsync(lt => lt.LeaveTypeId == leaveRequest.LeaveTypeId);

                return Ok(new ApiResponseDto<LeaveRequestResponseDto>
                {
                    Success = true,
                    Message = $"Leave request {request.Status.ToLower()} successfully",
                    Data = MapToResponse(
                        leaveRequest,
                        requestingEmployee.UserName,
                        leaveType?.LeaveName ?? ""
                    )
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelLeaveRequest(string id)
        {
            try
            {
                var employeeId = HttpContext.Items["EmployeeId"] as int?;
                if (employeeId == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Unauthorized" });

                int requestId = int.Parse(id);
                var leaveRequest = await _db.LeaveRequests
                    .FirstOrDefaultAsync(lr => lr.LeaveRequestId == requestId && lr.EmployeeId == employeeId);

                if (leaveRequest == null)
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Leave request not found" });

                if (leaveRequest.Status == "Cancelled")
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Leave request is already cancelled" });

                if (leaveRequest.Status == "Rejected")
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Cannot cancel a rejected leave request" });

                // If it was approved, restore leave balance
                if (leaveRequest.Status == "Approved")
                {
                    int leaveYear = leaveRequest.FromDate.Year;
                    var balance = await _db.LeaveBalances
                        .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId
                            && lb.LeaveTypeId == leaveRequest.LeaveTypeId
                            && lb.Year == leaveYear);

                    if (balance != null)
                    {
                        balance.Used = Math.Max(0, balance.Used - leaveRequest.TotalDays);
                        _db.LeaveBalances.Update(balance);
                    }
                }

                leaveRequest.Status = "Cancelled";
                _db.LeaveRequests.Update(leaveRequest);
                await _db.SaveChangesAsync();

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Leave request cancelled successfully"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok(new ApiResponseDto<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllLeaveRequests()
        {
            try
            {
                var callerRole = HttpContext.Items["Role"] as string;
                if (callerRole != "SuperAdmin")
                    return Ok(new ApiResponseDto<object> { Success = false, Message = "Only SuperAdmin can view all leave requests" });

                var requests = await _db.LeaveRequests
                    .OrderByDescending(lr => lr.CreatedAt)
                    .ToListAsync();

                var employeeIds = requests.Select(r => r.EmployeeId).Distinct().ToList();
                var employees = await _db.Employees
                    .Where(e => employeeIds.Contains(e.EmployeeId))
                    .ToDictionaryAsync(e => e.EmployeeId, e => e.UserName);

                var leaveTypeIds = requests.Select(r => r.LeaveTypeId).Distinct().ToList();
                var leaveTypes = await _db.LeaveTypes
                    .Where(lt => leaveTypeIds.Contains(lt.LeaveTypeId))
                    .ToDictionaryAsync(lt => lt.LeaveTypeId, lt => lt.LeaveName);

                var response = requests.Select(r => MapToResponse(
                    r,
                    employees.GetValueOrDefault(r.EmployeeId, ""),
                    leaveTypes.GetValueOrDefault(r.LeaveTypeId, "")
                )).ToList();

                return Ok(new ApiResponseDto<List<LeaveRequestResponseDto>>
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

        private static LeaveRequestResponseDto MapToResponse(LeaveRequestModel model, string employeeName, string leaveTypeName)
        {
            return new LeaveRequestResponseDto
            {
                LeaveRequestId = model.LeaveRequestId,
                EmployeeId = model.EmployeeId,
                EmployeeName = employeeName,
                LeaveTypeId = model.LeaveTypeId,
                LeaveTypeName = leaveTypeName,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                TotalDays = model.TotalDays,
                Reason = model.Reason,
                Status = model.Status,
                ApprovedById = model.ApprovedById,
                ApprovedAt = model.ApprovedAt,
                CreatedAt = model.CreatedAt
            };
        }
    }
}
