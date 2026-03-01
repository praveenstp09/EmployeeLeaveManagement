using System.ComponentModel.DataAnnotations;

namespace EmpLeave.Dtos.DepartmentDtos;

public class DepartmentRequestDto
{
    [Required(ErrorMessage = "Department name is required")]
    public string DepartmentName { get; set; } = null!;
}
