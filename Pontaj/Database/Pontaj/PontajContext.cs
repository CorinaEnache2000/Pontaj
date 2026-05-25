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

    public virtual DbSet<Addresses> Addresses { get; set; }

    public virtual DbSet<AppUsers> AppUsers { get; set; }

    public virtual DbSet<Configurations> Configurations { get; set; }

    public virtual DbSet<Countries> Countries { get; set; }

    public virtual DbSet<Counties> Counties { get; set; }

    public virtual DbSet<Employees> Employees { get; set; }

    public virtual DbSet<Languages> Languages { get; set; }

    public virtual DbSet<Localities> Localities { get; set; }

    public virtual DbSet<LogEntries> LogEntries { get; set; }

    public virtual DbSet<OrganizationalUnits> OrganizationalUnits { get; set; }

    public virtual DbSet<OrganizationalUnitTypes> OrganizationalUnitTypes { get; set; }

    public virtual DbSet<Punches> Punches { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<Streets> Streets { get; set; }

    public virtual DbSet<StreetTypes> StreetTypes { get; set; }

    public virtual DbSet<TextResources> TextResources { get; set; }

    public virtual DbSet<UATs> UATs { get; set; }

    public virtual DbSet<UATTypes> UATTypes { get; set; }

    public virtual DbSet<EmployeeOrganizationalUnits> EmployeeOrganizationalUnits { get; set; }

    public virtual DbSet<UserRoles> UserRoles { get; set; }

    public virtual DbSet<WorkStations> WorkStations { get; set; }

    public virtual DbSet<Zones> Zones { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=DB01\\SQL01;Database=Pontaj;Integrated Security=True;TrustServerCertificate=True;Encrypt=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Addresses>(entity =>
        {
            entity.Property(e => e.Apartment)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.Building)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.Entrance)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.Floor)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.Line1)
                .HasMaxLength(100)
                .IsFixedLength();
            entity.Property(e => e.Line1Ukrainian)
                .HasMaxLength(100)
                .IsFixedLength();
            entity.Property(e => e.Line2)
                .HasMaxLength(50)
                .IsFixedLength();
            entity.Property(e => e.Line2Ukrainian)
                .HasMaxLength(50)
                .IsFixedLength();
            entity.Property(e => e.Number)
                .HasMaxLength(10)
                .IsFixedLength();

            entity.HasOne(d => d.Country).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.CountryId)
                .HasConstraintName("FK_Addresses_Countries");

            entity.HasOne(d => d.Street).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.StreetId)
                .HasConstraintName("FK_Addresses_Streets");
        });

        modelBuilder.Entity<AppUsers>(entity =>
        {
            entity.HasIndex(e => e.Username, "UX_AppUsers_Username").IsUnique();

            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Username).HasMaxLength(200);

            entity.HasOne(d => d.Employee).WithMany(p => p.AppUsers)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_AppUsers_Employees");
        });

        modelBuilder.Entity<Configurations>(entity =>
        {
            entity.HasIndex(e => e.ConfigKey, "UQ_Configurations_ConfigKey").IsUnique();

            entity.Property(e => e.ConfigKey).HasMaxLength(200);
        });

        modelBuilder.Entity<Countries>(entity =>
        {
            entity.Property(e => e.FiscalAttribute)
                .HasMaxLength(5)
                .IsFixedLength();
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsFixedLength();
            entity.Property(e => e.NameUkrainian)
                .HasMaxLength(50)
                .IsFixedLength();
        });

        modelBuilder.Entity<Counties>(entity =>
        {
            entity.Property(e => e.Acronym)
                .HasMaxLength(2)
                .IsFixedLength();
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.SIRUTA)
                .HasMaxLength(6)
                .IsFixedLength();

            entity.HasOne(d => d.Country).WithMany(p => p.Counties)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Counties_Countries");
        });

        modelBuilder.Entity<Employees>(entity =>
        {
            entity.HasIndex(e => e.Badge, "IX_Employees_Badge");

            entity.HasIndex(e => e.Username, "UX_Employees_Username")
                .IsUnique()
                .HasFilter("([Username] IS NOT NULL)");

            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Badge).HasMaxLength(60);
            entity.Property(e => e.Mark).HasMaxLength(60);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Pin).HasMaxLength(60);
            entity.Property(e => e.Username).HasMaxLength(200);
        });

        modelBuilder.Entity<Languages>(entity =>
        {
            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.Label)
                .HasMaxLength(100)
                .HasDefaultValue("Română");
            entity.Property(e => e.NameKey)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Localities>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(40)
                .IsFixedLength();
            entity.Property(e => e.SIRUTA)
                .HasMaxLength(6)
                .IsFixedLength();

            entity.HasOne(d => d.UAT).WithMany(p => p.Localities)
                .HasForeignKey(d => d.UATId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Localities_UATs");
        });

        modelBuilder.Entity<LogEntries>(entity =>
        {
            entity.Property(e => e.Action).HasMaxLength(200);
            entity.Property(e => e.LoggedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<OrganizationalUnits>(entity =>
        {
            entity.Property(e => e.InternalNameKey)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.PublicNameKey)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.Address).WithMany(p => p.OrganizationalUnits)
                .HasForeignKey(d => d.AddressId)
                .HasConstraintName("FK_OrganizationalUnits_Addresses");

            entity.HasOne(d => d.DefaultLanguage).WithMany(p => p.OrganizationalUnits)
                .HasForeignKey(d => d.DefaultLanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganizationalUnits_Languages");

            entity.HasOne(d => d.OrganizationalUnitType).WithMany(p => p.OrganizationalUnits)
                .HasForeignKey(d => d.OrganizationalUnitTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganizationalUnits_OrganizationalUnitTypes");

            entity.HasOne(d => d.ParentOrganizationalUnit).WithMany(p => p.InverseParentOrganizationalUnit)
                .HasForeignKey(d => d.ParentOrganizationalUnitId)
                .HasConstraintName("FK_OrganizationalUnits_OrganizationalUnits");
        });

        modelBuilder.Entity<OrganizationalUnitTypes>(entity =>
        {
            entity.Property(e => e.NameKey)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Punches>(entity =>
        {
            entity.HasIndex(e => new { e.EmployeeId, e.InsertedDate }, "IX_Punches_EmployeeId_InsertedDate")
                .IsDescending(false, true);

            entity.Property(e => e.Badge).HasMaxLength(60);
            entity.Property(e => e.Ip).HasMaxLength(50);
            entity.Property(e => e.InsertedDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.InsertedMoment).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.InsertedTime).HasDefaultValueSql("(CONVERT([time],getdate()))");

            entity.HasOne(d => d.Employee).WithMany(p => p.Punches)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Punches_Employees");

            entity.HasOne(d => d.OrganizationalUnit).WithMany(p => p.Punches)
                .HasForeignKey(d => d.OrganizationalUnitId)
                .HasConstraintName("FK_Punches_OrganizationalUnits");

            entity.HasOne(d => d.WorkStation).WithMany(p => p.Punches)
                .HasForeignKey(d => d.WorkStationId)
                .HasConstraintName("FK_Punches_WorkStations");
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.AdGroupName).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Streets>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsFixedLength();

            entity.HasOne(d => d.Locality).WithMany(p => p.Streets)
                .HasForeignKey(d => d.LocalityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Streets_Localities");

            entity.HasOne(d => d.StreetType).WithMany(p => p.Streets)
                .HasForeignKey(d => d.StreetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Streets_StreetTypes");
        });

        modelBuilder.Entity<StreetTypes>(entity =>
        {
            entity.Property(e => e.Abbreviation)
                .HasMaxLength(5)
                .IsFixedLength();
            entity.Property(e => e.Name)
                .HasMaxLength(25)
                .IsFixedLength();
        });

        modelBuilder.Entity<TextResources>(entity =>
        {
            entity.Property(e => e.ResourceKey)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.Language).WithMany(p => p.TextResources)
                .HasForeignKey(d => d.LanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TextResources_Languages");
        });

        modelBuilder.Entity<UATs>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(40)
                .IsFixedLength();
            entity.Property(e => e.SIRUTA)
                .HasMaxLength(6)
                .IsFixedLength();

            entity.HasOne(d => d.County).WithMany(p => p.UATs)
                .HasForeignKey(d => d.CountyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UATs_Counties");

            entity.HasOne(d => d.UATType).WithMany(p => p.UATs)
                .HasForeignKey(d => d.UATTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UATs_UATTypes");
        });

        modelBuilder.Entity<UATTypes>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(10)
                .IsFixedLength();
        });

        modelBuilder.Entity<EmployeeOrganizationalUnits>(entity =>
        {
            entity.Property(e => e.Active).HasDefaultValue(true);

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeOrganizationalUnits)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeOrganizationalUnits_Employees");

            entity.HasOne(d => d.OrganizationalUnit).WithMany(p => p.EmployeeOrganizationalUnits)
                .HasForeignKey(d => d.OrganizationalUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeOrganizationalUnits_OrganizationalUnits");
        });

        modelBuilder.Entity<UserRoles>(entity =>
        {
            entity.Property(e => e.Active).HasDefaultValue(true);

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Roles");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_AppUsers");
        });

        modelBuilder.Entity<WorkStations>(entity =>
        {
            entity.HasIndex(e => e.Ip, "UX_WorkStations_Ip")
                .IsUnique()
                .HasFilter("([Ip] IS NOT NULL)");

            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Ip).HasMaxLength(50);
            entity.Property(e => e.NameKey).HasMaxLength(100);

            entity.HasOne(d => d.OrganizationalUnit).WithMany(p => p.WorkStations)
                .HasForeignKey(d => d.OrganizationalUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkStations_OrganizationalUnits");
        });

        modelBuilder.Entity<Zones>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.SIRUTA)
                .HasMaxLength(6)
                .IsFixedLength();

            entity.HasOne(d => d.Country).WithMany(p => p.Zones)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Zones_Countries");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
