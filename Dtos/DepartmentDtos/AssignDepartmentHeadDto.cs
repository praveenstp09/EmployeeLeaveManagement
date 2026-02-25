using System.ComponentModel.DataAnnotations;

namespace EmpLeave.Dtos.DepartmentDtos;

public class AssignDepartmentHeadDto
{
    [Required(ErrorMessage = "Employee ID is required")]
    public int EmployeeId { get; set; }
}
