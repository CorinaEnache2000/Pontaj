namespace Pontaj.Models.Admin.Users;

public class UserListItem
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public bool Active { get; set; }
}
