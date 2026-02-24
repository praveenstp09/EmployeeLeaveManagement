using System.ComponentModel.DataAnnotations;

namespace EmpLeave.Dtos.LeaveBalanceDtos;

public class LeaveBalanceAllocateDto
{
    [Required(ErrorMessage = "Employee ID is required")]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Leave type ID is required")]
    public int LeaveTypeId { get; set; }

    [Required(ErrorMessage = "Total allocated days is required")]
    [Range(1, 365, ErrorMessage = "Allocated days must be between 1 and 365")]
    public int TotalAllocated { get; set; }
}
