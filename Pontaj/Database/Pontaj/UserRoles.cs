using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class UserRoles
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RoleId { get; set; }

    public bool Active { get; set; }

    public bool IsMainRole { get; set; }

    public virtual Roles Role { get; set; } = null!;

    public virtual AppUsers User { get; set; } = null!;
}
