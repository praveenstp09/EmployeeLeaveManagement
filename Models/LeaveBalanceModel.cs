namespace EmpLeave.Models;

public class LeaveBalanceModel
{
    public int LeaveBalanceId { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public int TotalAllocated { get; set; }
    public int Used { get; set; }
}
