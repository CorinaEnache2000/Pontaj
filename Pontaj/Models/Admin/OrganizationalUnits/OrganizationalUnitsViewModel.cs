namespace Pontaj.Models.Admin.OrganizationalUnits;

// Backing model for the organizational-units tree page: the top-level units
// (no parent, or whose parent is missing), each carrying its subtree.
public class OrganizationalUnitsViewModel
{
    public List<OrganizationalUnitTreeNode> Roots { get; set; } = new();
}
