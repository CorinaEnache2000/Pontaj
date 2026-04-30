using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class UserXUserRole
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int UserRoleId { get; set; }

    public bool Active { get; set; }

    public bool IsMainRole { get; set; }

    public virtual AppUser User { get; set; } = null!;

    public virtual UserRole UserRole { get; set; } = null!;
}
