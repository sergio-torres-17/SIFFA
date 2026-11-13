using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SIFFA.WebApp.Data.Models;

public partial class SffaContext : DbContext
{
    public SffaContext()
    {
    }

    public SffaContext(DbContextOptions<SffaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccessAction> AccessActions { get; set; }

    public virtual DbSet<AddEmployee> AddEmployees { get; set; }

    public virtual DbSet<AddType> AddTypes { get; set; }

    public virtual DbSet<AddTypeEmployee> AddTypeEmployees { get; set; }

    public virtual DbSet<Area> Areas { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeType> EmployeeTypes { get; set; }

    public virtual DbSet<Trace> Traces { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.\\DEVCHECK;Database=SFFA;Trusted_Connection=False;User=sa;Password=personal;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");

        modelBuilder.Entity<AccessAction>(entity =>
        {
            entity.HasKey(e => e.AccessActionId).HasName("PK__AccessAc__10D720B61F779D95");

            entity.ToTable("AccessAction", "Catalog");

            entity.HasIndex(e => e.AccessActionName, "UX_AccessAction_AccessActionName").IsUnique();

            entity.Property(e => e.AccessActionDescription).HasMaxLength(200);
            entity.Property(e => e.AccessActionName).HasMaxLength(50);
            entity.Property(e => e.InsertionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<AddEmployee>(entity =>
        {
            entity.HasKey(e => e.AddEmployeeId).HasName("PK__AddEmplo__6D6935148C0EFEA3");

            entity.ToTable("AddEmployee", "Add");

            entity.HasIndex(e => e.AddTypeId, "IX_AddEmployee_AddTypeId");

            entity.HasIndex(e => new { e.EmployeeId, e.AddTypeId }, "IX_AddEmployee_EmployeeId_AddTypeId");

            entity.Property(e => e.DeliveryDate).HasColumnType("datetime");
            entity.Property(e => e.InsertionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.AddType).WithMany(p => p.AddEmployees)
                .HasForeignKey(d => d.AddTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_AddEmployee_AddTypeId");

            entity.HasOne(d => d.Employee).WithMany(p => p.AddEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_AddEmployee_EmployeeId");
        });

        modelBuilder.Entity<AddType>(entity =>
        {
            entity.HasKey(e => e.AddTypeId).HasName("PK__AddType__7B30BD5EF6F4E17A");

            entity.ToTable("AddType", "Catalog");

            entity.HasIndex(e => e.AddTypeName, "UX_AddType_AddTypeName").IsUnique();

            entity.Property(e => e.AddTypeDescription).HasMaxLength(200);
            entity.Property(e => e.AddTypeName).HasMaxLength(50);
            entity.Property(e => e.InsertionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<AddTypeEmployee>(entity =>
        {
            entity.HasKey(e => e.AddTypeEmployeeId).HasName("PK__AddTypeE__A8AE99C74C58F595");

            entity.ToTable("AddTypeEmployee", "Add");

            entity.HasIndex(e => e.AddTypeId, "IX_AddTypeEmployee_AddTypeId");

            entity.HasIndex(e => new { e.EmployeeId, e.AddTypeId }, "UX_AddTypeEmployee_EmployeeId_AddTypeId").IsUnique();

            entity.Property(e => e.InsertionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.AddType).WithMany(p => p.AddTypeEmployees)
                .HasForeignKey(d => d.AddTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_AddTypeEmployee_AddTypeId");

            entity.HasOne(d => d.Employee).WithMany(p => p.AddTypeEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_AddTypeEmployee_EmployeeId");
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.HasKey(e => e.AreaId).HasName("PK__Area__70B8204885C5510E");

            entity.ToTable("Area", "Catalog");

            entity.HasIndex(e => e.Description, "UX_Area_description").IsUnique();

            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .HasColumnName("description");
            entity.Property(e => e.InsertionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04F1193055737");

            entity.ToTable("Employee", "Catalog");

            entity.HasIndex(e => e.AreaId, "IX_Employee_AreaId");

            entity.HasIndex(e => e.EmployeeEmail, "UX_Employee_EmployeeEmail").IsUnique();

            entity.HasIndex(e => e.InernalIdentifier, "UX_Employee_InernalIdentifier").IsUnique();

            entity.Property(e => e.EmployeeId).ValueGeneratedNever();
            entity.Property(e => e.EmployeeEmail).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.InernalIdentifier).HasMaxLength(100);
            entity.Property(e => e.InsertionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LastName).HasMaxLength(100);

            entity.HasOne(d => d.Area).WithMany(p => p.Employees)
                .HasForeignKey(d => d.AreaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_Employee_AreaId");
        });

        modelBuilder.Entity<EmployeeType>(entity =>
        {
            entity.HasKey(e => e.EmployeeTypeId).HasName("PK__Employee__1F1B6A94A5B8216D");

            entity.ToTable("EmployeeType", "Catalog");

            entity.HasIndex(e => e.EmployeeTypeName, "UX_EmployeeType_EmployeeTypeName").IsUnique();

            entity.Property(e => e.EmployeeTypeDescription).HasMaxLength(200);
            entity.Property(e => e.EmployeeTypeName).HasMaxLength(50);
            entity.Property(e => e.InsertionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Trace>(entity =>
        {
            entity.HasKey(e => e.TraceId).HasName("PK__Trace__70C9CE9CE1C72DF0");

            entity.ToTable("Trace", "AccessControl");

            entity.HasIndex(e => e.AccessActionId, "IX_Trace_AccessActionId");

            entity.HasIndex(e => new { e.EmployeeId, e.TraceDate }, "IX_Trace_EmployeeId_TraceDate").IsDescending(false, true);

            entity.Property(e => e.InsertionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TraceDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TraceDescription).HasMaxLength(100);

            entity.HasOne(d => d.AccessAction).WithMany(p => p.Traces)
                .HasForeignKey(d => d.AccessActionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_Trace_AccessActionId");

            entity.HasOne(d => d.Employee).WithMany(p => p.Traces)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_Trace_EmployeeId");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
