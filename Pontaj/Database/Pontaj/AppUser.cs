using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class AppUser
{
    public int ID { get; set; }

    public int? PersonId { get; set; }

    public string Username { get; set; } = null!;

    public bool Active { get; set; }

    public virtual ICollection<UserXUserRole> UserXUserRoles { get; set; } = new List<UserXUserRole>();
}
