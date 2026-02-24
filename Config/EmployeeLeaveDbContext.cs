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

        // Department -> Employees
        modelBuilder.Entity<EmployeeModel>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Employee -> Manager (self-referencing)
        modelBuilder.Entity<EmployeeModel>()
            .HasOne(e => e.Manager)
            .WithMany(m => m.Subordinates)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // LeaveRequest -> Employee
        modelBuilder.Entity<LeaveRequestModel>()
            .HasOne(lr => lr.Employee)
            .WithMany(e => e.LeaveRequests)
            .HasForeignKey(lr => lr.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // LeaveRequest -> LeaveType
        modelBuilder.Entity<LeaveRequestModel>()
            .HasOne(lr => lr.LeaveType)
            .WithMany(lt => lt.LeaveRequests)
            .HasForeignKey(lr => lr.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // LeaveRequest -> ApprovedBy (Employee)
        modelBuilder.Entity<LeaveRequestModel>()
            .HasOne(lr => lr.ApprovedBy)
            .WithMany()
            .HasForeignKey(lr => lr.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);

        // LeaveBalance -> Employee
        modelBuilder.Entity<LeaveBalanceModel>()
            .HasOne(lb => lb.Employee)
            .WithMany(e => e.LeaveBalances)
            .HasForeignKey(lb => lb.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // LeaveBalance -> LeaveType
        modelBuilder.Entity<LeaveBalanceModel>()
            .HasOne(lb => lb.LeaveType)
            .WithMany(lt => lt.LeaveBalances)
            .HasForeignKey(lb => lb.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one balance per employee per leave type per year
        modelBuilder.Entity<LeaveBalanceModel>()
            .HasIndex(lb => new { lb.EmployeeId, lb.LeaveTypeId, lb.Year })
            .IsUnique();
    }
}
