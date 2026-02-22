namespace EmpLeave.Models;

public class Employee
{
    public string EmployeeId { get; set; } = null!;
    public string EmployeeCode { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Designation { get; set; } = null!;
    public DateTime DateOfJoining { get; set; }
    public bool IsActive { get; set; }
    public string DepartmentId { get; set; } = null!;
    public string? ManagerId { get; set; }
}
