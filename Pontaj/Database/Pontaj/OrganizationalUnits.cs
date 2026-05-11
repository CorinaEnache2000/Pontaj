using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class OrganizationalUnits
{
    public int Id { get; set; }

    public string InternalNameKey { get; set; } = null!;

    public string PublicNameKey { get; set; } = null!;

    public int OrganizationalUnitTypeId { get; set; }

    public int? ParentOrganizationalUnitId { get; set; }

    public int DefaultLanguageId { get; set; }

    public bool Active { get; set; }

    public int? AddressId { get; set; }

    public virtual Addresses? Address { get; set; }

    public virtual Languages DefaultLanguage { get; set; } = null!;

    public virtual ICollection<OrganizationalUnits> InverseParentOrganizationalUnit { get; set; } = new List<OrganizationalUnits>();

    public virtual OrganizationalUnitTypes OrganizationalUnitType { get; set; } = null!;

    public virtual OrganizationalUnits? ParentOrganizationalUnit { get; set; }

    public virtual ICollection<Punches> Punches { get; set; } = new List<Punches>();

    public virtual ICollection<UserOrganizationalUnits> UserOrganizationalUnits { get; set; } = new List<UserOrganizationalUnits>();

    public virtual ICollection<WorkStations> WorkStations { get; set; } = new List<WorkStations>();
}
