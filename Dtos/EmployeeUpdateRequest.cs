namespace EmpLeave.Dtos;

public class EmployeeUpdateRequest
{
    public string? UserName { get; set; }
    public string? Designation { get; set; }
    public string? DepartmentId { get; set; }
    public string? ManagerId { get; set; }
}
