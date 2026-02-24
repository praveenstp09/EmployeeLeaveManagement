namespace EmpLeave.Models;

public class EmployeeModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string? ImageUrl { get; set; } = string.Empty;
    public DateTime DateOfJoining { get; set; }
    public bool IsActive { get; set; }
    public string Role { get; set; } = "Employee"; // "Employee", "HR", "SuperAdmin"
    public int DepartmentId { get; set; }
    public int? ManagerId { get; set; }

    public DepartmentModel Department { get; set; } = null!;
    public EmployeeModel? Manager { get; set; }
    public ICollection<EmployeeModel> Subordinates { get; set; } = [];
    public ICollection<LeaveRequestModel> LeaveRequests { get; set; } = [];
    public ICollection<LeaveBalanceModel> LeaveBalances { get; set; } = [];
}
