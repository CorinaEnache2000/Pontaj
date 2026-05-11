using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class Zones
{
    public int Id { get; set; }

    public int CountryId { get; set; }

    public string Name { get; set; } = null!;

    public string SIRUTA { get; set; } = null!;

    public virtual Countries Country { get; set; } = null!;
}
