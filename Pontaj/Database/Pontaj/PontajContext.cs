using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Pontaj.Database.Pontaj;

public partial class PontajContext : DbContext
{
    public PontajContext()
    {
    }

    public PontajContext(DbContextOptions<PontajContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<Configuration> Configurations { get; set; }

    public virtual DbSet<LogEntry> LogEntries { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<UserXUserRole> UserXUserRoles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=DB01\\SQL01;Database=Pontaj;Integrated Security=True;TrustServerCertificate=True;Encrypt=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUser");

            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Username).HasMaxLength(200);
        });

        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.ToTable("Configuration");

            entity.HasIndex(e => e.ConfigKey, "UQ_Configuration_ConfigKey").IsUnique();

            entity.Property(e => e.ConfigKey).HasMaxLength(200);
        });

        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LogEntri__3214EC0710CE0268");

            entity.Property(e => e.Action).HasMaxLength(200);
            entity.Property(e => e.LoggedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserRole__3214EC07D6F64B48");

            entity.ToTable("UserRole");

            entity.Property(e => e.ADGroupName).HasMaxLength(100);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<UserXUserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserXUse__3214EC078F92E93A");

            entity.ToTable("UserXUserRole");

            entity.Property(e => e.Active).HasDefaultValue(true);

            entity.HasOne(d => d.User).WithMany(p => p.UserXUserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserXUserRole_User");

            entity.HasOne(d => d.UserRole).WithMany(p => p.UserXUserRoles)
                .HasForeignKey(d => d.UserRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserXUserRole_UserRole");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
