namespace Pontaj.Models.Admin.Users;

public class UserDetail
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string? EmployeeName { get; set; }

    public List<string> Roles { get; set; } = new();

    public bool Active { get; set; }
}
