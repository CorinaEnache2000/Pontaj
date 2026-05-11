using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class Addresses
{
    public int Id { get; set; }

    public int? StreetId { get; set; }

    public string? Number { get; set; }

    public string? Building { get; set; }

    public string? Entrance { get; set; }

    public string? Floor { get; set; }

    public string? Apartment { get; set; }

    public string? Line1 { get; set; }

    public string? Line2 { get; set; }

    public int? CountryId { get; set; }

    public string? Line1Ukrainian { get; set; }

    public string? Line2Ukrainian { get; set; }

    public virtual Countries? Country { get; set; }

    public virtual ICollection<OrganizationalUnits> OrganizationalUnits { get; set; } = new List<OrganizationalUnits>();

    public virtual Streets? Street { get; set; }
}
