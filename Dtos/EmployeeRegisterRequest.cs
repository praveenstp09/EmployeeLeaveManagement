namespace EmpLeave.Dtos;

public class EmployeeRegisterRequest
{
    public string EmployeeCode { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Designation { get; set; } = null!;
    public DateTime DateOfJoining { get; set; }
    public string DepartmentId { get; set; } = null!;
    public string? ManagerId { get; set; }
}
