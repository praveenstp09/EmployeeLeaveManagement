using System.ComponentModel.DataAnnotations;

namespace EmpLeave.Dtos.LeaveRequestDtos;

public class LeaveRequestCreateDto
{
    [Required(ErrorMessage = "Leave type is required")]
    public int LeaveTypeId { get; set; }

    [Required(ErrorMessage = "From date is required")]
    [DataType(DataType.Date)]
    public DateTime FromDate { get; set; }

    [Required(ErrorMessage = "To date is required")]
    [DataType(DataType.Date)]
    public DateTime ToDate { get; set; }

    [Required(ErrorMessage = "Reason is required")]
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string Reason { get; set; } = null!;
}
