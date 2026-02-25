namespace EmpLeave.Models;

public class DepartmentModel
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public int? DepartmentHeadId { get; set; }

    public EmployeeModel? DepartmentHead { get; set; }
    public ICollection<EmployeeModel> Employees { get; set; } = [];
}
