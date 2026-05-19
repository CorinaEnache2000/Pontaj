namespace Pontaj.Models.Admin.Users;

// One row in the left-side users list.
public class UserListItem
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public bool Active { get; set; }
}
