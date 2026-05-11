using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class UserOrganizationalUnits
{
    public int Id { get; set; }

    public int OrganizationalUnitId { get; set; }

    public int UserId { get; set; }

    public bool Deleted { get; set; }

    public virtual OrganizationalUnits OrganizationalUnit { get; set; } = null!;

    public virtual AppUsers User { get; set; } = null!;
}
