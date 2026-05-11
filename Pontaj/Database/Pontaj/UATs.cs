using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class UATs
{
    public int Id { get; set; }

    public int CountyId { get; set; }

    public int UATTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string SIRUTA { get; set; } = null!;

    public virtual Counties County { get; set; } = null!;

    public virtual ICollection<Localities> Localities { get; set; } = new List<Localities>();

    public virtual UATTypes UATType { get; set; } = null!;
}
