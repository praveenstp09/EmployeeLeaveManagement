using System.ComponentModel.DataAnnotations;

namespace EmpLeave.Dtos.LeaveTypeDtos;

public class LeaveTypeUpdateDto
{
    [StringLength(100, ErrorMessage = "Leave name cannot exceed 100 characters")]
    public string? LeaveName { get; set; }

    [Range(1, 365, ErrorMessage = "Max days must be between 1 and 365")]
    public int? MaxDaysPerYear { get; set; }

    public bool? RequiresApproval { get; set; }
}
