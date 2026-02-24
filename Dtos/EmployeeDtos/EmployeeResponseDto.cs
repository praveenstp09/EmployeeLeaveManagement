namespace EmpLeave.Dtos.EmployeeDtos;

public class EmployeeResponseDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Designation { get; set; } = null!;
    public string Role { get; set; } = null!;
    public int DepartmentId { get; set; }
    public int? ManagerId { get; set; }
    public bool IsActive { get; set; }
    public DateTime DateOfJoining { get; set; }
    public string? ImageUrl { get; set; }
}
