namespace Pontaj.Models.Admin.Users;

// "General" tab payload for a single user.
public class UserDetail
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    // "LastName FirstName" of the linked employee, or null if unlinked.
    public string? EmployeeName { get; set; }

    // The user's role names (alphabetical).
    public List<string> Roles { get; set; } = new();

    public bool Active { get; set; }
}
