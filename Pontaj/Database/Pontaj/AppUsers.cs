using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class AppUsers
{
    public int Id { get; set; }

    public int? EmployeeId { get; set; }

    public string Username { get; set; } = null!;

    public bool Active { get; set; }

    public virtual Employees? Employee { get; set; }

    public virtual ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();
}
