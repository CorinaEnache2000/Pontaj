using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class AppUsers
{
    public int Id { get; set; }

    public int? PersonId { get; set; }

    public string Username { get; set; } = null!;

    public bool Active { get; set; }

    public virtual ICollection<UserOrganizationalUnits> UserOrganizationalUnits { get; set; } = new List<UserOrganizationalUnits>();

    public virtual ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();
}
