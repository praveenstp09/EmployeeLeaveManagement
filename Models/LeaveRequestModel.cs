namespace EmpLeave.Models;

public class LeaveRequestModel
{
    public int LeaveRequestId { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalDays { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public EmployeeModel Employee { get; set; } = null!;
    public LeaveTypeModel LeaveType { get; set; } = null!;
    public EmployeeModel? ApprovedBy { get; set; }
}
