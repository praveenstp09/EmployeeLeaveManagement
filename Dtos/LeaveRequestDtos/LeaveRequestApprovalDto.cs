using System.ComponentModel.DataAnnotations;

namespace EmpLeave.Dtos.LeaveRequestDtos;

public class LeaveRequestApprovalDto
{
    [Required(ErrorMessage = "Status is required")]
    [RegularExpression("^(Approved|Rejected)$", ErrorMessage = "Status must be 'Approved' or 'Rejected'")]
    public string Status { get; set; } = null!;
}
