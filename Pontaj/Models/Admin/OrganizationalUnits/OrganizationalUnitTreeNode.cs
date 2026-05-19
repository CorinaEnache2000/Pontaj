namespace Pontaj.Models.Admin.OrganizationalUnits;

// One node in the organizational-units tree. Built from the self-referencing
// ParentOrganizationalUnitId. Name is resolved from the TextResources table
// (OU PublicNameKey + OU DefaultLanguageId).
public class OrganizationalUnitTreeNode
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string Name { get; set; } = null!;

    public bool Active { get; set; }

    public List<OrganizationalUnitTreeNode> Children { get; set; } = new();
}
