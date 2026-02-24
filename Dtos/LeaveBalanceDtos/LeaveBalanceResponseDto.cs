namespace EmpLeave.Dtos.LeaveBalanceDtos;

public class LeaveBalanceResponseDto
{
    public int LeaveBalanceId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = null!;
    public int TotalAllocated { get; set; }
    public int Used { get; set; }
    public int Remaining => TotalAllocated - Used;
}
