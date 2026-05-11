using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class Streets
{
    public int Id { get; set; }

    public int LocalityId { get; set; }

    public int StreetTypeId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Addresses> Addresses { get; set; } = new List<Addresses>();

    public virtual Localities Locality { get; set; } = null!;

    public virtual StreetTypes StreetType { get; set; } = null!;
}
