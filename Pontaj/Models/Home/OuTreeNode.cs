namespace Pontaj.Models.Home;

public class OuTreeNode
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<OuTreeNode> Children { get; set; } = new();
}
