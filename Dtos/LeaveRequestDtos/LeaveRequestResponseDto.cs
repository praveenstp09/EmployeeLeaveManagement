namespace EmpLeave.Dtos.LeaveRequestDtos;

public class LeaveRequestResponseDto
{
    public int LeaveRequestId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = null!;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalDays { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
