namespace Pontaj.Models;

public class ResponseBase
{
    public string Status { get; set; } = "success";
    public string? Reason { get; set; }
    public object? Data { get; set; }
    public string? Token { get; set; }

    public static ResponseBase Success(object? data = null) =>
        new() { Status = "success", Reason = null, Data = data };

    public static ResponseBase Error(string reason, object? data = null) =>
        new() { Status = "error", Reason = reason, Data = data };
}
