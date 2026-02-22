namespace EmpLeave.Models;

public class LeaveRequest
{
    public string LeaveRequestId { get; set; } = null!;
    public string EmployeeId { get; set; } = null!;
    public string LeaveTypeId { get; set; } = null!;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalDays { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
