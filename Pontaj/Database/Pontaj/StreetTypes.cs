using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class StreetTypes
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Abbreviation { get; set; } = null!;

    public virtual ICollection<Streets> Streets { get; set; } = new List<Streets>();
}
