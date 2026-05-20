namespace Pontaj.Models.Admin.OrganizationalUnits;

public class OrganizationalUnitDetail
{
    public int Id { get; set; }

    public string Denumire { get; set; } = null!;

    public string DenumireCompleta { get; set; } = null!;

    public string TypeName { get; set; } = null!;

    public string? ParentName { get; set; }

    public string LanguageLabel { get; set; } = null!;

    public string? Address { get; set; }

    public bool Active { get; set; }
}
