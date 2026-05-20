namespace Pontaj.Models.Admin.Users;

public class UserRoleItem
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public bool IsMainRole { get; set; }

    public bool Active { get; set; }
}
