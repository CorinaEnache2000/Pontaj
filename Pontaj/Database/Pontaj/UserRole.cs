using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class UserRole
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool Active { get; set; }

    public string? ADGroupName { get; set; }

    public virtual ICollection<UserXUserRole> UserXUserRoles { get; set; } = new List<UserXUserRole>();
}
