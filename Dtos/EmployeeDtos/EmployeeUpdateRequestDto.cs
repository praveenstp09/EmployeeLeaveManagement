namespace EmpLeave.Dtos.EmployeeDtos;

public class EmployeeUpdateRequestDto
{
    public string? UserName { get; set; }
    public string? Designation { get; set; }
    public int? DepartmentId { get; set; }
    public int? ManagerId { get; set; }
}
