namespace EmpLeave.Models;

public class LeaveBalance
{
    public string LeaveBalanceId { get; set; } = null!;
    public string EmployeeId { get; set; } = null!;
    public string LeaveTypeId { get; set; } = null!;
    public int TotalAllocated { get; set; }
    public int Used { get; set; }
}
