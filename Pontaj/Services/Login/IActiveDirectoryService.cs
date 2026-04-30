namespace Pontaj.Services.Login;

public interface IActiveDirectoryService
{
    bool Authenticate(string username, string password);
    ADUser? GetUserInfo(string username);
    List<string> GetUserGroups(string username);
}

public class ADUser
{
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
}
