using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class UATTypes
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<UATs> UATs { get; set; } = new List<UATs>();
}
