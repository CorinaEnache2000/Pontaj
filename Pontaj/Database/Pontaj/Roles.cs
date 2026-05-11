using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class Roles
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool Active { get; set; }

    public string? AdGroupName { get; set; }

    public virtual ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();
}
