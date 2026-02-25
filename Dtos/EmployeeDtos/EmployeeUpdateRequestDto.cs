namespace EmpLeave.Dtos.EmployeeDtos;

public class EmployeeUpdateRequestDto
{
    public string? UserName { get; set; }
    public string? Designation { get; set; }

    public string? ImageUrl { get; set; }
    public int? DepartmentId { get; set; }
    public int? ManagerId { get; set; }

    [System.ComponentModel.DataAnnotations.RegularExpression("^(Employee|Manager|HR|DepartmentHead|SuperAdmin)$",
        ErrorMessage = "Role must be 'Employee', 'Manager', 'HR', 'DepartmentHead', or 'SuperAdmin'")]
    public string? Role { get; set; }
}
