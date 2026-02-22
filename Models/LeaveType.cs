namespace EmpLeave.Models;

public class LeaveType
{
    public string LeaveTypeId { get; set; } = null!;
    public string LeaveName { get; set; } = null!;
    public int MaxDaysPerYear { get; set; }
    public bool RequiresApproval { get; set; }
}
