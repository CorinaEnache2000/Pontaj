using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace Pontaj.Services.Login;

[SupportedOSPlatform("windows")]
public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly string _ldapServer;
    private readonly string _domain;

    public ActiveDirectoryService(string ldapServer, string domain)
    {
        _ldapServer = ldapServer;
        _domain = domain;
    }

    public bool Authenticate(string username, string password)
    {
        try
        {
            using var connection = new LdapConnection(new LdapDirectoryIdentifier(_ldapServer));
            connection.AuthType = AuthType.Basic;
            var credentials = new NetworkCredential($"{_domain}\\{username}", password);
            connection.Bind(credentials);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ADUser? GetUserInfo(string username)
    {
        using var entry = new DirectoryEntry($"LDAP://{_ldapServer}");
        using var searcher = new DirectorySearcher(entry)
        {
            Filter = $"(sAMAccountName={EscapeLdapFilter(username)})"
        };

        searcher.PropertiesToLoad.Add("displayName");

        var result = searcher.FindOne();
        if (result == null) return null;

        return new ADUser
        {
            Username = username,
            DisplayName = result.Properties["displayName"]?[0]?.ToString()
        };
    }

    public List<string> GetUserGroups(string username)
    {
        var groups = new List<string>();

        using var entry = new DirectoryEntry($"LDAP://{_ldapServer}");
        using var searcher = new DirectorySearcher(entry)
        {
            Filter = $"(sAMAccountName={EscapeLdapFilter(username)})"
        };
        searcher.PropertiesToLoad.Add("memberOf");

        var result = searcher.FindOne();
        if (result == null) return groups;

        foreach (var g in result.Properties["memberOf"])
        {
            var match = Regex.Match(g.ToString()!, @"CN=([^,]+)");
            if (match.Success)
                groups.Add(match.Groups[1].Value);
        }

        return groups;
    }

    private static string EscapeLdapFilter(string input) =>
        input.Replace("\\", "\\5c").Replace("*", "\\2a").Replace("(", "\\28").Replace(")", "\\29").Replace("\0", "\\00");
}
