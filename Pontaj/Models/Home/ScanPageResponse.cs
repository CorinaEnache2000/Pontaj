namespace Pontaj.Models.Home;

public class ScanPageResponse
{
    public string RowsHtml { get; set; } = string.Empty;

    public int Total { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
