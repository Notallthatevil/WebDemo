using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace WebDemo.Server.Models
{
    public partial class NateDbContext : DbContext
    {
        public NateDbContext()
        {
        }

        public NateDbContext(DbContextOptions<NateDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Department> Departments { get; set; } = null!;
        public virtual DbSet<Employee> Employees { get; set; } = null!;
        public virtual DbSet<Manager> Managers { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Name=ConnectionStrings:NateDb");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasIndex(e => e.Name, "UQ__tmp_ms_x__72E12F1BAB7A1210")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("name");
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.ToTable("Employee");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.DepartmentId).HasColumnName("department_id");

                entity.Property(e => e.FirstName)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("first_name");

                entity.Property(e => e.LastName)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("last_name");

                entity.Property(e => e.Title)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("title");

                entity.HasOne(d => d.Department)
                    .WithMany(p => p.Employees)
                    .HasForeignKey(d => d.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Employee_Departments");
            });

            modelBuilder.Entity<Manager>(entity =>
            {
                entity.HasKey(e => new { e.DepartmentId, e.ManagerId });

                entity.ToTable("Manager");

                entity.HasIndex(e => e.DepartmentId, "UQ__Manager__C22324230792AFE7")
                    .IsUnique();

                entity.Property(e => e.DepartmentId).HasColumnName("department_id");

                entity.Property(e => e.ManagerId).HasColumnName("manager_id");

                entity.HasOne(d => d.Department)
                    .WithOne(p => p.Manager)
                    .HasForeignKey<Manager>(d => d.DepartmentId)
                    .HasConstraintName("FK_Manager_Departments");

                entity.HasOne(d => d.ManagerNavigation)
                    .WithMany(p => p.Managers)
                    .HasForeignKey(d => d.ManagerId)
                    .HasConstraintName("FK_Manager_Employee");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
