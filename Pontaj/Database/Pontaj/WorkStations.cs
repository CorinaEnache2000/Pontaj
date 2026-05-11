using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class WorkStations
{
    public int Id { get; set; }

    public string NameKey { get; set; } = null!;

    public string? Ip { get; set; }

    public bool Active { get; set; }

    public int OrganizationalUnitId { get; set; }

    public virtual OrganizationalUnits OrganizationalUnit { get; set; } = null!;

    public virtual ICollection<Punches> Punches { get; set; } = new List<Punches>();
}
