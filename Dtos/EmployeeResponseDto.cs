namespace EmpLeave.Dtos;

public class EmployeeResponseDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
}
