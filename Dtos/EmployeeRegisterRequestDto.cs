namespace EmpLeave.Dtos;

public class EmployeeRegisterRequestDto
{
    public string EmployeeCode { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Designation { get; set; } = null!;
    public DateTime DateOfJoining { get; set; }
    public int DepartmentId { get; set; }
    public int? ManagerId { get; set; }
}
