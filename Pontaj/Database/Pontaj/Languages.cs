using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class Languages
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Label { get; set; } = null!;

    public string NameKey { get; set; } = null!;

    public virtual ICollection<OrganizationalUnits> OrganizationalUnits { get; set; } = new List<OrganizationalUnits>();

    public virtual ICollection<TextResources> TextResources { get; set; } = new List<TextResources>();
}
