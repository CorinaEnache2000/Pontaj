using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class OrganizationalUnitTypes
{
    public int Id { get; set; }

    public string NameKey { get; set; } = null!;

    public virtual ICollection<OrganizationalUnits> OrganizationalUnits { get; set; } = new List<OrganizationalUnits>();
}
