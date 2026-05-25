using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class Employees
{
    public int Id { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? Pin { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Badge { get; set; }

    public bool Active { get; set; }

    public string? Mark { get; set; }

    public string? Username { get; set; }

    public virtual ICollection<AppUsers> AppUsers { get; set; } = new List<AppUsers>();

    public virtual ICollection<EmployeeOrganizationalUnits> EmployeeOrganizationalUnits { get; set; } = new List<EmployeeOrganizationalUnits>();

    public virtual ICollection<Punches> Punches { get; set; } = new List<Punches>();
}
