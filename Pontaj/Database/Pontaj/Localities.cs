using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class Localities
{
    public int Id { get; set; }

    public int UATId { get; set; }

    public string Name { get; set; } = null!;

    public string SIRUTA { get; set; } = null!;

    public virtual ICollection<Streets> Streets { get; set; } = new List<Streets>();

    public virtual UATs UAT { get; set; } = null!;
}
