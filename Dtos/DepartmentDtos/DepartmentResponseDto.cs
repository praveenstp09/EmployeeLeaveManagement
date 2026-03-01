namespace EmpLeave.Dtos.DepartmentDtos;

public class DepartmentResponseDto
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public int? DepartmentHeadId { get; set; }
}
