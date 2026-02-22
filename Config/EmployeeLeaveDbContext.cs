using EmpLeave.Models;
using Microsoft.EntityFrameworkCore;

namespace EmpLeave.Config;

public class EmployeeLeaveDbContext : DbContext
{
    public EmployeeLeaveDbContext(DbContextOptions<EmployeeLeaveDbContext> options)
        : base(options)
    {
    }

    public DbSet<DepartmentModel> Departments { get; set; }
    public DbSet<EmployeeModel> Employees { get; set; }
    public DbSet<LeaveTypeModel> LeaveTypes { get; set; }
    public DbSet<LeaveRequestModel> LeaveRequests { get; set; }
    public DbSet<LeaveBalanceModel> LeaveBalances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure primary keys
        modelBuilder.Entity<DepartmentModel>().HasKey(d => d.DepartmentId);
        modelBuilder.Entity<EmployeeModel>().HasKey(e => e.EmployeeId);
        modelBuilder.Entity<LeaveTypeModel>().HasKey(lt => lt.LeaveTypeId);
        modelBuilder.Entity<LeaveRequestModel>().HasKey(lr => lr.LeaveRequestId);
        modelBuilder.Entity<LeaveBalanceModel>().HasKey(lb => lb.LeaveBalanceId);

        // Configure relationships
        modelBuilder.Entity<EmployeeModel>()
            .HasOne<DepartmentModel>()
            .WithMany()
            .HasForeignKey(e => e.DepartmentId);

        modelBuilder.Entity<LeaveRequestModel>()
            .HasOne<EmployeeModel>()
            .WithMany()
            .HasForeignKey(lr => lr.EmployeeId);

        modelBuilder.Entity<LeaveRequestModel>()
            .HasOne<LeaveTypeModel>()
            .WithMany()
            .HasForeignKey(lr => lr.LeaveTypeId);

        modelBuilder.Entity<LeaveBalanceModel>()
            .HasOne<EmployeeModel>()
            .WithMany()
            .HasForeignKey(lb => lb.EmployeeId);

        modelBuilder.Entity<LeaveBalanceModel>()
            .HasOne<LeaveTypeModel>()
            .WithMany()
            .HasForeignKey(lb => lb.LeaveTypeId);
    }
}
