using EmpLeave.Models;
using Microsoft.EntityFrameworkCore;

namespace EmpLeave.Config;

public class EmployeeLeaveDbContext : DbContext
{
    public EmployeeLeaveDbContext(DbContextOptions<EmployeeLeaveDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<LeaveBalance> LeaveBalances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure primary keys
        modelBuilder.Entity<Department>().HasKey(d => d.DepartmentId);
        modelBuilder.Entity<Employee>().HasKey(e => e.EmployeeId);
        modelBuilder.Entity<LeaveType>().HasKey(lt => lt.LeaveTypeId);
        modelBuilder.Entity<LeaveRequest>().HasKey(lr => lr.LeaveRequestId);
        modelBuilder.Entity<LeaveBalance>().HasKey(lb => lb.LeaveBalanceId);

        // Configure relationships
        modelBuilder.Entity<Employee>()
            .HasOne<Department>()
            .WithMany()
            .HasForeignKey(e => e.DepartmentId);

        modelBuilder.Entity<LeaveRequest>()
            .HasOne<Employee>()
            .WithMany()
            .HasForeignKey(lr => lr.EmployeeId);

        modelBuilder.Entity<LeaveRequest>()
            .HasOne<LeaveType>()
            .WithMany()
            .HasForeignKey(lr => lr.LeaveTypeId);

        modelBuilder.Entity<LeaveBalance>()
            .HasOne<Employee>()
            .WithMany()
            .HasForeignKey(lb => lb.EmployeeId);

        modelBuilder.Entity<LeaveBalance>()
            .HasOne<LeaveType>()
            .WithMany()
            .HasForeignKey(lb => lb.LeaveTypeId);
    }
}
