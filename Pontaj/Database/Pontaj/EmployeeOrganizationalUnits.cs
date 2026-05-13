using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class EmployeeOrganizationalUnits
{
    public int Id { get; set; }

    public int OrganizationalUnitId { get; set; }

    public int EmployeeId { get; set; }

    public bool Active { get; set; }

    public virtual Employees Employee { get; set; } = null!;

    public virtual OrganizationalUnits OrganizationalUnit { get; set; } = null!;
}
