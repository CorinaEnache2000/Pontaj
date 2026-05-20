namespace Pontaj.Models.Admin.OrganizationalUnits;

public class OrganizationalUnitTreeNode
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string Name { get; set; } = null!;

    public bool Active { get; set; }

    public List<OrganizationalUnitTreeNode> Children { get; set; } = new();
}
