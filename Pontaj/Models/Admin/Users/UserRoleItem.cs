namespace Pontaj.Models.Admin.Users;

// One row in the "Roluri" tab for a selected user.
public class UserRoleItem
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public bool IsMainRole { get; set; }

    public bool Active { get; set; }
}
