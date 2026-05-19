namespace Pontaj.Models.Admin.Users;

// Backing model for the Users admin page: the left-side list. Per-user detail
// (General / Roluri tabs) is fetched on demand.
public class UsersViewModel
{
    public List<UserListItem> Users { get; set; } = new();
}
