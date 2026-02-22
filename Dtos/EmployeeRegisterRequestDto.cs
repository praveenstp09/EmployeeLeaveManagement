using System.ComponentModel.DataAnnotations;

namespace EmpLeave.Dtos;

public class EmployeeRegisterRequestDto
{
    [Required(ErrorMessage = "Employee Code is required")]
    public string EmployeeCode { get; set; } = null!;

    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,}$",
        ErrorMessage = "Password must contain uppercase, lowercase and number")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Designation is required")]
    [StringLength(50)]
    public string Designation { get; set; } = null!;

    [Required(ErrorMessage = "Date of Joining is required")]
    [DataType(DataType.Date)]
    public DateTime DateOfJoining { get; set; }

    [Required(ErrorMessage = "Department is required")]
    public int DepartmentId { get; set; }

    public int? ManagerId { get; set; }
}
