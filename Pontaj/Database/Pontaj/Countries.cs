using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class Countries
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string FiscalAttribute { get; set; } = null!;

    public string? NameUkrainian { get; set; }

    public virtual ICollection<Addresses> Addresses { get; set; } = new List<Addresses>();

    public virtual ICollection<Counties> Counties { get; set; } = new List<Counties>();

    public virtual ICollection<Zones> Zones { get; set; } = new List<Zones>();
}
