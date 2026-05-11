using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class Counties
{
    public int Id { get; set; }

    public int CountryId { get; set; }

    public int ZoneId { get; set; }

    public string Name { get; set; } = null!;

    public string SIRUTA { get; set; } = null!;

    public string? Acronym { get; set; }

    public virtual Countries Country { get; set; } = null!;

    public virtual ICollection<UATs> UATs { get; set; } = new List<UATs>();
}
