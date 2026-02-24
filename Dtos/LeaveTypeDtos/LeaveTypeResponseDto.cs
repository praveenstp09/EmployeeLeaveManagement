namespace EmpLeave.Dtos.LeaveTypeDtos;

public class LeaveTypeResponseDto
{
    public int LeaveTypeId { get; set; }
    public string LeaveName { get; set; } = null!;
    public int MaxDaysPerYear { get; set; }
    public bool RequiresApproval { get; set; }
}
