using System.ComponentModel.DataAnnotations;

namespace EmpLeave.Dtos.LeaveTypeDtos;

public class LeaveTypeCreateDto
{
    [Required(ErrorMessage = "Leave name is required")]
    [StringLength(100, ErrorMessage = "Leave name cannot exceed 100 characters")]
    public string LeaveName { get; set; } = null!;

    [Required(ErrorMessage = "Max days per year is required")]
    [Range(1, 365, ErrorMessage = "Max days must be between 1 and 365")]
    public int MaxDaysPerYear { get; set; }

    public bool RequiresApproval { get; set; } = true;
}
